#if UNITY_EDITOR
using System.Linq;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>
    /// Swaps every Ore prefab's click-detection collider from BoxCollider to MeshCollider so it
    /// hugs the actual rock shape instead of its bounding box - useful both for tighter click
    /// hit-testing and as an obstacle shape for NPC pathfinding. Ore has no Rigidbody, so a
    /// non-convex MeshCollider is safe here (unlike the NPC's CapsuleCollider, which must stay a
    /// primitive because it sits on a non-kinematic Rigidbody). Re-running this only touches
    /// prefabs that still have a BoxCollider, so it never re-processes already-converted ores.
    /// </summary>
    public static class MiningOreColliderSetupMenu
    {
        private const string OrePrefabFolder = "Assets/Prefabs/Ores";

        [MenuItem("Mining Simulator/Setup/Convert Ore Colliders To Mesh")]
        public static void ConvertOreCollidersToMesh()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { OrePrefabFolder });
            int converted = 0;
            int skippedNoBox = 0;
            int skippedNoMesh = 0;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Ore ore = contents.GetComponentInChildren<Ore>(true);
                    if (ore == null)
                    {
                        continue; // Not an Ore prefab (e.g. the lucky block variants) - leave alone.
                    }

                    GameObject oreObject = ore.gameObject;
                    BoxCollider box = oreObject.GetComponent<BoxCollider>();
                    if (box == null)
                    {
                        skippedNoBox++;
                        continue; // Already converted (or never had one) - idempotent, do nothing.
                    }

                    MeshFilter meshFilter = oreObject.GetComponent<MeshFilter>()
                        ?? oreObject.GetComponentInChildren<MeshFilter>();
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                    {
                        skippedNoMesh++;
                        Debug.LogWarning($"Mining Ore Collider Setup: '{path}' has no MeshFilter " +
                            "to source a collision mesh from - left its BoxCollider untouched.");
                        continue;
                    }

                    bool wasTrigger = box.isTrigger;
                    PhysicsMaterial sharedMaterial = box.sharedMaterial;
                    Object.DestroyImmediate(box, true);

                    MeshCollider meshCollider = oreObject.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = meshFilter.sharedMesh;
                    meshCollider.convex = false; // No Rigidbody on Ore, so a concave mesh is fine.
                    meshCollider.isTrigger = wasTrigger;
                    meshCollider.sharedMaterial = sharedMaterial;

                    converted++;
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            EditorUtility.DisplayDialog("Ore Collider Setup",
                $"Converted {converted} ore prefab(s) from BoxCollider to MeshCollider.\n" +
                $"Already converted / no BoxCollider: {skippedNoBox}\n" +
                $"Skipped (no mesh found - check the Console): {skippedNoMesh}", "OK");
        }
    }
}
#endif
