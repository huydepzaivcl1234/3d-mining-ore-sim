#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Removes only the legacy Inventory button presentation from the open scene.</summary>
    public static class MiningInventoryButtonSetupMenu
    {
        [MenuItem("Mining Simulator/UI/Remove Old Inventory Candy Visuals")]
        public static void RemoveOldVisuals()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            MiningInventoryPanel inventory = UnityEngine.Object.FindFirstObjectByType<MiningInventoryPanel>(
                FindObjectsInactive.Include);
            if (inventory == null)
            {
                Debug.LogWarning("Open the gameplay scene before cleaning up Inventory.");
                return;
            }

            SerializedObject fields = new(inventory);
            Button button = fields.FindProperty("openButton")?.objectReferenceValue as Button;
            if (button == null)
            {
                Debug.LogWarning("Inventory openButton is not assigned.", inventory);
                return;
            }

            Sprite authoredSprite = FindButtonSprite();
            Transform oldVisuals = button.transform.Find("Juicy Inventory Visuals");
            if (authoredSprite == null && oldVisuals != null)
            {
                Transform icon = oldVisuals.Find("Icon Socket/Inventory Icon");
                if (icon != null && icon.TryGetComponent(out Image oldIcon))
                    authoredSprite = oldIcon.sprite;
            }
            Image image = button.GetComponent<Image>();
            if (image == null && authoredSprite == null)
            {
                Debug.LogWarning("No Inventory button sprite found. Assign an Image and your own sprite " +
                                 "to Inventory Menu Button, then run this command again.", button);
                return;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove Old Inventory Candy Visuals");

            if (image == null) image = Undo.AddComponent<Image>(button.gameObject);
            Undo.RecordObject(image, "Restore Inventory Button Image");
            Undo.RecordObject(button, "Restore Inventory Button Click Target");
            if (authoredSprite != null) image.sprite = authoredSprite;
            image.enabled = true;
            image.color = Color.white;
            image.raycastTarget = true;
            image.preserveAspect = true;
            button.targetGraphic = image;

            // This component also works on any other UI button via Add Component.
            if (button.GetComponent<MiningUiSmoothFade>() == null)
                Undo.AddComponent<MiningUiSmoothFade>(button.gameObject);

            if (oldVisuals != null)
            {
                Undo.DestroyObjectImmediate(oldVisuals.gameObject);
            }

            // After deleting the old script file this removes its missing-script reference
            // from this button only. It does not touch any other objects in the scene.
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(button.gameObject);
            foreach (MonoBehaviour component in button.GetComponents<MonoBehaviour>())
            {
                if (component != null && component.GetType().Name == "JuicyInventoryButton")
                    Undo.DestroyObjectImmediate(component);
            }

            foreach (Component component in button.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name == "MiningCandyGradient")
                    Undo.DestroyObjectImmediate(component);
            }

            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(button);
            EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
            Selection.activeGameObject = button.gameObject;
            Undo.CollapseUndoOperations(group);
            Debug.Log("Old Inventory visuals removed. The authored button, RectTransform and " +
                      "click wiring are intact. Save the scene.", button);
        }

        [MenuItem("Mining Simulator/UI/Remove Old Inventory Candy Visuals", true)]
        private static bool CanRemove() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static Sprite FindButtonSprite()
        {
            string[] ids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Prefabs/UI" });
            Sprite fallback = null;
            foreach (string id in ids)
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name.IndexOf("btn_inventory", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("btn_invent", StringComparison.OrdinalIgnoreCase) >= 0)
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (fallback == null && name.IndexOf("invent", StringComparison.OrdinalIgnoreCase) >= 0)
                    fallback = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return fallback;
        }
    }
}
#endif
