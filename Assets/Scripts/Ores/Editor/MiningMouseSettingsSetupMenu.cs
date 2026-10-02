#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace MiningSimulator.Ores.Editor
{
    public static class MiningMouseSettingsSetupMenu
    {
        [MenuItem("Mining Simulator/Setup/Add Mouse Sensitivity To Settings")]
        public static void Setup()
        {
            var controller = Object.FindFirstObjectByType<MiningAudioSettingsPanel>(FindObjectsInactive.Include);
            if (controller == null) { Debug.LogError("Settings controller not found."); return; }
            var so = new SerializedObject(controller);
            var panel = so.FindProperty("settingsPanel").objectReferenceValue as GameObject;
            if (panel == null || panel.transform.Find("SFX Slider") == null) { Debug.LogError("Existing SFX row missing."); return; }
            Transform root = panel.transform;
            bool added = root.Find("Mouse Slider") == null;
            // Reuse the authored slider skin; no panel rebuild or unrelated restyling.
            foreach (string suffix in new[] { "Label", "Slider", "Value" })
            {
                var source = root.Find("SFX " + suffix);
                if (source == null) { Debug.LogError("Existing SFX " + suffix + " missing."); return; }
                if (root.Find("Mouse " + suffix) == null)
                {
                    var clone = Object.Instantiate(source.gameObject, root, false);
                    clone.name = "Mouse " + suffix;
                    Undo.RegisterCreatedObjectUndo(clone, "Add Mouse Sensitivity");
                }
            }
            if (added)
            {
                ShiftRow(root, "Music", 15f);
                ShiftRow(root, "SFX", 30f);
                foreach (string suffix in new[] { "Label", "Slider", "Value" })
                {
                    var rect = root.Find("Mouse " + suffix).GetComponent<RectTransform>();
                    Undo.RecordObject(rect, "Place Mouse Sensitivity");
                    var position = rect.anchoredPosition; position.y = suffix == "Slider" ? -50f : -22f;
                    rect.anchoredPosition = position;
                }
            }
            var slider = root.Find("Mouse Slider").GetComponent<UnityEngine.UI.Slider>();
            Undo.RecordObject(slider, "Configure Mouse Sensitivity");
            // Cloning an audio slider must never retain its volume callback.
            for (int i = slider.onValueChanged.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(slider.onValueChanged, i);
            slider.minValue = .25f; slider.maxValue = 4f; slider.wholeNumbers = false;
            if (slider.handleRect != null)
            {
                Undo.RecordObject(slider.handleRect, "Size sensitivity handle within track");
                // Slider drives both vertical anchors to stretch when enabled.
                var min = slider.handleRect.anchorMin; min.y = 0f;
                var max = slider.handleRect.anchorMax; max.y = 1f;
                slider.handleRect.anchorMin = min; slider.handleRect.anchorMax = max;
                var size = slider.handleRect.sizeDelta; size.y = -6f;
                slider.handleRect.sizeDelta = size;
                var handlePosition = slider.handleRect.anchoredPosition; handlePosition.y = 0f;
                slider.handleRect.anchoredPosition = handlePosition;
            }
            MiningOrbitCamera gameplay = null;
            foreach (var rig in Object.FindObjectsByType<MiningOrbitCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (rig.FollowTarget != null) { gameplay = rig; break; }
            if (gameplay != null) { slider.minValue = gameplay.SensitivityMinimum; slider.maxValue = gameplay.SensitivityMaximum; }
            slider.SetValueWithoutNotify(gameplay != null ? gameplay.MouseSensitivity : 1f);
            var label = root.Find("Mouse Label").GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(label, "Mouse Sensitivity Label"); label.text = MiningLocalization.Text("MOUSE SENSITIVITY");
            var value = root.Find("Mouse Value").GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(value, "Mouse Sensitivity Value"); value.text = $"{slider.value:0.00}x";
            so.FindProperty("cameraRig").objectReferenceValue = gameplay;
            so.FindProperty("sensitivitySlider").objectReferenceValue = slider;
            so.FindProperty("sensitivityValueLabel").objectReferenceValue = value;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(panel.scene);
            Debug.Log("Mouse Sensitivity added. Save the scene when ready.");
        }

        private static void ShiftRow(Transform root, string prefix, float offset)
        {
            foreach (string suffix in new[] { "Label", "Slider", "Value" })
            {
                var item = root.Find(prefix + " " + suffix); if (item == null) continue;
                var rect = item.GetComponent<RectTransform>();
                Undo.RecordObject(rect, "Make room for Mouse Sensitivity");
                rect.anchoredPosition += Vector2.up * offset;
            }
        }
    }
}
#endif
