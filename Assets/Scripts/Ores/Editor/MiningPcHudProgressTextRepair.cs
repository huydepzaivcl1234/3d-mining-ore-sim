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
                bool rebuilt = panel == null;
                if (panel == null)
                    panel = RestoreMissingPanel(canvas);
                if (panel == null) return;
                RepairPanel(panel);
                if (rebuilt) FitRestoredPanel(panel);
                Selection.activeGameObject = panel.gameObject;
                Debug.Log("Restored miner progress card, LV, Power, next ore and live XP bar. Save the scene.", panel);
                return;
            }

            EditorUtility.DisplayDialog("Miner progress",
                "Open the play scene containing Mining HUD Canvas.", "OK");
        }

        private static RectTransform RestoreMissingPanel(Canvas canvas)
        {
            NpcProgressionSystem progression = Object.FindFirstObjectByType<NpcProgressionSystem>(
                FindObjectsInactive.Include);
            OreSpawner spawner = Object.FindFirstObjectByType<OreSpawner>(FindObjectsInactive.Include);
            if (progression == null || spawner == null || spawner.SpawnData == null)
            {
                EditorUtility.DisplayDialog("Miner progress",
                    "The scene needs NPC Progression System and a configured Ore Spawner before the card can be restored.", "OK");
                return null;
            }

            // This project already ships an authored MicroBar and all three legacy labels.
            // Clone just that HUD, never a second manager, shop, NPC or whole runtime prefab.
            GameObject runtime = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Systems/MiningRuntime.prefab");
            Transform source = runtime != null ? runtime.transform.Find("Mining HUD Canvas/npc Progress Hud") : null;
            if (source == null)
            {
                EditorUtility.DisplayDialog("Miner progress",
                    "Could not find MiningRuntime.prefab > Mining HUD Canvas > npc Progress Hud.", "OK");
                return null;
            }

            GameObject copy = Object.Instantiate(source.gameObject, canvas.transform, false);
            copy.name = "NPC Progress HUD";
            Undo.RegisterCreatedObjectUndo(copy, "Restore missing miner progress HUD");
            RectTransform panel = copy.GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -16f);
            panel.localScale = Vector3.one;

            NpcProgressionHud hud = copy.GetComponent<NpcProgressionHud>();
            if (hud != null)
            {
                SerializedObject fields = new(hud);
                Set(fields, "progressionSystem", progression);
                Set(fields, "npcShop", Object.FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include));
                fields.ApplyModifiedProperties();
            }

            // Build the existing styled progress card around the cloned MicroBar.
            MiningSimulator.Ores.Editor.MiningJuicyMinerProgressSetupMenu.Build();
            if (panel.Find("Card_Visual") == null)
            {
                Undo.DestroyObjectImmediate(copy);
                EditorUtility.DisplayDialog("Miner progress",
                    "Could not rebuild the XP card. Check the Unity Console for the missing prefab or MicroBar asset.", "OK");
                return null;
            }
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            return panel;
        }

        private static void FitRestoredPanel(RectTransform panel)
        {
            RectTransform card = panel.Find("Card_Visual") as RectTransform;
            if (card == null) return;
            Undo.RecordObject(panel, "Fit restored miner progress");
            panel.sizeDelta = new Vector2(540f, 76f);
            Undo.RecordObject(card, "Fit restored miner card");
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f);
            card.sizeDelta = new Vector2(540f, 76f);
            card.anchoredPosition = Vector2.zero;
            card.localScale = Vector3.one;

            foreach (string old in new[] { "Header_Pill", "Stats_Well", "XP_Percent" })
            {
                Transform child = card.Find(old);
                if (child == null) continue;
                Undo.RecordObject(child.gameObject, "Hide oversized miner card decoration");
                child.gameObject.SetActive(false);
            }
            Transform legacyCounter = panel.Find("Experience");
            if (legacyCounter != null)
            {
                Undo.RecordObject(legacyCounter.gameObject, "Use compact XP label");
                legacyCounter.gameObject.SetActive(false);
            }
            RectTransform trench = card.Find("XP_Trench") as RectTransform;
            MicroBar bar = panel.GetComponentInChildren<MicroBar>(true);
            if (trench == null || bar == null) return;
            Undo.RecordObject(trench, "Fit restored XP track");
            trench.anchorMin = trench.anchorMax = trench.pivot = new Vector2(.5f, .5f);
            trench.anchoredPosition = new Vector2(0f, -19f);
            trench.sizeDelta = new Vector2(505f, 20f);
            RectTransform barRect = bar.transform as RectTransform;
            Undo.SetTransformParent(barRect, trench, "Keep live XP bar inside track");
            Undo.RecordObject(barRect, "Fit restored XP bar");
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(.5f, .5f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(495f, 16f);
            barRect.localScale = Vector3.one;
            barRect.SetAsFirstSibling();
        }

        public static void RepairPanel(RectTransform panel)
        {
            if (panel == null) return;
            Undo.RecordObject(panel.gameObject, "Show miner progress HUD");
            panel.gameObject.SetActive(true);
            Canvas parentCanvas = panel.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
            {
                Undo.RecordObject(parentCanvas.gameObject, "Show mining HUD canvas");
                parentCanvas.gameObject.SetActive(true);
                Undo.RecordObject(parentCanvas, "Enable mining HUD canvas");
                parentCanvas.enabled = true;
            }
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group != null)
            {
                Undo.RecordObject(group, "Reveal miner progress HUD");
                group.alpha = 1f;
            }
            RectTransform card = panel.Find("Card_Visual") as RectTransform;
            if (card == null)
            {
                Debug.LogWarning("Miner progress card is missing; run Build Juicy Miner Progress with the original HUD present.", panel);
                return;
            }
            Undo.RecordObject(card.gameObject, "Show miner progress card");
            card.gameObject.SetActive(true);
            Transform backdrop = panel.Find("PC Progress Background");
            if (backdrop != null)
            {
                Undo.RecordObject(backdrop.gameObject, "Show miner progress background");
                backdrop.gameObject.SetActive(true);
            }
            Transform trench = card.Find("XP_Trench");
            if (trench != null)
            {
                Undo.RecordObject(trench.gameObject, "Show XP track");
                trench.gameObject.SetActive(true);
            }
            MicroBar liveBar = panel.GetComponentInChildren<MicroBar>(true);
            if (liveBar != null)
            {
                Undo.RecordObject(liveBar.gameObject, "Show XP bar");
                liveBar.gameObject.SetActive(true);
                Undo.RecordObject(liveBar, "Enable XP bar");
                liveBar.enabled = true;
            }

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
            Set(fields, "experienceBar", liveBar);
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
                    Set(originalFields, "experienceBar", liveBar);
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
