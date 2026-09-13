#if UNITY_EDITOR
using MiningSimulator.Ores;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Editor
{
    /// <summary>Authors one editable Computer info/upgrade panel into the current HUD Canvas.</summary>
    public static class MiningComputerPanelSetupMenu
    {
        private const string PanelName = "Computer Info Panel";
        private const string HudCanvasName = "Mining HUD Canvas";
        private const string UiDataPath = "Assets/GameData/UI/MiningUiData.asset";

        [MenuItem("Mining Simulator/Setup/Create or Update Computer Info Panel")]
        public static void CreateOrUpdate()
        {
            Canvas canvas = FindHudCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Computer Panel Setup",
                    "Mining HUD Canvas was not found in the open scene.", "OK");
                return;
            }

            MiningUiData uiData = AssetDatabase.LoadAssetAtPath<MiningUiData>(UiDataPath);
            Transform existing = canvas.transform.Find(PanelName);
            bool panelCreated = existing == null;
            RectTransform panel = panelCreated
                ? CreateUiObject(PanelName, canvas.transform, typeof(Image),
                    typeof(CanvasGroup)).GetComponent<RectTransform>()
                : existing as RectTransform;

            if (panelCreated)
            {
                ConfigureCentered(panel, Vector2.zero, new Vector2(680f, 520f));
                panel.GetComponent<Image>().color = uiData != null
                    ? uiData.PanelColor
                    : new Color(0.06f, 0.075f, 0.11f, 0.98f);
            }

            Image panelImage = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
            panelImage.raycastTarget = true;
            CanvasGroup group = panel.GetComponent<CanvasGroup>() ??
                                panel.gameObject.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            RectTransform header = EnsureRect(panel, "Header", out bool headerCreated,
                typeof(Image));
            if (headerCreated)
            {
                ConfigureTopStretch(header, 0f, 76f);
                header.GetComponent<Image>().color = uiData != null
                    ? uiData.HeaderColor
                    : new Color(0.15f, 0.65f, 0.95f, 1f);
                header.GetComponent<Image>().raycastTarget = false;
            }

            TextMeshProUGUI title = EnsureText(header, "Title", out bool titleCreated);
            if (titleCreated)
            {
                Stretch(title.rectTransform, new Vector2(28f, 8f), new Vector2(-92f, -8f));
                StyleText(title, 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
                    uiData != null ? uiData.TitleTextColor : Color.white);
                title.text = "COIN COMPUTER";
            }

            Button closeButton = EnsureButton(header, "Close", out bool closeCreated,
                uiData != null ? uiData.CloseButtonColor : new Color(0.9f, 0.2f, 0.2f, 1f),
                uiData);
            if (closeCreated)
            {
                ConfigureTopRight(closeButton.transform as RectTransform,
                    new Vector2(-12f, -11f), new Vector2(54f, 54f));
            }
            TextMeshProUGUI closeLabel = EnsureButtonLabel(closeButton, "X",
                out bool closeLabelCreated);
            if (closeLabelCreated)
            {
                StyleText(closeLabel, 25f, FontStyles.Bold, TextAlignmentOptions.Center,
                    Color.white);
            }

            TextMeshProUGUI level = EnsureBodyText(panel, "Level", new Vector2(0f, 138f),
                "Level: 1/10", new Vector2(590f, 46f), 25f, FontStyles.Bold,
                uiData != null ? uiData.CardTextColor : Color.white);
            TextMeshProUGUI income = EnsureBodyText(panel, "Income", new Vector2(0f, 78f),
                "Income: 10 every 2s", new Vector2(590f, 42f), 22f, FontStyles.Normal,
                uiData != null ? uiData.CardTextColor : Color.white);
            TextMeshProUGUI nextIncome = EnsureBodyText(panel, "Next Income",
                new Vector2(0f, 25f), "Next level: 60 every 2s",
                new Vector2(590f, 42f), 22f, FontStyles.Normal,
                uiData != null ? uiData.CardTextColor : Color.white);
            TextMeshProUGUI cost = EnsureBodyText(panel, "Upgrade Cost",
                new Vector2(0f, -35f), "Upgrade cost: 500",
                new Vector2(590f, 42f), 22f, FontStyles.Bold,
                new Color(1f, 0.75f, 0.18f, 1f));
            TextMeshProUGUI status = EnsureBodyText(panel, "Status",
                new Vector2(0f, -88f), "Ready to upgrade",
                new Vector2(590f, 36f), 18f, FontStyles.Italic,
                uiData != null ? uiData.CardTextColor : Color.white);

            Button upgradeButton = EnsureButton(panel, "Upgrade", out bool upgradeCreated,
                uiData != null ? uiData.NavigationButtonColor :
                    new Color(0.12f, 0.78f, 0.38f, 1f), uiData);
            if (upgradeCreated)
            {
                ConfigureCentered(upgradeButton.transform as RectTransform,
                    new Vector2(0f, -174f), new Vector2(360f, 66f));
            }
            TextMeshProUGUI upgradeLabel = EnsureButtonLabel(upgradeButton, "UPGRADE",
                out bool upgradeLabelCreated);
            if (upgradeLabelCreated)
            {
                StyleText(upgradeLabel, 23f, FontStyles.Bold, TextAlignmentOptions.Center,
                    uiData != null ? uiData.TitleTextColor : Color.white);
            }

            MiningComputerPanel controller = panel.GetComponent<MiningComputerPanel>() ??
                                             panel.gameObject.AddComponent<MiningComputerPanel>();
            MiningUiPanelCoordinator coordinator = Object.FindFirstObjectByType<
                MiningUiPanelCoordinator>(FindObjectsInactive.Include);
            controller.Configure(panel, coordinator, closeButton, upgradeButton, title, level,
                income, nextIncome, cost, status, upgradeLabel);

            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            EditorGUIUtility.PingObject(panel.gameObject);
            SceneView.lastActiveSceneView?.FrameSelected();
            EditorUtility.DisplayDialog("Computer Panel Setup",
                "Computer Info Panel is visible and selected for editing. It hides when Play " +
                "starts, then opens when the purchased computer is hovered and F is pressed.",
                "OK");
        }

        private static Canvas FindHudCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null && canvas.name == HudCanvasName)
                {
                    return canvas;
                }
            }
            return null;
        }

        private static RectTransform EnsureRect(Transform parent, string name,
            out bool created, params System.Type[] components)
        {
            Transform existing = parent.Find(name);
            created = existing == null;
            GameObject target = created ? CreateUiObject(name, parent, components) : existing.gameObject;
            foreach (System.Type component in components)
            {
                if (target.GetComponent(component) == null)
                {
                    target.AddComponent(component);
                }
            }
            return target.transform as RectTransform;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name, out bool created)
        {
            RectTransform rect = EnsureRect(parent, name, out created, typeof(TextMeshProUGUI));
            TextMeshProUGUI label = rect.GetComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            return label;
        }

        private static TextMeshProUGUI EnsureBodyText(RectTransform panel, string name,
            Vector2 position, string previewText, Vector2 size, float fontSize,
            FontStyles style, Color color)
        {
            TextMeshProUGUI label = EnsureText(panel, name, out bool created);
            if (created)
            {
                ConfigureCentered(label.rectTransform, position, size);
                StyleText(label, fontSize, style, TextAlignmentOptions.Center, color);
                label.text = previewText;
            }
            return label;
        }

        private static Button EnsureButton(Transform parent, string name, out bool created,
            Color color, MiningUiData uiData)
        {
            RectTransform rect = EnsureRect(parent, name, out created, typeof(Image), typeof(Button));
            Image image = rect.GetComponent<Image>();
            if (created)
            {
                image.color = color;
            }
            image.raycastTarget = true;
            Button button = rect.GetComponent<Button>();
            button.targetGraphic = image;
            if (created || rect.GetComponent<SmoothButtonPunch>() == null)
            {
                ConfigureSmoothButton(rect, uiData);
            }
            return button;
        }

        private static TextMeshProUGUI EnsureButtonLabel(Button button, string previewText,
            out bool created)
        {
            TextMeshProUGUI label = EnsureText(button.transform, "Label", out created);
            if (created)
            {
                Stretch(label.rectTransform, new Vector2(8f, 4f), new Vector2(-8f, -4f));
                label.text = previewText;
            }
            return label;
        }

        private static void ConfigureSmoothButton(RectTransform rect, MiningUiData uiData)
        {
            if (rect == null || uiData == null || !uiData.SmoothButtonAnimationEnabled)
            {
                return;
            }
            SmoothButtonPunch animation = rect.GetComponent<SmoothButtonPunch>() ??
                                            rect.gameObject.AddComponent<SmoothButtonPunch>();
            animation.Configure(uiData.ButtonHoverScale, uiData.ButtonHoverPunchScale,
                uiData.ButtonPressedScale, uiData.ButtonClickBounceScale,
                uiData.ButtonHoverPunchDuration, uiData.ButtonHoverSettleDuration,
                uiData.ButtonPressDuration, uiData.ButtonClickBounceDuration,
                uiData.ButtonClickSettleDuration);
        }

        private static GameObject CreateUiObject(string name, Transform parent,
            params System.Type[] components)
        {
            GameObject result = new(name, typeof(RectTransform));
            result.layer = parent.gameObject.layer;
            result.transform.SetParent(parent, false);
            foreach (System.Type component in components)
            {
                if (result.GetComponent(component) == null)
                {
                    result.AddComponent(component);
                }
            }
            Undo.RegisterCreatedObjectUndo(result, "Create " + name);
            return result;
        }

        private static void ConfigureCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ConfigureTopStretch(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -top - height);
            rect.offsetMax = new Vector2(0f, -top);
        }

        private static void ConfigureTopRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minimum;
            rect.offsetMax = maximum;
        }

        private static void StyleText(TextMeshProUGUI label, float fontSize,
            FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
        }
    }
}
#endif
