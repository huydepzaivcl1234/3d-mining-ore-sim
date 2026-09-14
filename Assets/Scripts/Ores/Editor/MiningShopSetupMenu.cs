#if UNITY_EDITOR
using System.IO;
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
        private const string ApplePath = "Assets/GameData/Items/Apple.asset";
        private const string BananaPath = "Assets/GameData/Items/Banana.asset";
        private const string MoneyIconPath = "Assets/Prefabs/UI/UpgradeMoneyReward.png";
        private const string GemIconPath = "Assets/Ores/Icons/GemCurrencyIcon.png";
        private const string GeneratedUiFolder = "Assets/Generated/MiningUI";
        private const string WheelSpritePath = GeneratedUiFolder + "/LuckyWheelCircle.png";
        private static bool syncQueued;

        [InitializeOnLoadMethod]
        private static void QueueSyncAfterScriptsReload()
        {
            QueueOpenPanelSync();
        }

        [MenuItem("Mining Simulator/Setup/Create Or Update Shop Panel")]
        public static void CreateOrUpdateShopPanel()
        {
            CreateOrUpdateShopPanelInternal(true, true);
        }

        internal static void QueueOpenPanelSync()
        {
            if (syncQueued) return;
            syncQueued = true;
            EditorApplication.delayCall += () =>
            {
                syncQueued = false;
                SyncOpenShopPanelFromData();
            };
        }

        private static void SyncOpenShopPanelFromData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                return;
            }
            Canvas canvas = FindHudCanvas();
            if (canvas == null || canvas.transform.Find(PanelName) == null)
            {
                return;
            }
            CreateOrUpdateShopPanelInternal(false, false);
        }

        private static void CreateOrUpdateShopPanelInternal(bool showDialogs, bool applyTheme)
        {
            Canvas canvas = FindHudCanvas();
            if (canvas == null)
            {
                if (showDialogs)
                {
                    EditorUtility.DisplayDialog("Shop Setup",
                        $"Could not find the Canvas named '{CanvasName}' in the open scene.",
                        "OK");
                }
                return;
            }

            MiningItemData rareGift = AssetDatabase.LoadAssetAtPath<MiningItemData>(RareGiftPath);
            MiningShopData shopData = LoadOrCreateShopData(rareGift);
            MiningUiData uiData = FindFirstAsset<MiningUiData>();
            RectTransform panel = EnsurePanel(canvas.transform, uiData, shopData);
            Button openButton = EnsureGameplayButton(canvas.transform, uiData);
            MiningItemSystem itemSystem = Object.FindFirstObjectByType<MiningItemSystem>(
                FindObjectsInactive.Include);
            RegisterShopProductsInItemDatabase(shopData, itemSystem != null
                ? itemSystem.Database
                : null);
            MiningShopPanel controller = canvas.GetComponent<MiningShopPanel>() ??
                                         Undo.AddComponent<MiningShopPanel>(canvas.gameObject);

            SerializedObject serialized = new(controller);
            SetReference(serialized, "data", shopData);
            SetReference(serialized, "gameData", FindFirstAsset<MiningGameData>());
            SetReference(serialized, "wallet", Object.FindFirstObjectByType<PlayerWallet>(
                FindObjectsInactive.Include));
            SetReference(serialized, "itemSystem", itemSystem);
            SetReference(serialized, "panelCoordinator", Object.FindFirstObjectByType<
                MiningUiPanelCoordinator>(FindObjectsInactive.Include));
            SetReference(serialized, "audioManager", Object.FindFirstObjectByType<
                MiningAudioManager>(FindObjectsInactive.Include));
            SetReference(serialized, "panelRoot", panel);
            SetReference(serialized, "gameplayOpenButton", openButton);
            SetReference(serialized, "gemHud",
                canvas.transform.Find("Gem HUD")?.GetComponent<RectTransform>());
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
            SetReference(serialized, "wheelRoot",
                FindComponent<RectTransform>(panel, "Wheel Root"));
            SetReference(serialized, "spinOnceButton",
                FindComponent<Button>(panel, "Spin Once Button"));
            SetReference(serialized, "spinTenButton",
                FindComponent<Button>(panel, "Spin Ten Button"));
            SetReference(serialized, "wheelResultPanel",
                FindComponent<RectTransform>(panel, "Wheel Result Panel"));
            SetReference(serialized, "wheelResultGroup",
                FindComponent<CanvasGroup>(panel, "Wheel Result Panel"));
            SetReference(serialized, "wheelTitleLabel",
                FindComponent<TextMeshProUGUI>(panel, "Wheel Title"));
            SetReference(serialized, "spinOnceLabel",
                FindComponent<TextMeshProUGUI>(panel, "Spin Once Label"));
            SetReference(serialized, "spinTenLabel",
                FindComponent<TextMeshProUGUI>(panel, "Spin Ten Label"));
            SetReference(serialized, "wheelResultsLabel",
                FindComponent<TextMeshProUGUI>(panel, "Wheel Results Label"));
            WireProductViews(serialized, panel, shopData);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);

            MiningUiPanelCoordinator coordinator = Object.FindFirstObjectByType<
                MiningUiPanelCoordinator>(FindObjectsInactive.Include);
            if (coordinator != null)
            {
                SerializedObject coordinatorSerialized = new(coordinator);
                SetReference(coordinatorSerialized, "gemHud",
                    canvas.transform.Find("Gem HUD")?.GetComponent<RectTransform>());
                SetReference(coordinatorSerialized, "shopMenuButton",
                    openButton.transform as RectTransform);
                coordinatorSerialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(coordinator);
            }

            if (showDialogs)
            {
                MiningMainMenuSetupMenu.EnsureShopButtonForCurrentMenu();
            }
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            if (applyTheme)
            {
                MiningCandyUiSetupMenu.ApplyCandyThemeFromSetup();
                MiningButtonSfxSetupMenu.AssignAllButtonSfx(false);
            }
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            if (showDialogs)
            {
                Selection.activeGameObject = panel.gameObject;
                EditorGUIUtility.PingObject(panel.gameObject);
                EditorUtility.DisplayDialog("Shop Setup",
                    "Shop is ready. Add products and Wheel Rewards in MiningShopData, then " +
                    "save the scene.", "OK");
            }
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
            if (product != null && product.objectReferenceValue == null && rareGift != null)
            {
                product.objectReferenceValue = rareGift;
            }
            SerializedProperty products = serialized.FindProperty("products");
            SerializedProperty schemaVersion = serialized.FindProperty("productSchemaVersion");
            if (products != null && schemaVersion != null && schemaVersion.intValue < 1)
            {
                MiningItemData legacyItem = product != null
                    ? product.objectReferenceValue as MiningItemData
                    : rareGift;
                if (products.arraySize == 0 && legacyItem != null)
                {
                    products.arraySize = 1;
                    ConfigureShopProduct(products.GetArrayElementAtIndex(0), legacyItem, 1,
                        serialized.FindProperty("rareGiftBoxGemCost")?.floatValue ?? 100f);
                }
                schemaVersion.intValue = 1;
            }
            SerializedProperty rewards = serialized.FindProperty("wheelRewards");
            if (rewards != null && rewards.arraySize == 0)
            {
                MiningItemData apple = AssetDatabase.LoadAssetAtPath<MiningItemData>(ApplePath);
                MiningItemData banana = AssetDatabase.LoadAssetAtPath<MiningItemData>(BananaPath);
                rewards.arraySize = 5;
                ConfigureWheelReward(rewards.GetArrayElementAtIndex(0),
                    MiningShopWheelRewardType.Money, 50f, 250f, null,
                    string.Empty, string.Empty, new Color(1f, 0.66f, 0.08f));
                ConfigureWheelReward(rewards.GetArrayElementAtIndex(1),
                    MiningShopWheelRewardType.Gems, 25f, 5f, null,
                    string.Empty, string.Empty, new Color(0.31f, 0.85f, 1f));
                ConfigureWheelReward(rewards.GetArrayElementAtIndex(2),
                    MiningShopWheelRewardType.Item, 14f, 0f, apple,
                    string.Empty, string.Empty, new Color(1f, 0.26f, 0.25f));
                ConfigureWheelReward(rewards.GetArrayElementAtIndex(3),
                    MiningShopWheelRewardType.Item, 8f, 0f, banana,
                    string.Empty, string.Empty, new Color(1f, 0.85f, 0.18f));
                ConfigureWheelReward(rewards.GetArrayElementAtIndex(4),
                    MiningShopWheelRewardType.Item, 3f, 0f, rareGift,
                    string.Empty, string.Empty, new Color(0.62f, 0.24f, 0.95f));
            }
            AssignMissingCurrencyIcons(rewards);
            bool changed = serialized.hasModifiedProperties;
            serialized.ApplyModifiedProperties();
            if (changed)
            {
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
            }
            return data;
        }

        private static void ConfigureShopProduct(SerializedProperty product,
            MiningItemData item, int amount, float gemCost)
        {
            product.FindPropertyRelative("item").objectReferenceValue = item;
            product.FindPropertyRelative("itemAmount").intValue = Mathf.Max(1, amount);
            product.FindPropertyRelative("gemCost").floatValue = Mathf.Max(0f, gemCost);
        }

        private static void RegisterShopProductsInItemDatabase(MiningShopData shopData,
            MiningItemDatabase database)
        {
            if (shopData == null || database == null) return;
            SerializedObject serialized = new(database);
            SerializedProperty items = serialized.FindProperty("items");
            if (items == null) return;

            bool changed = false;
            foreach (MiningShopProduct product in shopData.Products)
            {
                MiningItemData item = product != null ? product.Item : null;
                if (item == null) continue;
                bool found = false;
                for (int index = 0; index < items.arraySize; index++)
                {
                    if (items.GetArrayElementAtIndex(index).objectReferenceValue == item)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) continue;
                items.InsertArrayElementAtIndex(items.arraySize);
                items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = item;
                changed = true;
            }
            if (!changed) return;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        private static void WireProductViews(SerializedObject controller, RectTransform panel,
            MiningShopData shopData)
        {
            RectTransform content = FindComponent<RectTransform>(panel, "Product Content");
            SerializedProperty views = controller.FindProperty("productViews");
            if (views == null) return;

            views.arraySize = shopData.Products.Count;
            for (int index = 0; index < shopData.Products.Count; index++)
            {
                RectTransform row = FindProductRow(content, index);
                SerializedProperty view = views.GetArrayElementAtIndex(index);
                SetReference(view, "root", row != null ? row.gameObject : null);
                SetReference(view, "canvasGroup", row != null
                    ? row.GetComponent<CanvasGroup>()
                    : null);
                SetReference(view, "icon", FindComponent<Image>(row, "Item Icon"));
                SetReference(view, "iconFallback",
                    FindComponent<TextMeshProUGUI>(row, "Item Icon Fallback"));
                SetReference(view, "nameLabel",
                    FindComponent<TextMeshProUGUI>(row, "Item Name"));
                SetReference(view, "descriptionLabel",
                    FindComponent<TextMeshProUGUI>(row, "Item Description"));
                SetReference(view, "amountLabel",
                    FindComponent<TextMeshProUGUI>(row, "Item Amount"));
                SetReference(view, "priceLabel",
                    FindComponent<TextMeshProUGUI>(row, "Gem Price"));
                Button buyButton = FindComponent<Button>(row, "Buy Product Button") ??
                                   FindComponent<Button>(row, "Buy Rare Gift Button");
                SetReference(view, "buyButton", buyButton);
                SetReference(view, "buyLabel",
                    FindComponent<TextMeshProUGUI>(row, "Buy Label"));
            }
        }

        private static void AssignMissingCurrencyIcons(SerializedProperty rewards)
        {
            if (rewards == null)
            {
                return;
            }
            Sprite moneyIcon = AssetDatabase.LoadAssetAtPath<Sprite>(MoneyIconPath);
            Sprite gemIcon = AssetDatabase.LoadAssetAtPath<Sprite>(GemIconPath);
            for (int index = 0; index < rewards.arraySize; index++)
            {
                SerializedProperty reward = rewards.GetArrayElementAtIndex(index);
                SerializedProperty icon = reward.FindPropertyRelative("icon");
                if (icon == null || icon.objectReferenceValue != null)
                {
                    continue;
                }
                MiningShopWheelRewardType type = (MiningShopWheelRewardType)
                    reward.FindPropertyRelative("rewardType").enumValueIndex;
                if (type == MiningShopWheelRewardType.Money)
                {
                    icon.objectReferenceValue = moneyIcon;
                }
                else if (type == MiningShopWheelRewardType.Gems)
                {
                    icon.objectReferenceValue = gemIcon;
                }
            }
        }

        private static void ConfigureWheelReward(SerializedProperty reward,
            MiningShopWheelRewardType type, float weight, float currency,
            MiningItemData item, string english, string vietnamese, Color color)
        {
            reward.FindPropertyRelative("rewardType").enumValueIndex = (int)type;
            reward.FindPropertyRelative("weight").floatValue = weight;
            reward.FindPropertyRelative("currencyAmount").floatValue = currency;
            reward.FindPropertyRelative("item").objectReferenceValue = item;
            reward.FindPropertyRelative("itemAmount").intValue = 1;
            reward.FindPropertyRelative("englishName").stringValue = english;
            reward.FindPropertyRelative("vietnameseName").stringValue = vietnamese;
            reward.FindPropertyRelative("wheelColor").colorValue = color;
        }

        private static RectTransform EnsurePanel(Transform canvas, MiningUiData uiData,
            MiningShopData shopData)
        {
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
            Center(card, Vector2.zero, new Vector2(1120f, 720f));

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
            RectTransform headerRect = card.Find("Header") as RectTransform;
            if (headerRect != null)
            {
                headerRect.anchorMin = new Vector2(0f, 1f);
                headerRect.anchorMax = Vector2.one;
                headerRect.pivot = new Vector2(0.5f, 1f);
                headerRect.anchoredPosition = Vector2.zero;
                headerRect.sizeDelta = new Vector2(0f, 78f);
            }
            RectTransform closeRect = FindComponent<RectTransform>(card, "Close Button");
            if (closeRect != null)
            {
                closeRect.anchoredPosition = new Vector2(510f, -39f);
            }

            TextMeshProUGUI gemBalance = FindComponent<TextMeshProUGUI>(card, "Gem Balance");
            if (gemBalance == null)
            {
                gemBalance = CreateLabel(card, "Gem Balance", "GEMS: 0", new Vector2(285f, 265f),
                    new Vector2(500f, 48f), 25f, new Color(0.78f, 0.62f, 1f),
                    FontStyles.Bold);
            }
            gemBalance.rectTransform.anchoredPosition = new Vector2(285f, 265f);
            gemBalance.rectTransform.sizeDelta = new Vector2(500f, 48f);
            EnsureProductList(card, uiData, shopData);
            EnsureLuckyWheel(card, uiData, shopData);

            TextMeshProUGUI status = FindComponent<TextMeshProUGUI>(card, "Shop Message");
            if (status == null)
            {
                status = CreateLabel(card, "Shop Message",
                    "Edit rewards and rates in MiningShopData.", new Vector2(0f, -325f),
                    new Vector2(1000f, 48f), 20f, new Color(0.75f, 0.82f, 0.92f, 1f),
                    FontStyles.Normal);
            }
            else
            {
                status.rectTransform.anchoredPosition = new Vector2(0f, -325f);
                status.rectTransform.sizeDelta = new Vector2(1000f, 48f);
            }
            status.text = string.Empty;
            return overlay;
        }

        private static RectTransform EnsureProductList(RectTransform card, MiningUiData uiData,
            MiningShopData shopData)
        {
            RectTransform scroll = card.Find("Product Scroll View") as RectTransform;
            if (scroll == null)
            {
                scroll = CreateImage(card, "Product Scroll View", Color.clear)
                    .GetComponent<RectTransform>();
            }
            Center(scroll, new Vector2(285f, 130f), new Vector2(510f, 190f));
            ConfigureTransparentScrollGraphic(scroll.gameObject);
            ConfigureCanvasGroup(scroll.gameObject);

            RectTransform viewport = scroll.Find("Viewport") as RectTransform;
            if (viewport == null)
            {
                viewport = CreateImage(scroll, "Viewport", Color.clear)
                    .GetComponent<RectTransform>();
            }
            Stretch(viewport);
            ConfigureTransparentScrollGraphic(viewport.gameObject);
            if (viewport.GetComponent<RectMask2D>() == null)
            {
                Undo.AddComponent<RectMask2D>(viewport.gameObject);
            }

            RectTransform content = viewport.Find("Product Content") as RectTransform;
            if (content == null)
            {
                GameObject contentObject = new("Product Content", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(contentObject, "Create Shop Product Content");
                contentObject.transform.SetParent(viewport, false);
                content = contentObject.GetComponent<RectTransform>();
            }
            float height = Mathf.Max(190f, shopData.Products.Count * 92f +
                                           Mathf.Max(0, shopData.Products.Count - 1) * 10f);
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(500f, height);

            RectTransform legacy = card.Find("Rare Gift Product") as RectTransform;
            if (legacy != null && legacy.parent != content)
            {
                Undo.SetTransformParent(legacy, content,
                    "Move Existing Shop Product Into Scroll View");
            }
            for (int index = 0; index < shopData.Products.Count; index++)
            {
                EnsureProductRow(content, uiData, shopData.Products[index], index);
            }
            DisableExtraProductRows(content, shopData.Products.Count);

            ScrollRect scrollRect = scroll.GetComponent<ScrollRect>() ??
                                    Undo.AddComponent<ScrollRect>(scroll.gameObject);
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.12f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 35f;
            return content;
        }

        private static RectTransform EnsureProductRow(RectTransform content,
            MiningUiData uiData, MiningShopProduct product, int index)
        {
            RectTransform row = FindProductRow(content, index);
            if (row == null)
            {
                string name = index == 0 ? "Rare Gift Product" : $"Shop Product {index + 1}";
                row = CreateImage(content, name,
                    new Color(0.10f, 0.12f, 0.19f, 0.98f)).GetComponent<RectTransform>();
            }
            row.gameObject.SetActive(true);
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -index * 102f);
            row.sizeDelta = new Vector2(490f, 92f);
            ConfigureCanvasGroup(row.gameObject);
            EnsureCandySurface(row.gameObject, new Color(0.12f, 0.17f, 0.27f, 1f),
                new Color(0.04f, 0.07f, 0.13f, 1f));

            MiningItemData item = product != null ? product.Item : null;
            Image icon = FindComponent<Image>(row, "Item Icon");
            if (icon == null)
            {
                icon = CreateImage(row, "Item Icon", Color.white).GetComponent<Image>();
            }
            icon.sprite = item != null ? item.InventoryIcon : null;
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;
            SetRect(icon.rectTransform, new Vector2(-205f, 0f), new Vector2(68f, 68f));

            TextMeshProUGUI fallback = FindComponent<TextMeshProUGUI>(row,
                "Item Icon Fallback");
            if (fallback == null)
            {
                fallback = CreateLabel(icon.rectTransform, "Item Icon Fallback", "?",
                    Vector2.zero, new Vector2(64f, 64f), 34f, Color.white, FontStyles.Bold);
            }
            fallback.text = item != null ? item.IconFallback : "?";
            fallback.color = item != null ? item.FallbackColor : Color.magenta;
            fallback.gameObject.SetActive(icon.sprite == null);

            TextMeshProUGUI nameLabel = EnsureProductLabel(row, "Item Name", 18f,
                FontStyles.Bold);
            SetRect(nameLabel.rectTransform, new Vector2(-85f, 24f), new Vector2(185f, 30f));
            nameLabel.text = item != null ? item.DisplayName : "ITEM";
            TextMeshProUGUI description = EnsureProductLabel(row, "Item Description", 13f,
                FontStyles.Normal);
            SetRect(description.rectTransform, new Vector2(-72f, -14f),
                new Vector2(210f, 42f));
            description.text = item != null ? item.Description : string.Empty;
            TextMeshProUGUI amount = EnsureProductLabel(row, "Item Amount", 16f,
                FontStyles.Bold);
            SetRect(amount.rectTransform, new Vector2(65f, 25f), new Vector2(70f, 28f));
            amount.text = $"x{(product != null ? product.ItemAmount : 1)}";
            TextMeshProUGUI price = EnsureProductLabel(row, "Gem Price", 16f,
                FontStyles.Bold);
            SetRect(price.rectTransform, new Vector2(65f, -20f), new Vector2(125f, 32f));
            price.text = $"{MiningMoneyFormatter.Format(product != null ? product.GemCost : 0f)} GEM";

            Button buy = FindComponent<Button>(row, "Buy Product Button") ??
                         FindComponent<Button>(row, "Buy Rare Gift Button");
            if (buy == null)
            {
                buy = CreateButton(row, "Buy Product Button", "Buy Label", "BUY",
                    new Vector2(190f, 0f), new Vector2(105f, 50f),
                    new Color(0.10f, 0.70f, 0.52f, 1f), Color.white, 18f, uiData);
            }
            SetRect(buy.transform as RectTransform, new Vector2(190f, 0f),
                new Vector2(105f, 50f));
            EnsureCandySurface(buy.gameObject, new Color(0.20f, 0.84f, 0.69f, 1f),
                new Color(0.08f, 0.49f, 0.36f, 1f));
            return row;
        }

        private static TextMeshProUGUI EnsureProductLabel(Transform row, string name,
            float fontSize, FontStyles style)
        {
            TextMeshProUGUI label = FindComponent<TextMeshProUGUI>(row, name);
            if (label == null)
            {
                label = CreateLabel(row, name, string.Empty, Vector2.zero,
                    new Vector2(100f, 30f), fontSize, Color.white, style);
            }
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(10f, fontSize - 4f);
            label.fontSizeMax = fontSize;
            return label;
        }

        private static RectTransform FindProductRow(RectTransform content, int index)
        {
            if (content == null) return null;
            string name = index == 0 ? "Rare Gift Product" : $"Shop Product {index + 1}";
            return content.Find(name) as RectTransform;
        }

        private static void DisableExtraProductRows(RectTransform content, int productCount)
        {
            if (content == null) return;
            for (int index = 0; index < content.childCount; index++)
            {
                Transform child = content.GetChild(index);
                bool productRow = child.name == "Rare Gift Product" ||
                                  child.name.StartsWith("Shop Product ",
                                      System.StringComparison.Ordinal);
                if (!productRow) continue;
                int rowIndex = child.name == "Rare Gift Product" ? 0 :
                    int.TryParse(child.name.Substring("Shop Product ".Length), out int number)
                        ? number - 1
                        : int.MaxValue;
                child.gameObject.SetActive(rowIndex < productCount);
            }
        }

        private static void ConfigureCanvasGroup(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>() ??
                                Undo.AddComponent<CanvasGroup>(target);
            Undo.RecordObject(group, "Configure Shop Canvas Group");
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            EditorUtility.SetDirty(group);
        }

        private static void ConfigureTransparentScrollGraphic(GameObject target)
        {
            Image image = target.GetComponent<Image>() ?? Undo.AddComponent<Image>(target);
            MiningCandyGradient gradient = target.GetComponent<MiningCandyGradient>();
            if (gradient != null) Undo.DestroyObjectImmediate(gradient);
            foreach (Shadow effect in target.GetComponents<Shadow>())
            {
                Undo.DestroyObjectImmediate(effect);
            }
            Transform highlight = target.transform.Find("Candy Highlight");
            if (highlight != null) Undo.DestroyObjectImmediate(highlight.gameObject);
            Undo.RecordObject(image, "Configure Transparent Shop Scroll Graphic");
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Color.clear;
            image.raycastTarget = true;
            EditorUtility.SetDirty(image);
        }

        private static void EnsureCandySurface(GameObject target, Color top, Color bottom)
        {
            MiningCandyGradient gradient = target.GetComponent<MiningCandyGradient>() ??
                                            Undo.AddComponent<MiningCandyGradient>(target);
            gradient.SetColors(top, bottom);
            if (target.GetComponent<Outline>() == null)
            {
                Outline outline = Undo.AddComponent<Outline>(target);
                outline.effectColor = new Color(0.035f, 0.065f, 0.12f, 1f);
                outline.effectDistance = new Vector2(3f, -3f);
            }
            bool hasPlainShadow = false;
            foreach (Shadow effect in target.GetComponents<Shadow>())
            {
                if (effect.GetType() == typeof(Shadow))
                {
                    hasPlainShadow = true;
                    break;
                }
            }
            if (!hasPlainShadow)
            {
                Shadow shadow = Undo.AddComponent<Shadow>(target);
                shadow.effectColor = new Color(0.01f, 0.02f, 0.04f, 0.65f);
                shadow.effectDistance = new Vector2(0f, -5f);
            }
            EditorUtility.SetDirty(gradient);
        }

        private static void EnsureLuckyWheel(RectTransform card, MiningUiData uiData,
            MiningShopData shopData)
        {
            RectTransform wheelRoot = FindComponent<RectTransform>(card, "Wheel Root");
            if (wheelRoot == null)
            {
                GameObject wheelObject = CreateImage(card, "Wheel Root", Color.white);
                wheelRoot = wheelObject.GetComponent<RectTransform>();
            }
            Center(wheelRoot, new Vector2(-285f, 5f), new Vector2(350f, 350f));
            Image wheelBackground = wheelRoot.GetComponent<Image>();
            wheelBackground.color = new Color(0.06f, 0.10f, 0.18f, 1f);
            wheelBackground.enabled = false;
            Sprite wheelSprite = LoadOrCreateWheelSprite();
            RectTransform labelsRoot = FindComponent<RectTransform>(wheelRoot, "Wheel Labels");
            if (labelsRoot == null)
            {
                GameObject labelsObject = new("Wheel Labels", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(labelsObject, "Create Wheel Labels Layer");
                labelsObject.transform.SetParent(wheelRoot, false);
                labelsRoot = labelsObject.GetComponent<RectTransform>();
            }
            Stretch(labelsRoot);

            int segmentCount = Mathf.Max(1, shopData.WheelRewards.Count);
            for (int index = 0; index < segmentCount; index++)
            {
                MiningShopWheelReward reward = shopData.WheelRewards[index];
                string segmentName = $"Wheel Segment {index + 1}";
                RectTransform segment = FindComponent<RectTransform>(wheelRoot, segmentName);
                if (segment == null)
                {
                    GameObject segmentObject = CreateImage(wheelRoot, segmentName,
                        reward != null ? reward.WheelColor : Color.gray);
                    segment = segmentObject.GetComponent<RectTransform>();
                }
                Stretch(segment);
                segment.localRotation = Quaternion.Euler(0f, 0f, index * 360f / segmentCount);
                Image segmentImage = segment.GetComponent<Image>();
                segment.gameObject.SetActive(true);
                segmentImage.sprite = wheelSprite;
                segmentImage.type = Image.Type.Filled;
                segmentImage.fillMethod = Image.FillMethod.Radial360;
                segmentImage.fillOrigin = (int)Image.Origin360.Top;
                segmentImage.fillClockwise = true;
                segmentImage.fillAmount = 1f / segmentCount;
                segmentImage.color = reward != null ? reward.WheelColor : Color.gray;
                segmentImage.raycastTarget = false;

                string contentName = $"Reward Content {index + 1}";
                RectTransform content = FindComponent<RectTransform>(segment, "Reward Content");
                content ??= labelsRoot.Find(contentName) as RectTransform;
                if (content == null)
                {
                    GameObject contentObject = new(contentName, typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(contentObject, "Create Wheel Reward Content");
                    contentObject.transform.SetParent(labelsRoot, false);
                    content = contentObject.GetComponent<RectTransform>();
                }
                else
                {
                    content.name = contentName;
                    if (content.parent != labelsRoot)
                    {
                        Undo.SetTransformParent(content, labelsRoot,
                            "Move Rewards Above Wheel Segments");
                    }
                }
                content.gameObject.SetActive(true);

                TextMeshProUGUI label = FindComponent<TextMeshProUGUI>(content, "Reward Label");
                if (label == null)
                {
                    label = CreateLabel(content, "Reward Label", string.Empty,
                        Vector2.zero, new Vector2(108f, 54f), 15f, Color.white,
                        FontStyles.Bold);
                }
                else if (label.transform.parent != content)
                {
                    Undo.SetTransformParent(label.transform, content,
                        "Move Wheel Reward Label Beside Icon");
                }

                float stepAngle = 360f / segmentCount;
                float middleRadians = (index + 0.5f) * stepAngle * Mathf.Deg2Rad;
                Center(content,
                    new Vector2(-Mathf.Sin(middleRadians), Mathf.Cos(middleRadians)) * 112f,
                    new Vector2(142f, 58f));
                content.localRotation = Quaternion.identity;
                content.localScale = Vector3.one;

                Image rewardIcon = FindComponent<Image>(content, "Reward Icon");
                if (rewardIcon == null)
                {
                    rewardIcon = CreateImage(content, "Reward Icon", Color.white)
                        .GetComponent<Image>();
                }
                Sprite icon = reward != null ? reward.Icon : null;
                rewardIcon.sprite = icon;
                rewardIcon.color = Color.white;
                rewardIcon.preserveAspect = true;
                rewardIcon.raycastTarget = false;
                rewardIcon.enabled = icon != null;
                SetRect(rewardIcon.rectTransform, new Vector2(-50f, 0f),
                    new Vector2(34f, 34f));

                SetRect(label.rectTransform, icon != null
                        ? new Vector2(22f, 0f)
                        : Vector2.zero,
                    icon != null ? new Vector2(96f, 56f) : new Vector2(138f, 56f));
                label.rectTransform.localRotation = Quaternion.identity;
                label.rectTransform.localScale = Vector3.one;
                label.fontSize = 15f;
                label.enableAutoSizing = true;
                label.fontSizeMin = 10f;
                label.fontSizeMax = 15f;
                label.text = reward != null
                    ? $"{reward.GetDisplayName()}\n{shopData.GetWheelRewardDisplayPercent(reward):0.##}%"
                    : "?";
            }
            labelsRoot.SetAsLastSibling();

            for (int index = segmentCount; index < 100; index++)
            {
                Transform extra = wheelRoot.Find($"Wheel Segment {index + 1}");
                if (extra == null) break;
                extra.gameObject.SetActive(false);
            }
            for (int index = segmentCount; index < 100; index++)
            {
                Transform extra = labelsRoot.Find($"Reward Content {index + 1}");
                if (extra == null) break;
                extra.gameObject.SetActive(false);
            }

            RectTransform hub = FindComponent<RectTransform>(wheelRoot, "Wheel Hub");
            if (hub == null)
            {
                hub = CreateImage(wheelRoot, "Wheel Hub",
                    new Color(0.31f, 0.85f, 1f, 1f)).GetComponent<RectTransform>();
            }
            Center(hub, Vector2.zero, new Vector2(82f, 82f));
            hub.SetAsLastSibling();

            RectTransform pointer = FindComponent<RectTransform>(card, "Wheel Pointer");
            if (pointer == null)
            {
                pointer = CreateImage(card, "Wheel Pointer",
                    new Color(1f, 0.80f, 0.16f, 1f)).GetComponent<RectTransform>();
            }
            Center(pointer, new Vector2(-285f, 190f), new Vector2(42f, 58f));
            pointer.localRotation = Quaternion.Euler(0f, 0f, 45f);
            pointer.SetAsLastSibling();

            TextMeshProUGUI title = FindComponent<TextMeshProUGUI>(card, "Wheel Title");
            if (title == null)
            {
                title = CreateLabel(card, "Wheel Title", "LUCKY WHEEL",
                    new Vector2(-285f, 265f), new Vector2(370f, 52f), 31f,
                    new Color(0.31f, 0.85f, 1f), FontStyles.Bold);
            }
            title.rectTransform.anchoredPosition = new Vector2(-285f, 265f);

            EnsureSpinButton(card, "Spin Once Button", "Spin Once Label", "SPIN 1\n10 GEM",
                new Vector2(-390f, -235f), new Vector2(190f, 76f), uiData);
            EnsureSpinButton(card, "Spin Ten Button", "Spin Ten Label", "SPIN 10\n100 GEM",
                new Vector2(-180f, -235f), new Vector2(190f, 76f), uiData);

            RectTransform results = FindComponent<RectTransform>(card, "Wheel Result Panel");
            if (results == null)
            {
                results = CreateImage(card, "Wheel Result Panel",
                    new Color(0.07f, 0.11f, 0.19f, 1f)).GetComponent<RectTransform>();
                Undo.AddComponent<CanvasGroup>(results.gameObject);
            }
            if (results.GetComponent<CanvasGroup>() == null)
            {
                Undo.AddComponent<CanvasGroup>(results.gameObject);
            }
            Center(results, new Vector2(285f, -135f), new Vector2(500f, 245f));
            TextMeshProUGUI resultsLabel =
                FindComponent<TextMeshProUGUI>(results, "Wheel Results Label");
            if (resultsLabel == null)
            {
                resultsLabel = CreateLabel(results, "Wheel Results Label",
                    "Your rewards appear here.", Vector2.zero, new Vector2(430f, 230f),
                    19f, Color.white, FontStyles.Bold);
            }
            if (resultsLabel != null)
            {
                SetRect(resultsLabel.rectTransform, Vector2.zero, new Vector2(440f, 210f));
                resultsLabel.alignment = TextAlignmentOptions.Center;
                resultsLabel.enableAutoSizing = true;
                resultsLabel.fontSizeMin = 14f;
                resultsLabel.fontSizeMax = 19f;
            }
        }

        private static void EnsureSpinButton(Transform parent, string name, string labelName,
            string text, Vector2 position, Vector2 size, MiningUiData uiData)
        {
            Button button = FindComponent<Button>(parent, name);
            if (button == null)
            {
                button = CreateButton(parent, name, labelName, text, position, size,
                    new Color(0.10f, 0.70f, 0.52f, 1f), Color.white, 22f, uiData);
            }
            SetRect(button.transform as RectTransform, position, size);
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect != null)
            {
                Center(rect, position, size);
            }
        }

        private static Sprite LoadOrCreateWheelSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(WheelSpritePath);
            if (existing != null)
            {
                return existing;
            }
            if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            {
                AssetDatabase.CreateFolder("Assets", "Generated");
            }
            if (!AssetDatabase.IsValidFolder(GeneratedUiFolder))
            {
                AssetDatabase.CreateFolder("Assets/Generated", "MiningUI");
            }

            const int size = 128;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = center - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y),
                        new Vector2(center, center));
                    byte alpha = distance <= radius ? (byte)255 : (byte)0;
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(WheelSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(WheelSpritePath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(WheelSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(WheelSpritePath);
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
            if (root == null) return null;
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

        private static void SetReference(SerializedProperty serialized, string name, Object value)
        {
            SerializedProperty property = serialized?.FindPropertyRelative(name);
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

    [CustomEditor(typeof(MiningShopData))]
    public sealed class MiningShopDataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (DrawDefaultInspector())
            {
                MiningShopSetupMenu.QueueOpenPanelSync();
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Sync Shop UI In Open Scene"))
            {
                MiningShopSetupMenu.QueueOpenPanelSync();
            }
            EditorGUILayout.HelpBox(
                "Add products under Shop Products. Each entry accepts any MiningItemData, " +
                "quantity and Gem price. The open Shop Panel syncs automatically.",
                MessageType.Info);
        }
    }
}
#endif
