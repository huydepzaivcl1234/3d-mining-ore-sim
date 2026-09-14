#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Authors the shared Main Menu/gameplay quest UI without replacing scene data.</summary>
    public static class MiningQuestSetupMenu
    {
        private const string DataFolder = "Assets/GameData/Quests";
        private const string DataPath = DataFolder + "/MiningQuestData.asset";
        private const string RoundedSpritePath = "Assets/Generated/MiningUI/CandyRoundedRect.png";

        private static readonly Color OutlineColor = new(0.035f, 0.065f, 0.12f, 1f);
        private static readonly Color NeutralTop = new(0.10f, 0.14f, 0.23f, 1f);
        private static readonly Color NeutralBottom = new(0.035f, 0.055f, 0.10f, 1f);
        private static readonly Color CyanTop = new(0.31f, 0.85f, 1f, 1f);
        private static readonly Color CyanBottom = new(0.12f, 0.50f, 0.88f, 1f);
        private static readonly Color GreenTop = new(0.20f, 0.84f, 0.69f, 1f);
        private static readonly Color GreenBottom = new(0.08f, 0.49f, 0.36f, 1f);
        private static readonly Color RedTop = new(1f, 0.42f, 0.24f, 1f);
        private static readonly Color RedBottom = new(0.88f, 0.09f, 0.23f, 1f);
        private static readonly Color DailyTop = new(0.31f, 0.85f, 1f, 1f);
        private static readonly Color DailyBottom = new(0.12f, 0.50f, 0.88f, 1f);
        private static readonly Color WeeklyTop = new(1f, 0.67f, 0.18f, 1f);
        private static readonly Color WeeklyBottom = new(0.95f, 0.31f, 0.12f, 1f);

        [MenuItem("Mining Simulator/Setup/Create Or Update Daily Weekly Quests")]
        public static void CreateOrUpdateQuests()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Quest Setup", "Exit Play Mode first.", "OK");
                return;
            }

            Canvas canvas = FindCanvas();
            PlayerWallet wallet = Object.FindFirstObjectByType<PlayerWallet>(
                FindObjectsInactive.Include);
            if (canvas == null || wallet == null)
            {
                EditorUtility.DisplayDialog("Quest Setup",
                    "Open the gameplay scene containing Mining HUD Canvas and GameManager.", "OK");
                return;
            }

            MiningQuestData data = LoadOrCreateData();
            if (!TryValidateQuestIds(data, out string validationMessage))
            {
                EditorUtility.DisplayDialog("Quest Setup", validationMessage, "OK");
                return;
            }
            MiningQuestSystem questSystem = wallet.GetComponent<MiningQuestSystem>() ??
                                            Undo.AddComponent<MiningQuestSystem>(wallet.gameObject);
            OreSpawner oreSpawner = Object.FindFirstObjectByType<OreSpawner>(
                FindObjectsInactive.Include);
            NpcShop npcShop = Object.FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            MiningRebirthSystem rebirthSystem = Object.FindFirstObjectByType<MiningRebirthSystem>(
                FindObjectsInactive.Include);
            MiningUiPanelCoordinator coordinator = Object.FindFirstObjectByType<
                MiningUiPanelCoordinator>(FindObjectsInactive.Include);
            WireQuestSystem(questSystem, data, wallet, oreSpawner, npcShop, rebirthSystem);

            Button gameplayButton = EnsureGameplayButton(canvas.transform);
            Button mainMenuButton = EnsureMainMenuButton(canvas.transform);
            RectTransform panelRoot = EnsureQuestPanel(canvas.transform);
            RectTransform card = panelRoot.Find("Quest Card") as RectTransform;
            Button closeButton = FindComponent<Button>(card, "Close Button");

            MiningQuestPanel panel = canvas.GetComponent<MiningQuestPanel>() ??
                                     Undo.AddComponent<MiningQuestPanel>(canvas.gameObject);
            WireQuestPanel(panel, data, questSystem, coordinator, panelRoot,
                gameplayButton, mainMenuButton, closeButton, card);
            if (rebirthSystem != null)
            {
                SetReference(new SerializedObject(rebirthSystem), "questSystem", questSystem, true);
            }

            MiningButtonSfxSetupMenu.AssignAllButtonSfx(false);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = panelRoot.gameObject;
            EditorGUIUtility.PingObject(panelRoot.gameObject);
            Debug.Log("Daily/Weekly Quests ready in Main Menu and gameplay. Save the scene.",
                panelRoot);
            EditorUtility.DisplayDialog("Quest Setup",
                "Daily/Weekly Quests are ready. The panel is selected for editing. Save the scene.",
                "OK");
        }

        private static MiningQuestData LoadOrCreateData()
        {
            MiningQuestData data = AssetDatabase.LoadAssetAtPath<MiningQuestData>(DataPath);
            if (data != null) return data;
            if (!AssetDatabase.IsValidFolder(DataFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/GameData"))
                {
                    AssetDatabase.CreateFolder("Assets", "GameData");
                }
                AssetDatabase.CreateFolder("Assets/GameData", "Quests");
            }
            data = ScriptableObject.CreateInstance<MiningQuestData>();
            AssetDatabase.CreateAsset(data, DataPath);
            AssetDatabase.SaveAssets();
            return data;
        }

        private static void WireQuestSystem(MiningQuestSystem system, MiningQuestData data,
            PlayerWallet wallet, OreSpawner oreSpawner, NpcShop npcShop,
            MiningRebirthSystem rebirthSystem)
        {
            var serialized = new SerializedObject(system);
            SetReference(serialized, "data", data);
            SetReference(serialized, "wallet", wallet);
            SetReference(serialized, "oreSpawner", oreSpawner);
            SetReference(serialized, "npcShop", npcShop);
            SetReference(serialized, "rebirthSystem", rebirthSystem);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(system);
        }

        private static void WireQuestPanel(MiningQuestPanel panel, MiningQuestData data,
            MiningQuestSystem system, MiningUiPanelCoordinator coordinator,
            RectTransform root, Button gameplayButton, Button mainMenuButton,
            Button closeButton, RectTransform card)
        {
            var serialized = new SerializedObject(panel);
            SetReference(serialized, "data", data);
            SetReference(serialized, "questSystem", system);
            SetReference(serialized, "panelCoordinator", coordinator);
            SetReference(serialized, "panelRoot", root);
            SetReference(serialized, "gameplayOpenButton", gameplayButton);
            SetReference(serialized, "mainMenuOpenButton", mainMenuButton);
            SetReference(serialized, "closeButton", closeButton);
            SetReference(serialized, "titleLabel", FindComponent<TextMeshProUGUI>(card, "Title"));
            SetReference(serialized, "resetTimerLabel",
                FindComponent<TextMeshProUGUI>(card, "Reset Timer"));
            SetReference(serialized, "statusLabel",
                FindComponent<TextMeshProUGUI>(card, "Quest Status"));

            SerializedProperty rows = serialized.FindProperty("rows");
            RectTransform content = EnsureQuestList(card, data.Quests.Count);
            rows.arraySize = data.Quests.Count;
            for (int index = 0; index < data.Quests.Count; index++)
            {
                MiningQuestDefinition quest = data.Quests[index];
                RectTransform row = EnsureQuestRow(card, content, quest, index);
                SerializedProperty element = rows.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("questId").stringValue = quest.QuestId;
                element.FindPropertyRelative("root").objectReferenceValue = row.gameObject;
                element.FindPropertyRelative("periodLabel").objectReferenceValue =
                    FindComponent<TextMeshProUGUI>(row, "Period");
                element.FindPropertyRelative("nameLabel").objectReferenceValue =
                    FindComponent<TextMeshProUGUI>(row, "Quest Name");
                element.FindPropertyRelative("progressLabel").objectReferenceValue =
                    FindComponent<TextMeshProUGUI>(row, "Progress");
                element.FindPropertyRelative("rewardLabel").objectReferenceValue =
                    FindComponent<TextMeshProUGUI>(row, "Reward");
                element.FindPropertyRelative("progressFill").objectReferenceValue =
                    FindComponent<Image>(row, "Progress Fill");
                element.FindPropertyRelative("claimButton").objectReferenceValue =
                    FindComponent<Button>(row, "Claim Button");
                element.FindPropertyRelative("claimLabel").objectReferenceValue =
                    FindComponent<TextMeshProUGUI>(row, "Claim Label");
            }
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(panel);
        }

        private static Button EnsureGameplayButton(Transform canvas)
        {
            Transform existing = canvas.Find("Quest Menu Button");
            if (existing != null) return existing.GetComponent<Button>();
            Button button = CreateButton(canvas, "Quest Menu Button", "QUESTS",
                new Vector2(-100f, -510f), new Vector2(190f, 58f), true);
            RectTransform rect = button.transform as RectTransform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -500f);
            return button;
        }

        private static Button EnsureMainMenuButton(Transform canvas)
        {
            Transform mainView = FindDescendant(canvas, "Main View");
            if (mainView == null) return null;
            Transform existing = mainView.Find("Quest Button");
            if (existing != null) return existing.GetComponent<Button>();
            return CreateButton(mainView, "Quest Button", "QUESTS",
                new Vector2(-250f, -220f), new Vector2(300f, 64f), false);
        }

        private static RectTransform EnsureQuestPanel(Transform canvas)
        {
            Transform existing = canvas.Find("Quest Panel");
            if (existing != null)
            {
                ConfigureCanvasGroup(existing.gameObject);
                existing.gameObject.SetActive(true);
                return existing as RectTransform;
            }

            GameObject rootObject = CreateImage(canvas, "Quest Panel",
                new Color(0.015f, 0.025f, 0.055f, 0.86f), false);
            RectTransform root = rootObject.GetComponent<RectTransform>();
            Stretch(root);
            ConfigureCanvasGroup(rootObject);

            GameObject cardObject = CreateImage(root, "Quest Card", Color.white, true,
                NeutralTop, NeutralBottom);
            RectTransform card = cardObject.GetComponent<RectTransform>();
            Center(card, Vector2.zero, new Vector2(1040f, 680f));

            GameObject headerObject = CreateImage(card, "Header", Color.white, true,
                CyanTop, CyanBottom);
            RectTransform header = headerObject.GetComponent<RectTransform>();
            Top(header, Vector2.zero, new Vector2(1040f, 78f));
            CreateLabel(header, "Title", "DAILY & WEEKLY QUESTS", Vector2.zero,
                new Vector2(760f, 62f), 34f, TextAlignmentOptions.Center);
            CreateButton(header, "Close Button", "X", new Vector2(478f, 0f),
                new Vector2(58f, 58f), false, RedTop, RedBottom);

            CreateLabel(card, "Reset Timer", "DAILY RESET 00:00:00  •  WEEKLY RESET 00:00:00",
                new Vector2(0f, 245f), new Vector2(900f, 38f), 18f,
                TextAlignmentOptions.Center);
            CreateLabel(card, "Quest Status", string.Empty, new Vector2(0f, -305f),
                new Vector2(900f, 42f), 22f, TextAlignmentOptions.Center);
            return root;
        }

        private static RectTransform EnsureQuestList(RectTransform card, int questCount)
        {
            Transform scrollTransform = card.Find("Quest Scroll View");
            if (scrollTransform == null)
            {
                GameObject scrollObject = CreateImage(card, "Quest Scroll View", Color.clear,
                    false);
                scrollTransform = scrollObject.transform;
            }

            RectTransform scrollRectTransform = scrollTransform as RectTransform;
            Center(scrollRectTransform, new Vector2(0f, -28f), new Vector2(970f, 460f));
            ConfigureCanvasGroup(scrollTransform.gameObject);
            ConfigureTransparentRaycastImage(scrollTransform.gameObject);

            Transform viewportTransform = scrollTransform.Find("Viewport");
            if (viewportTransform == null)
            {
                GameObject viewportObject = CreateImage(scrollTransform, "Viewport", Color.clear,
                    false);
                viewportTransform = viewportObject.transform;
            }
            RectTransform viewport = viewportTransform as RectTransform;
            Stretch(viewport);
            ConfigureTransparentRaycastImage(viewportTransform.gameObject);
            if (viewportTransform.GetComponent<RectMask2D>() == null)
            {
                Undo.AddComponent<RectMask2D>(viewportTransform.gameObject);
            }

            Transform contentTransform = viewportTransform.Find("Content");
            if (contentTransform == null)
            {
                GameObject contentObject = new("Content", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(contentObject, "Create Quest Content");
                contentObject.transform.SetParent(viewportTransform, false);
                contentTransform = contentObject.transform;
            }

            RectTransform content = contentTransform as RectTransform;
            float contentHeight = Mathf.Max(460f, questCount * 145f +
                                                   Mathf.Max(0, questCount - 1) * 12f);
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(940f, contentHeight);

            ScrollRect scroll = scrollTransform.GetComponent<ScrollRect>() ??
                                Undo.AddComponent<ScrollRect>(scrollTransform.gameObject);
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.12f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.scrollSensitivity = 45f;
            return content;
        }

        private static bool TryValidateQuestIds(MiningQuestData data, out string message)
        {
            var uniqueIds = new HashSet<string>(System.StringComparer.Ordinal);
            for (int index = 0; index < data.Quests.Count; index++)
            {
                MiningQuestDefinition quest = data.Quests[index];
                if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId))
                {
                    message = $"Quest #{index + 1} has no Quest Id. Give every quest a stable, " +
                              "unique Quest Id, then run this setup again.";
                    return false;
                }

                if (!uniqueIds.Add(quest.QuestId))
                {
                    message = $"Quest #{index + 1} uses the duplicate Quest Id " +
                              $"'{quest.QuestId}'. Change it to a unique value, for example " +
                              $"'{quest.QuestId}_{index + 1}', then run this setup again. " +
                              "Quest data was not changed.";
                    return false;
                }
            }

            message = null;
            return true;
        }

        private static void ConfigureTransparentRaycastImage(GameObject target)
        {
            Image image = target.GetComponent<Image>() ?? Undo.AddComponent<Image>(target);
            MiningCandyGradient gradient = target.GetComponent<MiningCandyGradient>();
            if (gradient != null)
            {
                Undo.DestroyObjectImmediate(gradient);
            }
            foreach (Shadow effect in target.GetComponents<Shadow>())
            {
                Undo.DestroyObjectImmediate(effect);
            }
            Transform highlight = target.transform.Find("Candy Highlight");
            if (highlight != null)
            {
                Undo.DestroyObjectImmediate(highlight.gameObject);
            }

            Undo.RecordObject(image, "Configure Transparent Quest Scroll Graphic");
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Color.clear;
            image.raycastTarget = true;
            EditorUtility.SetDirty(image);
        }

        private static void ConfigureCanvasGroup(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>() ??
                                Undo.AddComponent<CanvasGroup>(target);
            Undo.RecordObject(group, "Configure Quest Canvas Group");
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            EditorUtility.SetDirty(group);
        }

        private static RectTransform EnsureQuestRow(RectTransform card, RectTransform content,
            MiningQuestDefinition quest, int index)
        {
            string objectName = "Quest Row " + quest.QuestId;
            Transform existing = content.Find(objectName) ?? card.Find(objectName);
            bool created = existing == null;
            if (created)
            {
                GameObject rowObject = CreateImage(content, objectName, Color.white, true,
                    NeutralTop, NeutralBottom);
                existing = rowObject.transform;
            }
            else if (existing.parent != content)
            {
                Undo.SetTransformParent(existing, content, "Move Quest Row Into Scroll View");
            }

            RectTransform row = existing as RectTransform;
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -index * 157f);
            row.sizeDelta = new Vector2(940f, 145f);
            if (!created)
            {
                return row;
            }

            bool daily = quest.Period == MiningQuestPeriod.Daily;
            GameObject badgeObject = CreateImage(row, "Period Badge", Color.white, true,
                daily ? DailyTop : WeeklyTop, daily ? DailyBottom : WeeklyBottom);
            Center(badgeObject.GetComponent<RectTransform>(), new Vector2(-385f, 34f),
                new Vector2(135f, 42f));
            CreateLabel(badgeObject.transform, "Period", daily ? "DAILY" : "WEEKLY",
                Vector2.zero, new Vector2(125f, 36f), 17f, TextAlignmentOptions.Center);

            CreateLabel(row, "Quest Name", quest.GetLocalizedName(), new Vector2(-135f, 35f),
                new Vector2(360f, 44f), 22f, TextAlignmentOptions.Left);
            CreateLabel(row, "Reward", "REWARD: " + quest.GetRewardPreview(),
                new Vector2(-85f, -42f), new Vector2(470f, 36f), 17f,
                TextAlignmentOptions.Left);

            GameObject progressBackground = CreateImage(row, "Progress Background",
                new Color(0.04f, 0.07f, 0.12f, 1f), true);
            Center(progressBackground.GetComponent<RectTransform>(), new Vector2(-95f, -5f),
                new Vector2(450f, 25f));
            GameObject fillObject = CreateImage(progressBackground.transform, "Progress Fill",
                new Color(0.18f, 0.82f, 0.57f, 1f), false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            Stretch(fillRect);
            Image fill = fillObject.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            CreateLabel(progressBackground.transform, "Progress", "0 / 1", Vector2.zero,
                new Vector2(430f, 23f), 15f, TextAlignmentOptions.Center);

            Button claim = CreateButton(row, "Claim Button", "IN PROGRESS",
                new Vector2(375f, -5f), new Vector2(165f, 68f), false, GreenTop, GreenBottom);
            claim.GetComponentInChildren<TextMeshProUGUI>().name = "Claim Label";
            return row;
        }

        private static Button CreateButton(Transform parent, string name, string text,
            Vector2 position, Vector2 size, bool topRight)
        {
            return CreateButton(parent, name, text, position, size, topRight, CyanTop, CyanBottom);
        }

        private static Button CreateButton(Transform parent, string name, string text,
            Vector2 position, Vector2 size, bool topRight, Color top, Color bottom)
        {
            GameObject buttonObject = CreateImage(parent, name, Color.white, true, top, bottom);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            Center(rect, position, size);
            Button button = Undo.AddComponent<Button>(buttonObject);
            button.targetGraphic = buttonObject.GetComponent<Image>();
            CreateLabel(buttonObject.transform, name == "Close Button" ? "Close Label" : "Label",
                text, Vector2.zero, size - new Vector2(10f, 8f), 22f,
                TextAlignmentOptions.Center);
            return button;
        }

        private static GameObject CreateImage(Transform parent, string name, Color color,
            bool outlined, Color? top = null, Color? bottom = null)
        {
            GameObject value = new(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(value, "Create " + name);
            value.transform.SetParent(parent, false);
            Image image = value.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            if (top.HasValue && bottom.HasValue)
            {
                MiningCandyGradient gradient = Undo.AddComponent<MiningCandyGradient>(value);
                gradient.SetColors(top.Value, bottom.Value);
            }
            if (outlined)
            {
                Outline outline = Undo.AddComponent<Outline>(value);
                outline.effectColor = OutlineColor;
                outline.effectDistance = new Vector2(3f, -3f);
                Shadow shadow = Undo.AddComponent<Shadow>(value);
                shadow.effectColor = new Color(0.01f, 0.02f, 0.04f, 0.65f);
                shadow.effectDistance = new Vector2(0f, -6f);
            }
            return value;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
            Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject value = new(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI), typeof(Outline));
            Undo.RegisterCreatedObjectUndo(value, "Create " + name);
            value.transform.SetParent(parent, false);
            Center(value.GetComponent<RectTransform>(), position, size);
            TextMeshProUGUI label = value.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = alignment;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(10f, fontSize - 7f);
            label.fontSizeMax = fontSize;
            label.raycastTarget = false;
            Outline outline = value.GetComponent<Outline>();
            outline.effectColor = OutlineColor;
            outline.effectDistance = new Vector2(1.6f, -1.6f);
            return label;
        }

        private static void SetReference(SerializedObject serialized, string fieldName,
            Object value, bool apply = false)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property != null) property.objectReferenceValue = value;
            if (apply)
            {
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(serialized.targetObject);
            }
        }

        private static Canvas FindCanvas()
        {
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.name == "Mining HUD Canvas") return canvas;
            }
            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindDescendant(root.GetChild(index), name);
                if (found != null) return found;
            }
            return null;
        }

        private static T FindComponent<T>(Transform root, string name) where T : Component
        {
            Transform found = FindDescendant(root, name);
            return found != null ? found.GetComponent<T>() : null;
        }

        private static void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Top(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
