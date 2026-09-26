#if UNITY_EDITOR
using System.IO;
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
        private static bool previewAfterBake;

        [MenuItem(MenuRoot + "0. Set Up, Bake And Show In Scene")]
        private static void SetUpBakeAndShow()
        {
            if (!TryGetEditableScene(out Scene scene)) return;
            if (!PrepareScene(scene)) return;
            BakeScene(scene, true);
        }

        [MenuItem(MenuRoot + "1. Prepare Open Scene")]
        private static void Prepare()
        {
            if (!TryGetEditableScene(out Scene scene)) return;
            PrepareScene(scene);
        }

        private static bool PrepareScene(Scene scene)
        {
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
                return false;
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
            bool newArea = areaObject == null;
            if (newArea)
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
            if (newArea)
            {
                // Once authored, the area remains editable in the Scene and is
                // never moved or resized by running this setup menu again.
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
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = areaObject;
            Debug.Log("Occlusion Area is now an editable object in the scene. Mark any large permanent walls/rocks with menu 2, then bake with menu 3. Menu 0 prepares and bakes in one step.", areaObject);
            return true;
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
            BakeScene(scene, false);
        }

        private static void BakeScene(Scene scene, bool preview)
        {
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
            previewAfterBake = preview;
            if (!StaticOcclusionCulling.Compute())
            {
                bakingScenePath = null;
                previewAfterBake = false;
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
            bool preview = previewAfterBake;
            previewAfterBake = false;
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
            if (preview) ShowInScene(scene);
        }

        [MenuItem(MenuRoot + "5. Show Baked Occlusion In Scene")]
        private static void ShowInSceneMenu()
        {
            if (!TryGetEditableScene(out Scene scene)) return;
            ShowInScene(scene);
        }

        private static void ShowInScene(Scene scene)
        {
            // A previous bake's displayed size can outlive the scene it belonged to.
            // Confirm that THIS saved scene has a baked data reference first.
            if (string.IsNullOrEmpty(scene.path) || !File.Exists(scene.path) ||
                !HasBakedData(scene.path))
            {
                Debug.LogWarning("This scene does not reference baked occlusion data. Exit Play Mode, run '0. Set Up, Bake And Show In Scene', then save the scene.");
                return;
            }

            Camera mainCamera = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    if (camera.CompareTag("MainCamera")) { mainCamera = camera; break; }
            if (mainCamera == null)
            {
                Debug.LogWarning("Could not find the Main Camera in the open scene.");
                return;
            }

            EditorApplication.ExecuteMenuItem("Window/Rendering/Occlusion Culling");
            Selection.activeGameObject = mainCamera.gameObject;
            StaticOcclusionCullingVisualization.showOcclusionCulling = true;
            StaticOcclusionCullingVisualization.showGeometryCulling = true;
            StaticOcclusionCullingVisualization.showViewVolumes = true;
            StaticOcclusionCullingVisualization.showVisibilityLines = true;
            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.AlignViewToObject(mainCamera.transform);
                view.Repaint();
            }
            Debug.Log("Select the Visualization tab in the Occlusion window. Scene view now follows the selected Main Camera; move it behind a stationary wall or hill to check which renderers disappear.", mainCamera);
        }

        private static bool HasBakedData(string scenePath)
        {
            using StreamReader reader = new(scenePath);
            for (int i = 0; i < 32 && reader.ReadLine() is { } line; i++)
                if (line.TrimStart().StartsWith("m_OcclusionCullingData:"))
                    return !line.Contains("{fileID: 0}");
            return false;
        }

        [MenuItem(MenuRoot + "4. Cancel Running Bake")]
        private static void CancelBake()
        {
            if (!StaticOcclusionCulling.isRunning) return;
            EditorApplication.update -= SaveWhenFinished;
            bakingScenePath = null;
            previewAfterBake = false;
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
