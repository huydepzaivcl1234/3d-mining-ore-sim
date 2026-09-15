#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Converts the existing purchase Button in place, preserving MiningHud's serialized reference.</summary>
    public static class MiningJuicyBuyMinerSetupMenu
    {
        private static readonly Color Leather = Hex("#9C5930");
        private static readonly Color LeatherDark = Hex("#3E1B0B");
        private static readonly Color Brass = Hex("#FFE285");
        private static readonly Color Stitch = Hex("#F3D4A6");

        [MenuItem("Mining Simulator/UI/Build Juicy Buy Miner Button")]
        public static void Build()
        {
            MiningHud hud = Object.FindFirstObjectByType<MiningHud>(FindObjectsInactive.Include);
            if (hud == null)
            {
                Debug.LogWarning("Open the mining scene first: MiningHud was not found. No scene was changed.");
                return;
            }

            SerializedObject hudFields = new(hud);
            Button button = hudFields.FindProperty("buyButton").objectReferenceValue as Button;
            if (button == null)
            {
                Transform shop = hud.transform.Find("NPC Shop");
                button = (shop?.Find("BuyMinerButton") ?? shop?.Find("Buy Mining NPC"))?.GetComponent<Button>();
                if (button == null)
                {
                    Debug.LogWarning("The existing MiningHud purchase Button was not found. No scene was changed.");
                    return;
                }
                hudFields.FindProperty("buyButton").objectReferenceValue = button;
                hudFields.ApplyModifiedProperties();
            }

            Undo.RecordObject(button.gameObject, "Restyle miner button");
            button.gameObject.name = "BuyMinerButton";
            RectTransform root = button.transform as RectTransform;
            if (root == null) return;
            Undo.RecordObject(root, "Resize miner button");
            root.sizeDelta = new Vector2(294f, 68f);
            Transform status = root.parent?.Find("Status");
            if (status is RectTransform statusRect)
            {
                Undo.RecordObject(statusRect, "Keep miner status below button");
                statusRect.anchoredPosition = new Vector2(statusRect.anchoredPosition.x, -242f);
            }

            // Disable only the superseded visuals; never remove the Button or its HUD onClick wiring.
            Transform oldLabel = root.Find("Label");
            if (oldLabel != null && oldLabel.gameObject.activeSelf)
            {
                Undo.RecordObject(oldLabel.gameObject, "Hide legacy miner label");
                oldLabel.gameObject.SetActive(false);
            }
            foreach (Transform child in root)
            {
                if (child.name == "Label" || child.name == "Base_Shadow" || child.name == "Button_Body") continue;
                if (child.GetComponent<Image>() == null) continue;
                Undo.RecordObject(child.gameObject, "Hide legacy miner decoration");
                child.gameObject.SetActive(false);
            }

            Image rootImage = EnsureImage(root);
            rootImage.color = new Color(1f, 1f, 1f, 0.001f);
            rootImage.raycastTarget = true;
            button.targetGraphic = rootImage;
            button.transition = Selectable.Transition.None;

            RectTransform shadow = Child(root, "Base_Shadow", new Vector2(294f, 68f), new Vector2(0f, -8f));
            StyleImage(shadow, Hex("#1B0A03"), Hex("#1B0A03"));
            Outline(shadow.gameObject, Hex("#110701"), new Vector2(2f, -2f));

            RectTransform body = Child(root, "Button_Body", new Vector2(294f, 68f), Vector2.zero);
            Image bodyImage = StyleImage(body, Hex("#B27039"), LeatherDark);
            Outline(body.gameObject, Hex("#281007"), new Vector2(2f, -2f));

            // Leather frame is drawn below the clipped shimmer so the light is visible.
            RectTransform frame = Child(body, "Frame_Border", new Vector2(282f, 57f), Vector2.zero);
            StyleImage(frame, Leather, LeatherDark);
            Outline(frame.gameObject, Stitch, new Vector2(1.5f, -1.5f));
            Shadow(frame.gameObject, Hex("#372216"), new Vector2(-2f, 1f));
            JuicyButtonTrim frameTrim = frame.GetComponent<JuicyButtonTrim>() ??
                                        Undo.AddComponent<JuicyButtonTrim>(frame.gameObject);
            frameTrim.SetMedalRivets(false);

            RectTransform mask = Child(body, "Mask", new Vector2(278f, 51f), Vector2.zero);
            Image maskImage = EnsureImage(mask);
            maskImage.color = Color.white;
            maskImage.raycastTarget = false;
            Mask clip = mask.GetComponent<Mask>() ?? Undo.AddComponent<Mask>(mask.gameObject);
            clip.showMaskGraphic = false;
            RectTransform shimmer = Child(mask, "Shimmer_FX", new Vector2(36f, 54f), new Vector2(-180f, 0f));
            Image shimmerImage = EnsureImage(shimmer);
            shimmerImage.color = new Color(1f, 0.93f, 0.66f, 0.15f);
            shimmerImage.raycastTarget = false;

            RectTransform avatar = Child(body, "Avatar_Group", new Vector2(55f, 54f), new Vector2(-109f, 0f));
            RectTransform medal = Child(avatar, "Medal_Brass", new Vector2(51f, 51f), Vector2.zero);
            StyleImage(medal, Brass, Hex("#59390A"));
            Outline(medal.gameObject, Hex("#241607"), new Vector2(1f, -1f));
            Shadow(medal.gameObject, Stitch, new Vector2(-1f, 1f));
            JuicyButtonTrim medalTrim = medal.GetComponent<JuicyButtonTrim>() ??
                                        Undo.AddComponent<JuicyButtonTrim>(medal.gameObject);
            medalTrim.SetMedalRivets(true);
            RectTransform miner = Child(avatar, "Miner_Icon", new Vector2(42f, 42f), Vector2.zero);
            MiningUiData uiData = AssetDatabase.LoadAssetAtPath<MiningUiData>("Assets/GameData/UI/MiningUiData.asset");
            Image minerImage = EnsureImage(miner);
            minerImage.sprite = uiData != null ? uiData.BuyNpcIconSprite : null;
            minerImage.color = Color.white;
            minerImage.preserveAspect = true;
            minerImage.raycastTarget = false;
            RectTransform plus = Child(avatar, "Plus_Badge", new Vector2(20f, 20f), new Vector2(20f, -19f));
            StyleImage(plus, Brass, Hex("#B47A19"));
            TextMeshProUGUI plusText = Text(plus, "Symbol", "+", 16f, Color.white,
                new Vector2(18f, 18f), Vector2.zero);
            plusText.alignment = TextAlignmentOptions.Center;

            RectTransform texts = Child(body, "Content_Texts", new Vector2(143f, 54f), new Vector2(-10f, 0f));
            TextMeshProUGUI title = Text(texts, "Title_Text", "HIRE MINER", 14f, Hex("#FFF0CA"),
                new Vector2(125f, 20f), new Vector2(-9f, 14f));
            title.fontStyle = FontStyles.Bold;
            TextMeshProUGUI count = Text(texts, "Count_Badge", "x1", 10f, Brass,
                new Vector2(30f, 18f), new Vector2(56f, 14f));
            count.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI stats = Text(texts, "SubStats_Text", "⚡ Mining rate: +0 ore/min", 9f,
                Hex("#C3F8D0"), new Vector2(139f, 28f), new Vector2(0f, -13f));

            RectTransform plaque = Child(body, "Price_Plaque", new Vector2(80f, 53f), new Vector2(101f, 0f));
            RectTransform plaqueBackground = Child(plaque, "Plaque_Bg", new Vector2(78f, 51f), Vector2.zero);
            StyleImage(plaqueBackground, Hex("#261309"), Hex("#150A05"));
            Outline(plaqueBackground.gameObject, Brass, new Vector2(1f, -1f));
            RectTransform coin = Child(plaque, "Coin_Icon", new Vector2(20f, 20f), new Vector2(-24f, -8f));
            Image coinImage = EnsureImage(coin);
            coinImage.sprite = uiData != null ? uiData.MoneyIconSprite : null;
            coinImage.color = Brass;
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;
            TextMeshProUGUI priceLabel = Text(plaque, "Label_Text", "HIRE COST", 9f, Stitch,
                new Vector2(72f, 16f), new Vector2(0f, 13f));
            priceLabel.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI cost = Text(plaque, "Cost_Text", "25", 16f, Brass,
                new Vector2(56f, 25f), new Vector2(10f, -9f));
            cost.fontStyle = FontStyles.Bold;
            cost.alignment = TextAlignmentOptions.Center;

            // Make the old HUD label irrelevant: the new localized labels are owned by the visual component.
            hudFields.Update();
            hudFields.FindProperty("buyButtonLabel").objectReferenceValue = null;
            hudFields.ApplyModifiedProperties();

            NpcShop npcShop = Object.FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            JuicyBuyMinerButton juicy = button.GetComponent<JuicyBuyMinerButton>() ??
                                       Undo.AddComponent<JuicyBuyMinerButton>(button.gameObject);
            SerializedObject fields = new(juicy);
            Set(fields, "buttonBody", body);
            Set(fields, "shimmerRect", shimmer);
            Set(fields, "titleText", title);
            Set(fields, "countBadgeText", count);
            Set(fields, "subStatsText", stats);
            Set(fields, "priceLabelText", priceLabel);
            Set(fields, "costText", cost);
            Set(fields, "npcShop", npcShop);
            fields.ApplyModifiedProperties();

            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(button);
            EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
            Debug.Log("Juicy BuyMinerButton built in the active scene. Purchase remains connected to MiningHud. Save the scene when satisfied.");
        }

        private static void Set(SerializedObject fields, string name, Object value) =>
            fields.FindProperty(name).objectReferenceValue = value;

        private static RectTransform Child(Transform parent, string name, Vector2 size, Vector2 position)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(obj, "Build miner button " + name);
                obj.transform.SetParent(parent, false);
            }
            else obj = existing.gameObject;
            RectTransform rect = obj.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Layout miner button " + name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image EnsureImage(RectTransform rect) =>
            rect.GetComponent<Image>() ?? Undo.AddComponent<Image>(rect.gameObject);

        private static Image StyleImage(RectTransform rect, Color top, Color bottom)
        {
            Image image = EnsureImage(rect);
            image.color = Color.white;
            image.raycastTarget = false;
            MiningUiGradient gradient = rect.GetComponent<MiningUiGradient>() ??
                                        Undo.AddComponent<MiningUiGradient>(rect.gameObject);
            gradient.SetColors(top, bottom);
            return image;
        }

        private static void Outline(GameObject obj, Color color, Vector2 distance)
        {
            UnityEngine.UI.Outline outline = obj.GetComponent<UnityEngine.UI.Outline>() ??
                                              Undo.AddComponent<UnityEngine.UI.Outline>(obj);
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void Shadow(GameObject obj, Color color, Vector2 distance)
        {
            UnityEngine.UI.Shadow shadow = null;
            foreach (UnityEngine.UI.Shadow effect in obj.GetComponents<UnityEngine.UI.Shadow>())
            {
                if (effect.GetType() == typeof(UnityEngine.UI.Shadow)) { shadow = effect; break; }
            }
            shadow ??= Undo.AddComponent<UnityEngine.UI.Shadow>(obj);
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value, float size,
            Color color, Vector2 dimensions, Vector2 position)
        {
            RectTransform rect = Child(parent, name, dimensions, position);
            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>() ??
                                   Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.enableAutoSizing = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
    }
}
#endif
