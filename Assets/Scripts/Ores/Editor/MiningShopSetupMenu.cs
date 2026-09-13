#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Authors the shared Gem Shop and its Rare Gift Box product.</summary>
    public static class MiningShopSetupMenu
    {
        private const string CanvasName = "Mining HUD Canvas";
        private const string PanelName = "Shop Panel";
        private const string OpenButtonName = "Shop Menu Button";
        private const string ShopDataFolder = "Assets/GameData/Shop";
        private const string ShopDataPath = ShopDataFolder + "/MiningShopData.asset";
        private const string RareGiftPath = "Assets/GameData/Items/Rare Gift Box.asset";

        [MenuItem("Mining Simulator/Setup/Create Or Update Shop Panel")]
        public static void CreateOrUpdateShopPanel()
        {
            Canvas canvas = FindHudCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Shop Setup",
                    $"Could not find the Canvas named '{CanvasName}' in the open scene.", "OK");
                return;
            }

            MiningItemData rareGift = AssetDatabase.LoadAssetAtPath<MiningItemData>(RareGiftPath);
            if (rareGift == null)
            {
                EditorUtility.DisplayDialog("Shop Setup",
                    $"Rare Gift Box was not found at '{RareGiftPath}'.", "OK");
                return;
            }

            MiningShopData shopData = LoadOrCreateShopData(rareGift);
            MiningUiData uiData = FindFirstAsset<MiningUiData>();
            RectTransform panel = EnsurePanel(canvas.transform, uiData, shopData);
            Button openButton = EnsureGameplayButton(canvas.transform, uiData);
            MiningShopPanel controller = canvas.GetComponent<MiningShopPanel>() ??
                                         Undo.AddComponent<MiningShopPanel>(canvas.gameObject);

            SerializedObject serialized = new(controller);
            SetReference(serialized, "data", shopData);
            SetReference(serialized, "gameData", FindFirstAsset<MiningGameData>());
            SetReference(serialized, "wallet", Object.FindFirstObjectByType<PlayerWallet>(
                FindObjectsInactive.Include));
            SetReference(serialized, "itemSystem", Object.FindFirstObjectByType<MiningItemSystem>(
                FindObjectsInactive.Include));
            SetReference(serialized, "panelCoordinator", Object.FindFirstObjectByType<
                MiningUiPanelCoordinator>(FindObjectsInactive.Include));
            SetReference(serialized, "panelRoot", panel);
            SetReference(serialized, "gameplayOpenButton", openButton);
            SetReference(serialized, "closeButton", FindComponent<Button>(panel, "Close Button"));
            SetReference(serialized, "buyRareGiftButton",
                FindComponent<Button>(panel, "Buy Rare Gift Button"));
            SetReference(serialized, "gameplayButtonLabel",
                FindComponent<TextMeshProUGUI>(openButton.transform, "Shop Button Label"));
            SetReference(serialized, "titleLabel",
                FindComponent<TextMeshProUGUI>(panel, "Shop Title"));
            SetReference(serialized, "gemBalanceLabel",
                FindComponent<TextMeshProUGUI>(panel, "Gem Balance"));
            SetReference(serialized, "itemNameLabel",
                FindComponent<TextMeshProUGUI>(panel, "Item Name"));
            SetReference(serialized, "itemDescriptionLabel",
                FindComponent<TextMeshProUGUI>(panel, "Item Description"));
            SetReference(serialized, "priceLabel",
                FindComponent<TextMeshProUGUI>(panel, "Gem Price"));
            SetReference(serialized, "buyLabel",
                FindComponent<TextMeshProUGUI>(panel, "Buy Label"));
            SetReference(serialized, "statusLabel",
                FindComponent<TextMeshProUGUI>(panel, "Shop Message"));
            SetReference(serialized, "itemIcon", FindComponent<Image>(panel, "Item Icon"));
            SetReference(serialized, "itemIconFallback",
                FindComponent<TextMeshProUGUI>(panel, "Item Icon Fallback"));
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);

            MiningMainMenuSetupMenu.EnsureShopButtonForCurrentMenu();
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            MiningButtonSfxSetupMenu.AssignAllButtonSfx(false);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            EditorGUIUtility.PingObject(panel.gameObject);
            EditorUtility.DisplayDialog("Shop Setup",
                "Shop is ready with Rare Gift Box for 100 Gems. Save the scene after editing.",
                "OK");
        }

        private static MiningShopData LoadOrCreateShopData(MiningItemData rareGift)
        {
            if (!AssetDatabase.IsValidFolder("Assets/GameData"))
            {
                AssetDatabase.CreateFolder("Assets", "GameData");
            }
            if (!AssetDatabase.IsValidFolder(ShopDataFolder))
            {
                AssetDatabase.CreateFolder("Assets/GameData", "Shop");
            }

            MiningShopData data = AssetDatabase.LoadAssetAtPath<MiningShopData>(ShopDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MiningShopData>();
                AssetDatabase.CreateAsset(data, ShopDataPath);
            }

            SerializedObject serialized = new(data);
            SerializedProperty product = serialized.FindProperty("rareGiftBox");
            if (product != null && product.objectReferenceValue == null)
            {
                product.objectReferenceValue = rareGift;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
            return data;
        }

        private static RectTransform EnsurePanel(Transform canvas, MiningUiData uiData,
            MiningShopData shopData)
        {
            MiningItemData rareGift = shopData.RareGiftBox;
            Transform existing = canvas.Find(PanelName);
            RectTransform overlay;
            if (existing == null)
            {
                GameObject overlayObject = CreateImage(canvas, PanelName,
                    new Color(0f, 0f, 0f, 0.74f));
                overlay = overlayObject.GetComponent<RectTransform>();
                Stretch(overlay);
                overlayObject.AddComponent<CanvasGroup>();
            }
            else
            {
                overlay = existing.GetComponent<RectTransform>();
                if (existing.GetComponent<CanvasGroup>() == null)
                {
                    Undo.AddComponent<CanvasGroup>(existing.gameObject);
                }
            }

            Transform cardTransform = overlay.Find("Shop Card");
            if (cardTransform == null)
            {
                Color cardColor = uiData != null ? uiData.InventoryPanelColor :
                    new Color(0.04f, 0.07f, 0.13f, 1f);
                GameObject cardObject = CreateImage(overlay, "Shop Card", cardColor);
                cardTransform = cardObject.transform;
                Center((RectTransform)cardTransform, Vector2.zero, new Vector2(780f, 560f));
            }
            RectTransform card = (RectTransform)cardTransform;

            Transform headerTransform = card.Find("Header");
            if (headerTransform == null)
            {
                Color headerColor = uiData != null ? uiData.InventoryHeaderColor :
                    new Color(0.52f, 0.27f, 0.88f, 1f);
                GameObject headerObject = CreateImage(card, "Header", headerColor);
                RectTransform header = headerObject.GetComponent<RectTransform>();
                header.anchorMin = new Vector2(0f, 1f);
                header.anchorMax = Vector2.one;
                header.pivot = new Vector2(0.5f, 1f);
                header.anchoredPosition = Vector2.zero;
                header.sizeDelta = new Vector2(0f, 78f);
                CreateLabel(header, "Shop Title", "GEM SHOP", Vector2.zero,
                    new Vector2(500f, 70f), 38f, Color.white, FontStyles.Bold);
                CreateButton(header, "Close Button", "Close Label", "X",
                    new Vector2(340f, -39f), new Vector2(64f, 56f),
                    new Color(0.82f, 0.16f, 0.24f, 1f), Color.white, 28f, uiData);
            }

            if (FindDescendant(card, "Gem Balance") == null)
            {
                CreateLabel(card, "Gem Balance", "GEMS: 0", new Vector2(0f, 202f),
                    new Vector2(650f, 48f), 25f, new Color(0.78f, 0.62f, 1f),
                    FontStyles.Bold);
            }
            if (card.Find("Rare Gift Product") == null)
            {
                CreateProductCard(card, uiData, rareGift, shopData.RareGiftBoxGemCost);
            }
            TextMeshProUGUI price = FindComponent<TextMeshProUGUI>(card, "Gem Price");
            if (price != null)
            {
                price.text = $"{MiningMoneyFormatter.Format(shopData.RareGiftBoxGemCost)} GEM";
            }

            TextMeshProUGUI status = FindComponent<TextMeshProUGUI>(card, "Shop Message");
            if (status == null)
            {
                status = CreateLabel(card, "Shop Message",
                    "Purchased gifts appear in Inventory.", new Vector2(0f, -230f),
                    new Vector2(680f, 48f), 20f, new Color(0.75f, 0.82f, 0.92f, 1f),
                    FontStyles.Normal);
            }
            else if (card.Find("Rare Gift Product") != null)
            {
                status.rectTransform.anchoredPosition = new Vector2(0f, -230f);
                status.rectTransform.sizeDelta = new Vector2(680f, 48f);
            }
            return overlay;
        }

        private static void CreateProductCard(RectTransform card, MiningUiData uiData,
            MiningItemData rareGift, float gemCost)
        {
            GameObject productObject = CreateImage(card, "Rare Gift Product",
                new Color(0.10f, 0.12f, 0.19f, 0.98f));
            RectTransform product = productObject.GetComponent<RectTransform>();
            Center(product, new Vector2(0f, -20f), new Vector2(660f, 330f));

            GameObject iconObject = CreateImage(product, "Item Icon", Color.white);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            Center(iconRect, new Vector2(-220f, 35f), new Vector2(150f, 150f));
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = rareGift.InventoryIcon;
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;
            TextMeshProUGUI fallback = CreateLabel(iconRect, "Item Icon Fallback",
                rareGift.IconFallback, Vector2.zero, new Vector2(140f, 140f), 62f,
                rareGift.FallbackColor, FontStyles.Bold);
            fallback.gameObject.SetActive(icon.sprite == null);

            CreateLabel(product, "Item Name", rareGift.DisplayName,
                new Vector2(85f, 105f), new Vector2(360f, 50f), 30f,
                new Color(0.90f, 0.72f, 1f, 1f), FontStyles.Bold);
            CreateLabel(product, "Item Description", rareGift.Description,
                new Vector2(85f, 37f), new Vector2(360f, 78f), 18f,
                new Color(0.78f, 0.84f, 0.95f, 1f), FontStyles.Normal);
            CreateLabel(product, "Gem Price", $"{MiningMoneyFormatter.Format(gemCost)} GEM",
                new Vector2(-80f, -105f),
                new Vector2(260f, 54f), 27f, new Color(0.78f, 0.62f, 1f),
                FontStyles.Bold);
            CreateButton(product, "Buy Rare Gift Button", "Buy Label", "BUY",
                new Vector2(190f, -105f), new Vector2(220f, 58f),
                new Color(0.52f, 0.27f, 0.88f, 1f), Color.white, 25f, uiData);
        }

        private static Button EnsureGameplayButton(Transform canvas, MiningUiData uiData)
        {
            Transform existing = canvas.Find(OpenButtonName);
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }
            Button button = CreateButton(canvas, OpenButtonName, "Shop Button Label", "SHOP",
                Vector2.zero, new Vector2(180f, 52f),
                new Color(0.52f, 0.27f, 0.88f, 1f), Color.white, 24f, uiData);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -342f);
            return button;
        }

        private static Button CreateButton(Transform parent, string name, string labelName,
            string text, Vector2 position, Vector2 size, Color color, Color textColor,
            float fontSize, MiningUiData uiData)
        {
            GameObject buttonObject = CreateImage(parent, name, color);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            Center(rect, position, size);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            SmoothButtonPunch punch = buttonObject.AddComponent<SmoothButtonPunch>();
            punch.SetTarget(rect);
            if (uiData != null)
            {
                punch.Configure(uiData.ButtonHoverScale, uiData.ButtonHoverPunchScale,
                    uiData.ButtonPressedScale, uiData.ButtonClickBounceScale,
                    uiData.ButtonHoverPunchDuration, uiData.ButtonHoverSettleDuration,
                    uiData.ButtonPressDuration, uiData.ButtonClickBounceDuration,
                    uiData.ButtonClickSettleDuration);
            }
            CreateLabel(rect, labelName, text, Vector2.zero, size, fontSize, textColor,
                FontStyles.Bold);
            return button;
        }

        private static GameObject CreateImage(Transform parent, string name, Color color)
        {
            GameObject value = new(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(value, "Create " + name);
            value.transform.SetParent(parent, false);
            value.GetComponent<Image>().color = color;
            return value;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
            Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style)
        {
            GameObject labelObject = new(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelObject, "Create " + name);
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            Center(label.rectTransform, position, size);
            return label;
        }

        private static Canvas FindHudCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null && canvas.name == CanvasName)
                {
                    return canvas;
                }
            }
            return null;
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        private static T FindComponent<T>(Transform root, string name) where T : Component
        {
            Transform value = FindDescendant(root, name);
            return value != null ? value.GetComponent<T>() : null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }

        private static void SetReference(SerializedObject serialized, string name, Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
#endif
