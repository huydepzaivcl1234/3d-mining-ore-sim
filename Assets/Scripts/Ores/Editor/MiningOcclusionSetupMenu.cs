#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Editor
{
    /// <summary>Prepare and bake scene-authored occlusion without modifying the source scene in a ZIP.</summary>
    public static class MiningOcclusionSetupMenu
    {
        private const string AreaName = "Mining Occlusion Area";
        private const string MenuRoot = "Mining Simulator/Optimization/Occlusion Culling/";
        private static string bakingScenePath;

        [MenuItem(MenuRoot + "1. Prepare Open Scene")]
        private static void Prepare()
        {
            if (!TryGetEditableScene(out Scene scene)) return;

            Camera gameCamera = null;
            Terrain terrain = null;
            OreSpawner spawner = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (gameCamera == null)
                    foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                        if (camera.CompareTag("MainCamera")) { gameCamera = camera; break; }
                if (terrain == null)
                    terrain = root.GetComponentInChildren<Terrain>(true);
                if (spawner == null)
                    spawner = root.GetComponentInChildren<OreSpawner>(true);
            }
            if (gameCamera == null || terrain == null)
            {
                Debug.LogError("Occlusion setup needs the play scene with Main Camera and Ground/Terrain.");
                return;
            }

            Undo.RecordObject(gameCamera, "Enable main camera occlusion");
            gameCamera.useOcclusionCulling = true;
            EditorUtility.SetDirty(gameCamera);

            // The scene's Terrain already has all static flags. Preserve its other flags
            // while ensuring it can occlude the cave/world geometry behind it.
            StaticEditorFlags terrainFlags = GameObjectUtility.GetStaticEditorFlags(terrain.gameObject);
            StaticEditorFlags wanted = terrainFlags | StaticEditorFlags.OccluderStatic |
                                       StaticEditorFlags.OccludeeStatic;
            if (terrainFlags != wanted)
            {
                Undo.RecordObject(terrain.gameObject, "Mark stationary terrain for occlusion");
                GameObjectUtility.SetStaticEditorFlags(terrain.gameObject, wanted);
            }

            GameObject areaObject = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == AreaName) { areaObject = root; break; }
            if (areaObject == null)
            {
                areaObject = new GameObject(AreaName);
                SceneManager.MoveGameObjectToScene(areaObject, scene);
                Undo.RegisterCreatedObjectUndo(areaObject, "Create mining occlusion area");
            }

            OcclusionArea area = areaObject.GetComponent<OcclusionArea>() ??
                                 Undo.AddComponent<OcclusionArea>(areaObject);
            MiningGameData gameData = AssetDatabase.LoadAssetAtPath<MiningGameData>(
                "Assets/GameData/Game/MiningGameData.asset");
            float reach = gameData != null ? gameData.CameraMaximumDistance : 45f;
            Vector3 center = spawner != null && spawner.SpawnData != null
                ? spawner.SpawnAreaCenter : Vector3.zero;
            Vector3 size = spawner != null && spawner.SpawnData != null
                ? spawner.SpawnAreaSize : new Vector3(20f, 0f, 20f);
            Undo.RecordObject(areaObject.transform, "Fit mining occlusion area");
            Undo.RecordObject(area, "Fit mining occlusion area");
            areaObject.transform.position = center + Vector3.up * 10f;
            areaObject.transform.rotation = Quaternion.identity;
            areaObject.transform.localScale = Vector3.one;
            area.center = Vector3.zero;
            area.size = new Vector3(
                Mathf.Max(64f, Mathf.Abs(size.x) + reach * 2f + 20f),
                Mathf.Max(60f, reach * 2f + 20f),
                Mathf.Max(64f, Mathf.Abs(size.z) + reach * 2f + 20f));

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = areaObject;
            Debug.Log("Occlusion prepared on the existing camera and terrain. Area follows the ore spawn bounds and maximum camera zoom. Mark large permanent walls/rocks using menu 2, then run menu 3 to bake and save the scene.", areaObject);
        }

        [MenuItem(MenuRoot + "2. Mark Selected Permanent Walls Or Rocks")]
        private static void MarkSelected()
        {
            if (!TryGetEditableScene(out Scene scene)) return;
            int marked = 0;
            foreach (GameObject selected in Selection.gameObjects)
            {
                if (selected.scene != scene) continue;
                foreach (MeshRenderer renderer in selected.GetComponentsInChildren<MeshRenderer>(true))
                {
                    GameObject candidate = renderer.gameObject;
                    if (candidate.GetComponent<MeshFilter>() == null ||
                        candidate.GetComponentInParent<Rigidbody>(true) != null ||
                        candidate.GetComponentInParent<Animator>(true) != null ||
                        candidate.GetComponentInParent<Ore>(true) != null ||
                        candidate.GetComponentInParent<MiningChest>(true) != null ||
                        candidate.GetComponentInParent<LuckyBlock>(true) != null ||
                        candidate.GetComponentInParent<MiningNpc>(true) != null ||
                        renderer.bounds.size.magnitude < 4f || !Opaque(renderer))
                        continue;

                    StaticEditorFlags before = GameObjectUtility.GetStaticEditorFlags(candidate);
                    StaticEditorFlags after = before | StaticEditorFlags.OccluderStatic |
                                              StaticEditorFlags.OccludeeStatic;
                    if (after == before) continue;
                    Undo.RecordObject(candidate, "Mark permanent occlusion geometry");
                    GameObjectUtility.SetStaticEditorFlags(candidate, after);
                    marked++;
                }
            }
            if (marked > 0) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Occlusion: marked {marked} large opaque, stationary meshes. Moving ores, chests, miners and transparent geometry were skipped.");
        }

        private static bool Opaque(Renderer renderer)
        {
            foreach (Material material in renderer.sharedMaterials)
                if (material == null || material.renderQueue > 2000)
                    return false;
            return renderer.sharedMaterials.Length > 0;
        }

        [MenuItem(MenuRoot + "3. Bake And Save Open Scene")]
        private static void Bake()
        {
            if (!TryGetEditableScene(out Scene scene)) return;
            if (StaticOcclusionCulling.isRunning)
            {
                Debug.LogWarning("An occlusion bake is already running.");
                return;
            }
            if (string.IsNullOrEmpty(scene.path) || !EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError("Save the open scene before baking occlusion.");
                return;
            }

            bakingScenePath = scene.path;
            if (!StaticOcclusionCulling.Compute())
            {
                bakingScenePath = null;
                Debug.LogError("Unity could not start the occlusion bake. Check the Occlusion Culling window and Console.");
                return;
            }
            EditorApplication.update -= SaveWhenFinished;
            EditorApplication.update += SaveWhenFinished;
            Debug.Log("Occlusion bake started. Unity will save the scene's baked data reference when it finishes.");
        }

        private static void SaveWhenFinished()
        {
            if (StaticOcclusionCulling.isRunning) return;
            EditorApplication.update -= SaveWhenFinished;
            Scene scene = SceneManager.GetSceneByPath(bakingScenePath);
            bakingScenePath = null;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("Occlusion bake ended after the scene was closed. Reopen the scene and save it.");
                return;
            }
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError("Occlusion bake ended, but Unity could not save the scene reference.");
                return;
            }
            Debug.Log($"Occlusion bake finished. Data size: {StaticOcclusionCulling.umbraDataSize / 1024f:0.#} KiB. Inspect the Occlusion Culling Visualization tab and compare Game view Stats before/after.");
        }

        [MenuItem(MenuRoot + "4. Cancel Running Bake")]
        private static void CancelBake()
        {
            if (!StaticOcclusionCulling.isRunning) return;
            EditorApplication.update -= SaveWhenFinished;
            bakingScenePath = null;
            StaticOcclusionCulling.Cancel();
            Debug.Log("Occlusion bake cancelled; the previous baked data is unchanged.");
        }

        private static bool TryGetEditableScene(out Scene scene)
        {
            scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("Open the gameplay scene and exit Play Mode first.");
                return false;
            }
            return true;
        }
    }
}
#endif
