#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>
    /// Places the Portal visual (copied from the Tower Defense project's
    /// Assets/Art/Models/Portal.prefab, materials, shader graph, and textures unchanged) as a
    /// clickable gate in the open scene. Reuses the existing OreClickInput raycast for the click
    /// and the existing HUD Canvas for the notice panel - creates only the two new objects below,
    /// and re-running this is always safe (it reuses the same named objects instead of
    /// duplicating them or touching anything else in the scene).
    /// </summary>
    public static class MiningPortalSetupMenu
    {
        private const string PortalPrefabPath = "Assets/Prefabs/Portal/Portal.prefab";
        private const string HudCanvasName = "Mining HUD Canvas";
        private const string GateObjectName = "Mining Portal Gate";
        private const float GateColliderRadius = 1.5f;

        [MenuItem("Mining Simulator/Setup/Create Portal Gate")]
        public static void CreatePortalGate()
        {
            GameObject portalVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
            if (portalVisualPrefab == null)
            {
                EditorUtility.DisplayDialog("Portal Gate Setup",
                    $"Couldn't find the Portal prefab at:\n{PortalPrefabPath}\n\n" +
                    "Make sure the Portal.prefab and its materials/shader/textures copied from " +
                    "the Tower Defense project are in that folder, then run this again.", "OK");
                return;
            }

            MiningPortalMaintenancePanel panel = EnsureMaintenancePanelExists();
            bool panelReady = panel != null;

            GameObject gate = GameObject.Find(GateObjectName);
            bool isNewGate = gate == null;
            if (isNewGate)
            {
                gate = (GameObject)PrefabUtility.InstantiatePrefab(portalVisualPrefab);
                gate.name = GateObjectName;
                gate.transform.position = Vector3.zero;
            }

            SphereCollider gateCollider = gate.GetComponent<SphereCollider>() ??
                                          gate.AddComponent<SphereCollider>();
            // Must stay a non-trigger collider - OreClickInput's raycast uses
            // QueryTriggerInteraction.Ignore, the same as every Ore collider, so a trigger here
            // would silently make the gate unclickable.
            gateCollider.isTrigger = false;
            gateCollider.radius = GateColliderRadius;

            if (gate.GetComponent<MiningPortalGate>() == null)
            {
                gate.AddComponent<MiningPortalGate>();
            }

            EditorUtility.SetDirty(gate);
            EditorSceneManager.MarkSceneDirty(gate.scene);

            string positionNote = isNewGate
                ? "\n\nIt was placed at the world origin (0,0,0) - drag it to wherever you want " +
                  "the gate in the scene; re-running this tool will not move it again."
                : $"\n\nReused the existing '{GateObjectName}' object already in the scene.";
            string panelNote = panelReady
                ? "The 'ĐANG SỬA CHỮA' notice panel is ready on the HUD canvas."
                : "Couldn't find a Canvas in the open scene yet to add the notice panel to - " +
                  "run the scene's gameplay setup first, then re-run this command.";

            EditorUtility.DisplayDialog("Portal Gate Setup",
                $"Created the clickable Portal gate. {panelNote}{positionNote}", "OK");

            if (panel != null)
            {
                // Select the real Scene object so the designer can immediately edit its
                // RectTransforms, Images and TMP labels in the Inspector.
                Selection.activeGameObject = panel.gameObject;
                EditorGUIUtility.PingObject(panel.gameObject);
            }
        }

        [MenuItem("Mining Simulator/Setup/Show Portal Panel For Editing")]
        public static void ShowPortalPanelForEditing()
        {
            MiningPortalMaintenancePanel panel = EnsureMaintenancePanelExists();
            if (panel == null)
            {
                EditorUtility.DisplayDialog("Portal Gate Setup",
                    "Couldn't find the Mining HUD Canvas in the open scene.", "OK");
                return;
            }

            panel.SetEditorPreviewVisible(true);
            EditorUtility.SetDirty(panel.gameObject);
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            EditorGUIUtility.PingObject(panel.gameObject);
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        private static MiningPortalMaintenancePanel EnsureMaintenancePanelExists()
        {
            Canvas canvas = FindHudCanvas();
            if (canvas == null)
            {
                return null;
            }

            MiningPortalMaintenancePanel panel = MiningPortalMaintenancePanel.EnsureRuntime(canvas);
            if (panel != null)
            {
                panel.SetEditorPreviewVisible(true);
                EditorUtility.SetDirty(panel);
                EditorUtility.SetDirty(panel.gameObject);
                EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            }

            return panel;
        }

        private static Canvas FindHudCanvas()
        {
            OreClickInput sceneClickInput =
                Object.FindFirstObjectByType<OreClickInput>(FindObjectsInactive.Include);
            if (sceneClickInput != null)
            {
                Transform canvasTransform = sceneClickInput.transform.Find(HudCanvasName);
                if (canvasTransform != null)
                {
                    Canvas namedCanvas = canvasTransform.GetComponent<Canvas>();
                    if (namedCanvas != null)
                    {
                        return namedCanvas;
                    }
                }
            }

            return Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        }
    }
}
#endif
