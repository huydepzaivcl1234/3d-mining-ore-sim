#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>Adds the coin-machine behavior to the existing Computer1 prefab without rebuilding it.</summary>
    public static class MiningComputerSetupMenu
    {
        private const string ComputerPrefabPath =
            "Assets/80sComputerandGamingSetup/Prefabs/Computer1.prefab";
        private const string ComputerDataFolder = "Assets/GameData/Computer";
        private const string ComputerDataPath = ComputerDataFolder + "/MiningComputerData.asset";
        private const string RewardPopupPath = "Assets/Prefabs/UI/OreRewardPopup.prefab";
        private const string UiDataPath = "Assets/GameData/UI/MiningUiData.asset";

        [MenuItem("Mining Simulator/Setup/Configure Computer 1 Coin Machine")]
        public static void ConfigureComputerOne()
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ComputerPrefabPath);
            if (prefabAsset == null)
            {
                EditorUtility.DisplayDialog("Computer 1 Setup",
                    "Computer1.prefab was not found at the expected path.", "OK");
                return;
            }

            MiningComputerData computerData = LoadOrCreateData();
            OreRewardPopup popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                RewardPopupPath)?.GetComponent<OreRewardPopup>();
            MiningUiData uiData = AssetDatabase.LoadAssetAtPath<MiningUiData>(UiDataPath);

            GameObject root = PrefabUtility.LoadPrefabContents(ComputerPrefabPath);
            try
            {
                MiningComputerStation station = root.GetComponent<MiningComputerStation>();
                if (station == null)
                {
                    station = root.AddComponent<MiningComputerStation>();
                }
                station.ConfigureIfMissing(computerData, popupPrefab, uiData);

                Collider collider = root.GetComponent<Collider>();
                if (collider == null)
                {
                    BoxCollider box = root.AddComponent<BoxCollider>();
                    FitColliderToRenderers(root.transform, box);
                }

                EditorUtility.SetDirty(station);
                PrefabUtility.SaveAsPrefabAsset(root, ComputerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            MiningInteractionSetupMenu.EnsureInOpenScene(false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ComputerPrefabPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            EditorUtility.DisplayDialog("Computer 1 Setup",
                "Computer1 is ready. Before purchase it is a ghost; hover and press F to buy. " +
                "Edit prices, coin ticks and animation in Assets/GameData/Computer/" +
                "MiningComputerData.asset.", "OK");
        }

        private static MiningComputerData LoadOrCreateData()
        {
            MiningComputerData existing = AssetDatabase.LoadAssetAtPath<MiningComputerData>(
                ComputerDataPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder("Assets", "GameData");
            EnsureFolder("Assets/GameData", "Computer");
            MiningComputerData created = ScriptableObject.CreateInstance<MiningComputerData>();
            AssetDatabase.CreateAsset(created, ComputerDataPath);
            return created;
        }

        private static void FitColliderToRenderers(Transform root, BoxCollider box)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool initialized = false;
            Bounds localBounds = default;
            foreach (Renderer renderer in renderers)
            {
                Bounds worldBounds = renderer.bounds;
                Vector3 center = worldBounds.center;
                Vector3 extents = worldBounds.extents;
                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 worldCorner = center + Vector3.Scale(extents,
                                new Vector3(x, y, z));
                            Vector3 localCorner = root.InverseTransformPoint(worldCorner);
                            if (!initialized)
                            {
                                localBounds = new Bounds(localCorner, Vector3.zero);
                                initialized = true;
                            }
                            else
                            {
                                localBounds.Encapsulate(localCorner);
                            }
                        }
                    }
                }
            }

            box.center = initialized ? localBounds.center : Vector3.up * 0.5f;
            box.size = initialized
                ? Vector3.Max(localBounds.size, Vector3.one * 0.05f)
                : Vector3.one;
            box.isTrigger = false;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
