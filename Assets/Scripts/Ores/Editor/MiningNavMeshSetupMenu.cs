#if UNITY_EDITOR
using MiningSimulator.Ores;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace MiningSimulator.Editor
{
    /// <summary>
    /// One-click setup that activates the NavMesh pathfinding stack so miners can route
    /// around ore clusters instead of relying on local reactive steering alone.
    ///
    /// What it does:
    /// 1. Enables the existing <see cref="MiningNavMeshBuilder"/> and <see cref="NavMeshSurface"/>
    ///    components on the "Navmeshsuface" GameObject in the active scene.
    /// 2. Fixes the surface's layer mask to include both Default (0) and Ground (3) so the
    ///    floor Plane is picked up by the bake regardless of which layer it lives on.
    /// 3. Adds <see cref="MiningNavMeshObstacle"/> (+ the required <see cref="NavMeshObstacle"/>)
    ///    to every ore and lucky-block prefab in Assets/Prefabs/Ores so they carve holes in the
    ///    NavMesh at runtime.
    /// </summary>
    public static class MiningNavMeshSetupMenu
    {
        private const string OrePrefabFolder = "Assets/Prefabs/Ores";

        [MenuItem("Mining Simulator/Setup/Enable NavMesh Pathfinding")]
        public static void EnableNavMeshPathfinding()
        {
            int sceneChanges = SetupSceneComponents();
            int prefabChanges = SetupOrePrefabs();

            if (sceneChanges > 0 || prefabChanges > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[NavMesh Setup] Done. Scene changes: {sceneChanges}, prefabs updated: {prefabChanges}. " +
                      "Press Ctrl+S to save the scene, then enter Play mode to test.");
        }

        /// <summary>
        /// Finds the "Navmeshsuface" GameObject, enables its components, and fixes the layer mask.
        /// </summary>
        private static int SetupSceneComponents()
        {
            int changes = 0;

            // --- MiningNavMeshBuilder ---
            MiningNavMeshBuilder builder = Object.FindFirstObjectByType<MiningNavMeshBuilder>(
                FindObjectsInactive.Include);

            if (builder == null)
            {
                Debug.LogError("[NavMesh Setup] Could not find MiningNavMeshBuilder in the active scene. " +
                               "Make sure 'Navmeshsuface' GameObject exists.");
                return 0;
            }

            if (!builder.enabled)
            {
                Undo.RecordObject(builder, "Enable MiningNavMeshBuilder");
                builder.enabled = true;
                EditorUtility.SetDirty(builder);
                changes++;
                Debug.Log("[NavMesh Setup] Enabled MiningNavMeshBuilder.");
            }

            // --- NavMeshSurface ---
            NavMeshSurface surface = builder.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                Debug.LogError("[NavMesh Setup] NavMeshSurface component missing on the MiningNavMeshBuilder GameObject.");
                return changes;
            }

            if (!surface.enabled)
            {
                Undo.RecordObject(surface, "Enable NavMeshSurface");
                surface.enabled = true;
                EditorUtility.SetDirty(surface);
                changes++;
                Debug.Log("[NavMesh Setup] Enabled NavMeshSurface.");
            }

            // --- Fix layer mask ---
            // The floor "Plane" is on Layer 0 (Default). The surface currently only includes
            // Layer 3 (Ground) with m_Bits = 8. We need both: bits 0b1001 = 9.
            int defaultBit = 1 << 0; // Layer 0 - Default
            int groundBit = 1 << 3;  // Layer 3 - Ground
            int requiredMask = defaultBit | groundBit;

            // Read the current mask via SerializedObject to be safe.
            var serialized = new SerializedObject(surface);
            SerializedProperty layerMaskProp = serialized.FindProperty("m_LayerMask");
            int currentMask = layerMaskProp.intValue;

            if ((currentMask & requiredMask) != requiredMask)
            {
                layerMaskProp.intValue = currentMask | requiredMask;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(surface);
                changes++;
                Debug.Log($"[NavMesh Setup] Fixed NavMeshSurface LayerMask: {currentMask} → {currentMask | requiredMask} " +
                          "(added Default + Ground layers).");
            }

            if (changes > 0)
            {
                EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
            }

            return changes;
        }

        /// <summary>
        /// Scans all prefabs in the ore folder and adds MiningNavMeshObstacle where missing.
        /// </summary>
        private static int SetupOrePrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { OrePrefabFolder });
            int updated = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                // Only process prefabs that are ores or lucky blocks.
                bool isOre = prefab.GetComponentInChildren<Ore>(true) != null;
                bool isLuckyBlock = prefab.GetComponentInChildren<LuckyBlock>(true) != null;
                if (!isOre && !isLuckyBlock)
                {
                    continue;
                }

                // Skip if already set up.
                if (prefab.GetComponentInChildren<MiningNavMeshObstacle>(true) != null)
                {
                    continue;
                }

                // Find the root GameObject that has the Ore or LuckyBlock component - that's
                // where the colliders live and where the obstacle should sit.
                GameObject target = prefab;
                Ore oreComponent = prefab.GetComponentInChildren<Ore>(true);
                LuckyBlock luckyComponent = prefab.GetComponentInChildren<LuckyBlock>(true);
                if (oreComponent != null)
                {
                    target = oreComponent.gameObject;
                }
                else if (luckyComponent != null)
                {
                    target = luckyComponent.gameObject;
                }

                // Add components. NavMeshObstacle is added automatically via [RequireComponent]
                // on MiningNavMeshObstacle, but we add it explicitly for clarity.
                if (target.GetComponent<NavMeshObstacle>() == null)
                {
                    target.AddComponent<NavMeshObstacle>();
                }

                target.AddComponent<MiningNavMeshObstacle>();

                // OnValidate in MiningNavMeshObstacle will set carving=true, shape=Box,
                // carveOnlyStationary=true automatically.

                PrefabUtility.SavePrefabAsset(prefab);
                updated++;
                Debug.Log($"[NavMesh Setup] Added MiningNavMeshObstacle to: {path}");
            }

            return updated;
        }
    }
}
#endif
