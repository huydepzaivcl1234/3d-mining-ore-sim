#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Authors the Inventory button visuals into the open scene without moving its root.</summary>
    public static class MiningInventoryButtonSetupMenu
    {
        private const string VisualRootName = "Juicy Inventory Visuals";

        [MenuItem("Mining Simulator/UI/Build Inventory Menu Button")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Exit Play Mode",
                    "Stop Play Mode before building the Inventory button so the authored objects can be saved.",
                    "OK");
                return;
            }

            MiningInventoryPanel inventory = Object.FindFirstObjectByType<MiningInventoryPanel>(
                FindObjectsInactive.Include);
            if (inventory == null)
            {
                Debug.LogWarning("Open the mining scene first: MiningInventoryPanel was not found. No scene was changed.");
                return;
            }

            SerializedObject inventoryFields = new(inventory);
            Button button = inventoryFields.FindProperty("openButton")?.objectReferenceValue as Button;
            if (button == null)
            {
                Debug.LogWarning("Inventory open button reference is missing. No scene was changed.", inventory);
                return;
            }

            TextMeshProUGUI title = FindAuthoredTitle(button.transform);
            if (title == null)
            {
                Debug.LogWarning("Inventory button title was not found. No scene was changed.", button);
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Inventory Menu Button");

            RectTransform visualRoot = EnsureChild(button.transform as RectTransform, VisualRootName);
            _ = GetOrAdd<CanvasRenderer>(visualRoot.gameObject);
            _ = GetOrAdd<MiningInventoryButtonGraphic>(visualRoot.gameObject);

            RectTransform socket = EnsureChild(visualRoot, "Icon Socket");
            _ = GetOrAdd<CanvasRenderer>(socket.gameObject);
            _ = GetOrAdd<MiningInventoryIconMaskGraphic>(socket.gameObject);
            _ = GetOrAdd<Mask>(socket.gameObject);

            RectTransform icon = EnsureChild(socket, "Inventory Icon");
            _ = GetOrAdd<CanvasRenderer>(icon.gameObject);
            _ = GetOrAdd<Image>(icon.gameObject);
            _ = GetOrAdd<AspectRatioFitter>(icon.gameObject);

            Transform staleHotkey = visualRoot.Find("Hotkey");
            if (staleHotkey != null)
            {
                Undo.DestroyObjectImmediate(staleHotkey.gameObject);
            }

            JuicyInventoryButton presentation = button.GetComponent<JuicyInventoryButton>() ??
                                                Undo.AddComponent<JuicyInventoryButton>(button.gameObject);

            Component[] affected = button.GetComponentsInChildren<Component>(true);
            Undo.RecordObjects(affected, "Style Inventory Menu Button");
            presentation.Configure(button, title);

            SetLayerRecursively(visualRoot, button.gameObject.layer);
            foreach (Component component in affected)
            {
                if (component != null) EditorUtility.SetDirty(component);
            }
            EditorUtility.SetDirty(button);
            EditorUtility.SetDirty(presentation);
            EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = button.gameObject;
            EditorGUIUtility.PingObject(button.gameObject);
            Debug.Log("Inventory button authored into the Scene. Root anchors, position and size were preserved. Save the scene when satisfied.", button);
        }

        [MenuItem("Mining Simulator/UI/Build Inventory Menu Button", true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static TextMeshProUGUI FindAuthoredTitle(Transform button)
        {
            Transform named = button.Find("Text (TMP)");
            if (named != null)
            {
                TextMeshProUGUI label = named.GetComponent<TextMeshProUGUI>();
                if (label != null) return label;
            }

            Transform visualRoot = button.Find(VisualRootName);
            foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (visualRoot == null || !label.transform.IsChildOf(visualRoot)) return label;
            }
            return null;
        }

        private static RectTransform EnsureChild(RectTransform parent, string childName)
        {
            Transform existing = parent != null ? parent.Find(childName) : null;
            if (existing is RectTransform existingRect) return existingRect;

            GameObject child = new(childName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
            Undo.SetTransformParent(child.transform, parent, $"Parent {childName}");
            child.layer = parent.gameObject.layer;
            return child.GetComponent<RectTransform>();
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            return existing != null ? existing : Undo.AddComponent<T>(target);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null) return;
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayerRecursively(child, layer);
        }
    }
}
#endif
