#if UNITY_EDITOR
using System;
using System.IO;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>
    /// Creates the starter ore data and prefabs with the same data-driven pattern used
    /// by the Tower Defense project's TowerData and setup menu.
    /// </summary>
    public static class MiningOreSetupMenu
    {
        private const string ModelFolder = "Assets/Ores/Models";
        private const string DataFolder = "Assets/GameData/Ores";
        private const string PrefabFolder = "Assets/Prefabs/Ores";

        private readonly struct OreSpec
        {
            public OreSpec(OreKind kind, string name, string description, int tier,
                int miningPower, int durability, int sellValue, Color mapColor, string modelFile)
            {
                Kind = kind;
                Name = name;
                Description = description;
                Tier = tier;
                MiningPower = miningPower;
                Durability = durability;
                SellValue = sellValue;
                MapColor = mapColor;
                ModelFile = modelFile;
            }

            public OreKind Kind { get; }
            public string Name { get; }
            public string Description { get; }
            public int Tier { get; }
            public int MiningPower { get; }
            public int Durability { get; }
            public int SellValue { get; }
            public Color MapColor { get; }
            public string ModelFile { get; }
        }

        private static readonly OreSpec[] StarterOres =
        {
            new(OreKind.Stone, "Stone", "Common stone. The first material a miner can break.",
                1, 1, 10, 1, new Color(0.48f, 0.52f, 0.56f), "stone_tier1.fbx"),
            new(OreKind.Coal, "Coal", "Dark fuel ore unlocked after basic stone mining.",
                2, 3, 20, 4, new Color(0.10f, 0.12f, 0.14f), "coal_tier2.fbx"),
            new(OreKind.Copper, "Copper", "Valuable metallic ore used for stronger upgrades.",
                3, 6, 35, 9, new Color(0.82f, 0.32f, 0.08f), "copper_tier3.fbx")
        };

        [MenuItem("Mining Simulator/Setup/Create or Update Starter Ores")]
        public static void CreateOrUpdateStarterOres()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(PrefabFolder);

            foreach (OreSpec spec in StarterOres)
            {
                OreData data = CreateOrUpdateData(spec);
                GameObject prefab = CreateOrUpdatePrefab(spec, data);
                if (prefab != null)
                {
                    SetObjectReference(data, "prefab", prefab);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Mining ore setup complete: {StarterOres.Length} independent OreData assets and prefabs.");
        }

        // Entry point used by Unity batch mode and CI.
        public static void CreateOrUpdateStarterOresBatch()
        {
            CreateOrUpdateStarterOres();
        }

        private static OreData CreateOrUpdateData(OreSpec spec)
        {
            string path = $"{DataFolder}/{spec.Name}.asset";
            OreData data = AssetDatabase.LoadAssetAtPath<OreData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<OreData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var serialized = new SerializedObject(data);
            serialized.FindProperty("kind").enumValueIndex = (int)spec.Kind;
            serialized.FindProperty("displayName").stringValue = spec.Name;
            serialized.FindProperty("description").stringValue = spec.Description;
            serialized.FindProperty("tier").intValue = spec.Tier;
            serialized.FindProperty("miningPowerRequired").intValue = spec.MiningPower;
            serialized.FindProperty("durability").intValue = spec.Durability;
            serialized.FindProperty("baseSellValue").intValue = spec.SellValue;
            serialized.FindProperty("mapColor").colorValue = spec.MapColor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject CreateOrUpdatePrefab(OreSpec spec, OreData data)
        {
            string modelPath = $"{ModelFolder}/{spec.ModelFile}";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError($"Cannot create {spec.Name} prefab. Model was not imported at {modelPath}.");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"Cannot instantiate model for {spec.Name}.");
                return null;
            }

            try
            {
                instance.name = spec.Name;
                Ore ore = instance.GetComponent<Ore>() ?? instance.AddComponent<Ore>();
                ore.SetData(data);
                EnsureCollider(instance);

                string prefabPath = $"{PrefabFolder}/{spec.Name}.prefab";
                return PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void EnsureCollider(GameObject root)
        {
            if (root.GetComponent<Collider>() != null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = root.transform.InverseTransformVector(worldBounds.size);
            collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent))
            {
                throw new InvalidOperationException($"Invalid Unity folder path: {path}");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
