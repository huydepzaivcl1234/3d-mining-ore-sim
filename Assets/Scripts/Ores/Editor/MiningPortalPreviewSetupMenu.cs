#if UNITY_EDITOR
using MiningSimulator.Ores;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Editor
{
    /// <summary>Authors an editable confirmation panel and wires an existing gate in the open scene.</summary>
    public static class MiningPortalPreviewSetupMenu
    {
        private const string CanvasName = "Mining HUD Canvas";
        private const string PanelName = "Portal Preview Panel";
        private const string ShaderPath = "Assets/Shaders/DynamicRadialMaskTransition.shader";

        [MenuItem("Mining Simulator/Portal/Setup Enter Preview On Selected Gate")]
        private static void Setup()
        {
            GameObject gate = Selection.activeGameObject;
            if (gate == null || !gate.scene.IsValid())
            {
                EditorUtility.DisplayDialog("Portal preview", "Select the existing Mining Portal Gate in the scene Hierarchy.", "OK");
                return;
            }

            Canvas canvas = FindCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Portal preview", "Mining HUD Canvas was not found in the open scene. Select or create your scene Canvas first.", "OK");
                return;
            }

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Portal preview", "DynamicRadialMaskTransition.shader was not found. Import your radial transition shader first.", "OK");
                return;
            }

            // Reuse the existing interaction prompt, data, and Input System configuration.
            if (!MiningInteractionSetupMenu.EnsureInOpenScene(false))
            {
                EditorUtility.DisplayDialog("Portal preview", "Could not configure the existing interaction prompt.", "OK");
                return;
            }

            Transform existingPanel = canvas.transform.Find(PanelName);
            MiningPortalPreviewPanel panel = existingPanel != null
                ? existingPanel.GetComponent<MiningPortalPreviewPanel>()
                : null;
            if (panel == null && existingPanel != null)
            {
                EditorUtility.DisplayDialog("Portal preview", "An unrelated UI object already uses the name '" + PanelName + "'. Rename it before running setup.", "OK");
                return;
            }
            if (panel == null) panel = CreatePanel(canvas.transform);

            MiningDynamicRadialMaskTransition radial = gate.GetComponent<MiningDynamicRadialMaskTransition>();
            if (radial == null)
            {
                radial = Object.FindFirstObjectByType<MiningDynamicRadialMaskTransition>(
                    FindObjectsInactive.Include);
                if (radial != null)
                {
                    SerializedObject existingRadial = new(radial);
                    Object oldPortal = existingRadial.FindProperty("portalGate").objectReferenceValue;
                    if (oldPortal != null && oldPortal != gate.transform) radial = null;
                }
            }
            if (radial == null) radial = Undo.AddComponent<MiningDynamicRadialMaskTransition>(gate);

            SerializedObject serializedRadial = new(radial);
            SerializedProperty portal = serializedRadial.FindProperty("portalGate");
            SerializedProperty shaderProperty = serializedRadial.FindProperty("transitionShader");
            if (portal.objectReferenceValue == null) portal.objectReferenceValue = gate.transform;
            if (shaderProperty.objectReferenceValue == null) shaderProperty.objectReferenceValue = shader;
            serializedRadial.ApplyModifiedProperties();

            MiningPortalPreviewGate interaction = gate.GetComponent<MiningPortalPreviewGate>();
            if (interaction == null) interaction = Undo.AddComponent<MiningPortalPreviewGate>(gate);
            Undo.RecordObject(interaction, "Wire Portal Preview Gate");
            interaction.ConfigureIfMissing(panel, radial);
            EditorUtility.SetDirty(interaction);

            // The shared raycast ignores trigger colliders. Preserve authored colliders and
            // add a separate hit area only if none of them can receive that raycast.
            bool hasRaycastCollider = false;
            foreach (Collider existingCollider in gate.GetComponentsInChildren<Collider>(true))
            {
                if (existingCollider.enabled && !existingCollider.isTrigger)
                {
                    hasRaycastCollider = true;
                    break;
                }
            }
            if (!hasRaycastCollider)
            {
                SphereCollider hitArea = Undo.AddComponent<SphereCollider>(gate);
                Undo.RecordObject(hitArea, "Set Portal Interaction Radius");
                hitArea.radius = 1.5f;
            }

            EditorSceneManager.MarkSceneDirty(gate.scene);
            Selection.activeGameObject = panel.gameObject;
            EditorGUIUtility.PingObject(panel.gameObject);
        }

        [MenuItem("Mining Simulator/Portal/Setup Enter Preview On Selected Gate", true)]
        private static bool ValidateSetup() => Selection.activeGameObject != null &&
                                              Selection.activeGameObject.scene.IsValid();

        private static Canvas FindCanvas()
        {
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.name == CanvasName && canvas.gameObject.scene.IsValid()) return canvas;
            return null;
        }

        private static MiningPortalPreviewPanel CreatePanel(Transform canvas)
        {
            GameObject root = new(PanelName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(canvas, false);
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.68f);
            root.transform.SetAsLastSibling();

            RectTransform card = MakeRect("Portal Card", rootRect,
                Vector2.zero, new Vector2(700, 350));
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = new Color(0.065f, 0.04f, 0.075f, 0.97f);
            Outline border = card.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.88f, 0.68f, 0.19f, 1f);
            border.effectDistance = new Vector2(3f, -3f);

            TMP_Text title = MakeText("Title", card, new Vector2(0f, 119f),
                new Vector2(640f, 58f), 34, FontStyles.Bold);
            TMP_Text message = MakeText("Message", card, new Vector2(0f, 35f),
                new Vector2(620f, 94f), 23, FontStyles.Normal);
            Button enterButton = MakeButton("Enter Button", card,
                new Vector2(-133f, -112f), new Color(0.62f, 0.32f, 0.08f, 1f));
            TMP_Text enterText = MakeText("Label", enterButton.transform,
                Vector2.zero, new Vector2(248, 56), 26, FontStyles.Bold);
            Button cancelButton = MakeButton("Cancel Button", card,
                new Vector2(133f, -112f), new Color(0.19f, 0.15f, 0.21f, 1f));
            TMP_Text cancelText = MakeText("Label", cancelButton.transform,
                Vector2.zero, new Vector2(248, 56), 24, FontStyles.Bold);

            MiningPortalPreviewPanel panel = root.AddComponent<MiningPortalPreviewPanel>();
            panel.Configure(root.GetComponent<CanvasGroup>(), title, message,
                enterButton, enterText, cancelButton, cancelText);
            Undo.RegisterCreatedObjectUndo(root, "Create Portal Preview Panel");
            EditorUtility.SetDirty(root);
            return panel;
        }

        private static RectTransform MakeRect(string name, Transform parent,
            Vector2 position, Vector2 size)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text MakeText(string name, Transform parent,
            Vector2 position, Vector2 size, float fontSize, FontStyles style)
        {
            RectTransform rect = MakeRect(name, parent, position, size);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = new Color(1f, 0.93f, 0.76f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(string name, Transform parent,
            Vector2 position, Color color)
        {
            RectTransform rect = MakeRect(name, parent, position, new Vector2(245f, 70f));
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = color;
            return rect.gameObject.AddComponent<Button>();
        }
    }
}
#endif
