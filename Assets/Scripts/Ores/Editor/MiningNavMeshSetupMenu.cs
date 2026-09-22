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
    /// 3. Marks ore/lucky-block hierarchies Ignore From Build and configures compact circular
    ///    carving obstacles, so paths stay on the ground and route tightly around mineables.
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

            changes += SetupUndergroundSurface(builder);

            if (changes > 0)
            {
                EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
            }

            return changes;
        }

        /// <summary>
        /// Adds navigation only to the user's existing UnderGround root. It never creates or
        /// duplicates the platform, ore spawner, NPC, or wandering trader.
        /// </summary>
        private static int SetupUndergroundSurface(MiningNavMeshBuilder builder)
        {
            GameObject undergroundRoot = FindUnderGroundRoot();
            if (undergroundRoot == null)
            {
                Debug.LogWarning("[NavMesh Setup] No scene root named 'UnderGround' was found. " +
                                 "Your existing underground objects were not changed.");
                return 0;
            }

            int changes = 0;
            NavMeshSurface undergroundSurface =
                undergroundRoot.GetComponent<NavMeshSurface>();
            if (undergroundSurface == null)
            {
                undergroundSurface = Undo.AddComponent<NavMeshSurface>(undergroundRoot);
                changes++;
            }

            Undo.RecordObject(undergroundSurface, "Configure Underground NavMeshSurface");
            if (!undergroundSurface.enabled)
            {
                undergroundSurface.enabled = true;
                changes++;
            }
            if (undergroundSurface.collectObjects != CollectObjects.Children)
            {
                undergroundSurface.collectObjects = CollectObjects.Children;
                changes++;
            }
            if (undergroundSurface.useGeometry != NavMeshCollectGeometry.PhysicsColliders)
            {
                undergroundSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                changes++;
            }
            if (undergroundSurface.layerMask.value != ~0)
            {
                undergroundSurface.layerMask = ~0;
                changes++;
            }
            EditorUtility.SetDirty(undergroundSurface);

            var builderSerialized = new SerializedObject(builder);
            SerializedProperty undergroundSurfaceProperty =
                builderSerialized.FindProperty("undergroundSurface");
            if (undergroundSurfaceProperty != null &&
                undergroundSurfaceProperty.objectReferenceValue != undergroundSurface)
            {
                undergroundSurfaceProperty.objectReferenceValue = undergroundSurface;
                builderSerialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(builder);
                changes++;
            }

            // Bake once now so duplicated underground miners can move immediately in Play Mode.
            // Temporarily revealing an inactive root is not recorded and its authored state is
            // restored before this setup method returns.
            bool wasActive = undergroundRoot.activeSelf;
            if (!wasActive) undergroundRoot.SetActive(true);
            undergroundSurface.BuildNavMesh();
            if (!wasActive) undergroundRoot.SetActive(false);

            EditorSceneManager.MarkSceneDirty(undergroundRoot.scene);
            Debug.Log("[NavMesh Setup] Underground NavMeshSurface configured on existing root: " +
                      undergroundRoot.name, undergroundRoot);
            return changes;
        }

        private static GameObject FindUnderGroundRoot()
        {
            foreach (Transform candidate in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate == null || candidate.parent != null ||
                    !candidate.gameObject.scene.IsValid())
                {
                    continue;
                }

                string compact = candidate.name.Replace(" ", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("-", string.Empty);
                if (string.Equals(compact, "underground",
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Scans all prefabs in the ore folder, excludes each mineable hierarchy from source
        /// collection, and configures one compact circular carving obstacle.
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

                GameObject target = isOre
                    ? prefab.GetComponentInChildren<Ore>(true).gameObject
                    : prefab.GetComponentInChildren<LuckyBlock>(true).gameObject;
                bool changed = false;

                NavMeshModifier modifier = target.GetComponent<NavMeshModifier>();
                if (modifier == null)
                {
                    modifier = target.AddComponent<NavMeshModifier>();
                    changed = true;
                }

                if (!modifier.enabled || !modifier.ignoreFromBuild || modifier.overrideArea)
                {
                    modifier.enabled = true;
                    modifier.overrideArea = false;
                    modifier.ignoreFromBuild = true;
                    EditorUtility.SetDirty(modifier);
                    changed = true;
                }

                MiningNavMeshObstacle circularObstacle =
                    target.GetComponent<MiningNavMeshObstacle>();
                if (circularObstacle == null)
                {
                    circularObstacle = target.AddComponent<MiningNavMeshObstacle>();
                    changed = true;
                }

                foreach (NavMeshObstacle obstacle in
                         prefab.GetComponentsInChildren<NavMeshObstacle>(true))
                {
                    obstacle.carving = false;
                    obstacle.enabled = false;
                    EditorUtility.SetDirty(obstacle);
                }

                circularObstacle.EnableCircularCarving();
                EditorUtility.SetDirty(circularObstacle);
                changed = true;

                if (!changed)
                {
                    continue;
                }

                PrefabUtility.SavePrefabAsset(prefab);
                updated++;
                Debug.Log($"[NavMesh Setup] Configured circular mineable obstacle: {path}");
            }

            return updated;
        }
    }
}
#endif
