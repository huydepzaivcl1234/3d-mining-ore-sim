#if UNITY_EDITOR
using System;
using System.IO;
using Microlight.MicroBar;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private const string NpcPrefabFolder = "Assets/Prefabs/NPC";
        private const string SystemPrefabFolder = "Assets/Prefabs/Systems";
        private const string NpcPrefabPath = NpcPrefabFolder + "/MiningNpc.prefab";
        private const string RuntimePrefabPath = SystemPrefabFolder + "/MiningRuntime.prefab";
        private const string HealthBarPrefabPath =
            "Assets/Microlight/MicroBar/Prefabs/SimpleBars/Sprite_SimpleMicroBarSRP.prefab";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        private readonly struct OreSpec
        {
            public OreSpec(OreKind kind, string name, string description, int tier,
                int miningPower, int durability, int clickDamage, int sellValue,
                Color mapColor, string modelFile)
            {
                Kind = kind;
                Name = name;
                Description = description;
                Tier = tier;
                MiningPower = miningPower;
                Durability = durability;
                ClickDamage = clickDamage;
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
            public int ClickDamage { get; }
            public int SellValue { get; }
            public Color MapColor { get; }
            public string ModelFile { get; }
        }

        private static readonly OreSpec[] StarterOres =
        {
            new(OreKind.Stone, "Stone", "Common stone. The first material a miner can break.",
                1, 1, 10, 1, 1, new Color(0.48f, 0.52f, 0.56f), "stone_tier1.fbx"),
            new(OreKind.Coal, "Coal", "Dark fuel ore unlocked after basic stone mining.",
                2, 3, 20, 2, 4, new Color(0.10f, 0.12f, 0.14f), "coal_tier2.fbx"),
            new(OreKind.Copper, "Copper", "Valuable metallic ore used for stronger upgrades.",
                3, 6, 35, 3, 9, new Color(0.82f, 0.32f, 0.08f), "copper_tier3.fbx")
        };

        [MenuItem("Mining Simulator/Setup/Create or Update Starter Ores")]
        public static void CreateOrUpdateStarterOres()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(NpcPrefabFolder);
            EnsureFolder(SystemPrefabFolder);

            var dataAssets = new OreData[StarterOres.Length];

            for (int index = 0; index < StarterOres.Length; index++)
            {
                OreSpec spec = StarterOres[index];
                OreData data = CreateOrUpdateData(spec);
                dataAssets[index] = data;
                GameObject prefab = CreateOrUpdatePrefab(spec, data);
                if (prefab != null && data.Prefab == null)
                {
                    SetObjectReference(data, "prefab", prefab);
                }
            }

            MiningNpc npcPrefab = CreateOrUpdateNpcPrefab();
            CreateOrUpdateRuntimePrefab(dataAssets, npcPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Mining ore setup complete: {StarterOres.Length} independent OreData assets and prefabs.");
        }

        // Entry point used by Unity batch mode and CI.
        public static void CreateOrUpdateStarterOresBatch()
        {
            CreateOrUpdateStarterOres();
        }

        [MenuItem("Mining Simulator/Setup/Setup Complete Mining Gameplay")]
        public static void AddMiningRuntimeToActiveScene()
        {
            CreateOrUpdateStarterOres();
            Scene scene = SceneManager.GetActiveScene();
            AddRuntimeToSceneIfMissing(scene, registerUndo: true);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        public static void SetupSampleSceneBatch()
        {
            CreateOrUpdateStarterOres();
            Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            if (AddRuntimeToSceneIfMissing(scene, registerUndo: false))
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static OreData CreateOrUpdateData(OreSpec spec)
        {
            string path = $"{DataFolder}/{spec.Name}.asset";
            OreData existing = AssetDatabase.LoadAssetAtPath<OreData>(path);
            if (existing != null)
            {
                // Migration only: old assets predate clickDamage. Do not reset designer values.
                var existingSerialized = new SerializedObject(existing);
                SerializedProperty clickDamage = existingSerialized.FindProperty("clickDamage");
                if (clickDamage != null && clickDamage.intValue <= 0)
                {
                    clickDamage.intValue = spec.ClickDamage;
                    existingSerialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(existing);
                }
                return existing;
            }

            OreData data = ScriptableObject.CreateInstance<OreData>();
            AssetDatabase.CreateAsset(data, path);
            var serialized = new SerializedObject(data);
            serialized.FindProperty("kind").enumValueIndex = (int)spec.Kind;
            serialized.FindProperty("displayName").stringValue = spec.Name;
            serialized.FindProperty("description").stringValue = spec.Description;
            serialized.FindProperty("tier").intValue = spec.Tier;
            serialized.FindProperty("miningPowerRequired").intValue = spec.MiningPower;
            serialized.FindProperty("durability").intValue = spec.Durability;
            serialized.FindProperty("clickDamage").intValue = spec.ClickDamage;
            serialized.FindProperty("baseSellValue").intValue = spec.SellValue;
            serialized.FindProperty("mapColor").colorValue = spec.MapColor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject CreateOrUpdatePrefab(OreSpec spec, OreData data)
        {
            string prefabPath = $"{PrefabFolder}/{spec.Name}.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    Ore existingOre = contents.GetComponent<Ore>() ?? contents.AddComponent<Ore>();
                    if (existingOre.Data == null)
                    {
                        existingOre.SetData(data);
                    }
                    EnsureCollider(contents);
                    AddHealthBarIfMissing(contents, existingOre);
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
                return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

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
                AddHealthBarIfMissing(instance, ore);

                return PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static MiningNpc CreateOrUpdateNpcPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath);
            if (existing != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(NpcPrefabPath);
                try
                {
                    if (contents.GetComponent<MiningNpc>() == null)
                    {
                        contents.AddComponent<MiningNpc>();
                    }
                    PrefabUtility.SaveAsPrefabAsset(contents, NpcPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
                GameObject updated = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath);
                return updated != null ? updated.GetComponent<MiningNpc>() : null;
            }

            GameObject npcObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                npcObject.name = "Mining NPC";
                npcObject.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
                MiningNpc miningNpc = npcObject.AddComponent<MiningNpc>();

                GameObject helmet = GameObject.CreatePrimitive(PrimitiveType.Cube);
                helmet.name = "Miner Helmet";
                helmet.transform.SetParent(npcObject.transform, false);
                helmet.transform.localPosition = new Vector3(0f, 0.86f, 0.08f);
                helmet.transform.localScale = new Vector3(0.9f, 0.18f, 0.92f);
                UnityEngine.Object.DestroyImmediate(helmet.GetComponent<Collider>());

                GameObject pickaxeHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickaxeHandle.name = "Pickaxe Handle";
                pickaxeHandle.transform.SetParent(npcObject.transform, false);
                pickaxeHandle.transform.localPosition = new Vector3(0.65f, 0f, 0f);
                pickaxeHandle.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
                pickaxeHandle.transform.localScale = new Vector3(0.08f, 0.85f, 0.08f);
                UnityEngine.Object.DestroyImmediate(pickaxeHandle.GetComponent<Collider>());

                GameObject pickaxeHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickaxeHead.name = "Pickaxe Head";
                pickaxeHead.transform.SetParent(pickaxeHandle.transform, false);
                pickaxeHead.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                pickaxeHead.transform.localScale = new Vector3(3.8f, 0.18f, 0.65f);
                UnityEngine.Object.DestroyImmediate(pickaxeHead.GetComponent<Collider>());
                miningNpc.ConfigureTool(pickaxeHandle.transform);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(npcObject, NpcPrefabPath);
                return saved != null ? saved.GetComponent<MiningNpc>() : null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(npcObject);
            }
        }

        private static void CreateOrUpdateRuntimePrefab(OreData[] dataAssets, MiningNpc npcPrefab)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimePrefabPath);
            bool isNew = existing == null;
            GameObject runtime = isNew
                ? new GameObject("MiningRuntime")
                : PrefabUtility.LoadPrefabContents(RuntimePrefabPath);

            try
            {
                PlayerWallet wallet = runtime.GetComponent<PlayerWallet>() ?? runtime.AddComponent<PlayerWallet>();
                OreSpawner spawner = runtime.GetComponent<OreSpawner>() ?? runtime.AddComponent<OreSpawner>();
                if (runtime.GetComponent<OreClickInput>() == null)
                {
                    runtime.AddComponent<OreClickInput>();
                }
                NpcShop shop = runtime.GetComponent<NpcShop>() ?? runtime.AddComponent<NpcShop>();
                MiningHud hud = runtime.GetComponent<MiningHud>() ?? runtime.AddComponent<MiningHud>();
                if (runtime.GetComponent<MiningOrbitCamera>() == null)
                {
                    runtime.AddComponent<MiningOrbitCamera>();
                }

                var walletSerialized = new SerializedObject(wallet);
                SerializedProperty startingMoney = walletSerialized.FindProperty("startingMoney");
                if (startingMoney.intValue <= 0)
                {
                    startingMoney.intValue = 100;
                    walletSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                var serialized = new SerializedObject(spawner);
                SetReferenceIfMissing(serialized.FindProperty("wallet"), wallet);

                SerializedProperty pool = serialized.FindProperty("orePool");
                if (isNew || pool.arraySize == 0)
                {
                    pool.arraySize = dataAssets.Length;
                    float[] defaultWeights = { 60f, 30f, 10f };
                    for (int i = 0; i < dataAssets.Length; i++)
                    {
                        SerializedProperty entry = pool.GetArrayElementAtIndex(i);
                        entry.FindPropertyRelative("data").objectReferenceValue = dataAssets[i];
                        entry.FindPropertyRelative("weight").floatValue = defaultWeights[i];
                    }
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var shopSerialized = new SerializedObject(shop);
                SetReferenceIfMissing(shopSerialized.FindProperty("wallet"), wallet);
                SetReferenceIfMissing(shopSerialized.FindProperty("oreSpawner"), spawner);
                SetReferenceIfMissing(shopSerialized.FindProperty("npcPrefab"), npcPrefab);
                shopSerialized.ApplyModifiedPropertiesWithoutUndo();

                var hudSerialized = new SerializedObject(hud);
                SetReferenceIfMissing(hudSerialized.FindProperty("wallet"), wallet);
                SetReferenceIfMissing(hudSerialized.FindProperty("npcShop"), shop);
                hudSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(runtime, RuntimePrefabPath);
            }
            finally
            {
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(runtime);
                }
                else
                {
                    PrefabUtility.UnloadPrefabContents(runtime);
                }
            }
        }

        private static void AddHealthBarIfMissing(GameObject root, Ore ore)
        {
            if (root.GetComponentInChildren<OreHealthBar>(true) != null)
            {
                return;
            }

            GameObject barPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath);
            if (barPrefab == null)
            {
                Debug.LogError($"MicroBar prefab is missing at {HealthBarPrefabPath}.");
                return;
            }

            GameObject barObject = PrefabUtility.InstantiatePrefab(barPrefab, root.transform) as GameObject;
            if (barObject == null)
            {
                Debug.LogError($"Could not add a MicroBar to ore prefab '{root.name}'.");
                return;
            }

            barObject.name = "Ore Health Bar";
            barObject.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            barObject.transform.localRotation = Quaternion.identity;
            barObject.transform.localScale = Vector3.one * 0.65f;
            MicroBar bar = barObject.GetComponent<MicroBar>();
            OreHealthBar binding = barObject.AddComponent<OreHealthBar>();
            binding.Configure(ore, bar, barObject.transform);
        }

        private static void SetReferenceIfMissing(SerializedProperty property, UnityEngine.Object value)
        {
            if (property != null && property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static bool AddRuntimeToSceneIfMissing(Scene scene, bool registerUndo)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<OreSpawner>(true) != null)
                {
                    Debug.Log("MiningRuntime already exists in the active scene. No changes made.");
                    return false;
                }
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Mining runtime prefab is missing at {RuntimePrefabPath}.");
                return false;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError("Could not instantiate MiningRuntime in the active scene.");
                return false;
            }

            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (registerUndo)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Add Mining Runtime");
            }
            return true;
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
