using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Builds an authored, editable trader offer panel under the existing mining HUD.</summary>
    public static class WanderingTraderPanelSetupMenu
    {
        private const string MenuPath = "Mining Simulator/Setup/Create Or Select Wandering Trader Offer Panel";

        [MenuItem(MenuPath)]
        private static void CreateOrSelect()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Exit Play Mode before creating the Wandering Trader Offer Panel.");
                return;
            }

            WanderingTraderPanel panel = Object.FindFirstObjectByType<WanderingTraderPanel>(
                FindObjectsInactive.Include);
            if (panel == null)
            {
                Canvas canvas = FindHudCanvas();
                if (canvas == null)
                {
                    Debug.LogWarning("Mining HUD Canvas was not found.");
                    return;
                }

                panel = BuildPanel(canvas.transform);
                EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            }

            Selection.activeGameObject = panel.gameObject;
            EditorGUIUtility.PingObject(panel.gameObject);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateCreateOrSelect() => !Application.isPlaying;

        private static WanderingTraderPanel BuildPanel(Transform parent)
        {
            GameObject root = Create("Wandering Trader Panel", parent);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            root.AddComponent<CanvasGroup>();
            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject card = Create("Offer Card", root.transform);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(720f, 390f);
            cardRect.anchoredPosition = Vector2.zero;
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.12f, 0.12f, 0.12f, 0.98f);

            TextMeshProUGUI title = CreateLabel("Title", card.transform, "WANDERING TRADER OFFER",
                30f, new Vector2(0f, 138f), new Vector2(650f, 52f), new Color(0.3f, 1f, 0.65f));
            TextMeshProUGUI offer = CreateLabel("Offer Text", card.transform, "YOU GIVE\nItem x1\n\n→\n\nYOU RECEIVE\n30 MONEY",
                28f, new Vector2(0f, 22f), new Vector2(620f, 215f), Color.white);
            TextMeshProUGUI feedback = CreateLabel("Feedback", card.transform, string.Empty, 20f,
                new Vector2(0f, -84f), new Vector2(600f, 34f), new Color(1f, 0.45f, 0.35f));
            Button trade = CreateButton("Trade Button", card.transform, "TRADE", new Vector2(-155f, -140f),
                new Color(0.15f, 0.55f, 0.25f));
            Button close = CreateButton("Close Button", card.transform, "CLOSE", new Vector2(155f, -140f),
                new Color(0.55f, 0.18f, 0.16f));

            WanderingTraderPanel panel = Undo.AddComponent<WanderingTraderPanel>(root);
            panel.ConfigureSceneUi(title, offer, feedback, trade, close);
            root.SetActive(false);
            return panel;
        }

        private static Canvas FindHudCanvas()
        {
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (canvas.name.Contains("Mining HUD Canvas"))
                {
                    return canvas;
                }
            }
            return null;
        }

        private static GameObject Create(string name, Transform parent)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(result, $"Create {name}");
            result.transform.SetParent(parent, false);
            return result;
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, string text,
            float fontSize, Vector2 position, Vector2 size, Color color)
        {
            GameObject result = Create(name, parent);
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            TextMeshProUGUI label = result.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }

        private static Button CreateButton(string name, Transform parent, string text, Vector2 position,
            Color color)
        {
            GameObject result = Create(name, parent);
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(245f, 58f);
            rect.anchoredPosition = position;
            Image image = result.AddComponent<Image>();
            image.color = color;
            Button button = result.AddComponent<Button>();
            CreateLabel("Label", result.transform, text, 24f, Vector2.zero, rect.sizeDelta, Color.white);
            return button;
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
