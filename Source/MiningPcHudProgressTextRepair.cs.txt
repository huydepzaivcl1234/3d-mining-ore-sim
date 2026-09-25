#if UNITY_EDITOR
using Microlight.MicroBar;
using MiningSimulator.Ores;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Editor
{
    /// <summary>Restore the small, live labels on the editable PC miner progress card.</summary>
    public static class MiningPcHudProgressTextRepair
    {
        [MenuItem("Mining Simulator/UI/Repair Miner Progress Text")]
        private static void RepairSelectedScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Miner progress", "Stop Play Mode first.", "OK");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.gameObject.scene != activeScene || canvas.name != "Mining HUD Canvas")
                    continue;
                RectTransform panel = canvas.transform.Find("NPC Progress HUD") as RectTransform;
                if (panel == null) continue;
                RepairPanel(panel);
                Selection.activeGameObject = panel.gameObject;
                Debug.Log("Restored miner progress title, LV, Power, next ore and XP labels. Save the scene.", panel);
                return;
            }

            EditorUtility.DisplayDialog("Miner progress",
                "Open the play scene with Mining HUD Canvas > NPC Progress HUD.", "OK");
        }

        public static void RepairPanel(RectTransform panel)
        {
            if (panel == null) return;
            RectTransform card = panel.Find("Card_Visual") as RectTransform;
            if (card == null) return;

            RectTransform pill = card.Find("PC Level Pill") as RectTransform;
            if (pill == null)
            {
                var created = new GameObject("PC Level Pill", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(created, "Create level pill");
                pill = (RectTransform)created.transform;
                Undo.SetTransformParent(pill, card, "Place level pill");
            }
            Layout(pill, new Vector2(-217f, 13f), new Vector2(80f, 30f));

            TextMeshProUGUI level = FindLabel(pill, "Level_Badge") ??
                FindLabel(card, "Header_Pill/Avatar_Group/Level_Badge");
            level = Place(level, pill, "Level_Badge", "LV.1",
                Vector2.zero, new Vector2(77f, 28f), 16f, new Color(.98f, .9f, .65f));
            TextMeshProUGUI title = FindLabel(card, "PC Progress Title") ??
                FindLabel(card, "Header_Pill/Title_Text");
            title = Place(title, card, "PC Progress Title", "MINER PROGRESS",
                new Vector2(-116f, 13f), new Vector2(110f, 28f), 11f, Color.white);
            RectTransform well = card.Find("Stats_Well") as RectTransform;
            Transform stats = well != null ? well : card;
            TextMeshProUGUI power = FindLabel(card, "PC Power Text") ??
                FindLabel(stats, "Power_Value");
            power = Place(power, card, "PC Power Text", "Power: 0",
                new Vector2(-10f, 13f), new Vector2(90f, 28f), 13f, Color.white);
            TextMeshProUGUI reward = FindLabel(stats, "Reward_Text") ??
                FindLabel(card, "PC Next Ore Text");
            reward = Place(reward, card, "PC Next Ore Text", "Next: Stone",
                new Vector2(148f, 13f), new Vector2(220f, 28f), 13f,
                new Color(1f, .78f, .27f));
            TextMeshProUGUI xp = FindLabel(card, "XP_Label");
            xp = Place(xp, card, "XP_Label", "WORK XP",
                new Vector2(0f, -19f), new Vector2(495f, 20f), 12f, Color.white);
            // Show text above the MicroBar's images.
            if (xp != null) xp.transform.SetAsLastSibling();

            JuicyMinerProgress presenter = panel.GetComponent<JuicyMinerProgress>();
            if (presenter == null) presenter = Undo.AddComponent<JuicyMinerProgress>(panel.gameObject);
            var fields = new SerializedObject(presenter);
            Set(fields, "cardTransform", card);
            Set(fields, "titleText", title);
            Set(fields, "levelBadgeText", level);
            Set(fields, "powerValueText", power);
            Set(fields, "rewardText", reward);
            Set(fields, "xpLabelText", xp);
            Set(fields, "experienceBar", panel.GetComponentInChildren<MicroBar>(true));
            SetIfMissing(fields, "progressionSystem", Object.FindFirstObjectByType<NpcProgressionSystem>(FindObjectsInactive.Include));
            SetIfMissing(fields, "oreSpawner", Object.FindFirstObjectByType<OreSpawner>(FindObjectsInactive.Include));
            SerializedProperty compact = fields.FindProperty("compactPcXpLabel");
            if (compact != null) compact.boolValue = true;
            fields.ApplyModifiedProperties();
            presenter.Refresh();

            NpcProgressionHud original = panel.GetComponent<NpcProgressionHud>();
            if (original != null)
            {
                var originalFields = new SerializedObject(original);
                if (originalFields.FindProperty("progressionSystem")?.objectReferenceValue == null)
                    Set(originalFields, "progressionSystem", Object.FindFirstObjectByType<NpcProgressionSystem>(FindObjectsInactive.Include));
                if (originalFields.FindProperty("experienceBar")?.objectReferenceValue == null)
                    Set(originalFields, "experienceBar", panel.GetComponentInChildren<MicroBar>(true));
                originalFields.ApplyModifiedProperties();
            }
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        }

        private static TextMeshProUGUI FindLabel(Transform root, string path)
        {
            return root != null ? root.Find(path)?.GetComponent<TextMeshProUGUI>() : null;
        }

        private static TextMeshProUGUI Place(TextMeshProUGUI label, Transform parent,
            string name, string fallback, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            if (label == null)
            {
                var created = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(created, "Restore miner progress label");
                label = created.GetComponent<TextMeshProUGUI>();
                label.text = fallback;
            }
            if (label.transform.parent != parent)
                Undo.SetTransformParent(label.transform, parent, "Restore miner progress label");
            Undo.RecordObject(label.gameObject, "Show miner progress label");
            label.gameObject.name = name;
            label.gameObject.SetActive(true);
            Layout(label.rectTransform, position, size);
            Undo.RecordObject(label, "Fit miner progress label");
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 9f;
            label.fontSizeMax = fontSize;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static void Layout(RectTransform rect, Vector2 position, Vector2 size)
        {
            Undo.RecordObject(rect, "Fit miner progress text");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
        }

        private static void Set(SerializedObject fields, string name, Object value)
        {
            SerializedProperty property = fields.FindProperty(name);
            if (property != null && value != null) property.objectReferenceValue = value;
        }

        private static void SetIfMissing(SerializedObject fields, string name, Object value)
        {
            SerializedProperty property = fields.FindProperty(name);
            if (property != null && property.objectReferenceValue == null && value != null)
                property.objectReferenceValue = value;
        }
    }
}
#endif
