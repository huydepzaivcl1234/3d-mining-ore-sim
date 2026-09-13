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
        private const string GeneratedUiFolder = "Assets/Generated/MiningUI";
        private const string WheelSpritePath = GeneratedUiFolder + "/LuckyWheelCircle.png";

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

            MiningMainMenuSetupMenu.EnsureShopButtonForCurrentMenu();
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            MiningCandyUiSetupMenu.ApplyCandyThemeFromSetup();
            MiningButtonSfxSetupMenu.AssignAllButtonSfx(false);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            EditorGUIUtility.PingObject(panel.gameObject);
            EditorUtility.DisplayDialog("Shop Setup",
                "Shop is ready with a configurable Lucky Wheel (10 Gems x1, 100 Gems x10). " +
                "Edit Wheel Rewards in MiningShopData, then save the scene.",
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
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return data;
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
            if (card.Find("Rare Gift Product") == null)
            {
                CreateProductCard(card, uiData, rareGift, shopData.RareGiftBoxGemCost);
            }
            LayoutProductCard(card);
            EnsureLuckyWheel(card, uiData, shopData);
            TextMeshProUGUI price = FindComponent<TextMeshProUGUI>(card, "Gem Price");
            if (price != null)
            {
                price.text = $"{MiningMoneyFormatter.Format(shopData.RareGiftBoxGemCost)} GEM";
            }

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
            return overlay;
        }

        private static void LayoutProductCard(RectTransform card)
        {
            RectTransform product = card.Find("Rare Gift Product") as RectTransform;
            if (product == null)
            {
                return;
            }
            Center(product, new Vector2(285f, 92f), new Vector2(500f, 235f));
            SetRect(FindComponent<RectTransform>(product, "Item Icon"),
                new Vector2(-170f, 20f), new Vector2(112f, 112f));
            SetRect(FindComponent<RectTransform>(product, "Item Icon Fallback"),
                Vector2.zero, new Vector2(105f, 105f));
            SetRect(FindComponent<RectTransform>(product, "Item Name"),
                new Vector2(65f, 72f), new Vector2(315f, 46f));
            SetRect(FindComponent<RectTransform>(product, "Item Description"),
                new Vector2(65f, 17f), new Vector2(315f, 64f));
            SetRect(FindComponent<RectTransform>(product, "Gem Price"),
                new Vector2(-70f, -80f), new Vector2(210f, 48f));
            SetRect(FindComponent<RectTransform>(product, "Buy Rare Gift Button"),
                new Vector2(145f, -80f), new Vector2(180f, 56f));
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

                TextMeshProUGUI label = FindComponent<TextMeshProUGUI>(segment, "Reward Label");
                if (label == null)
                {
                    label = CreateLabel(segment, "Reward Label", string.Empty,
                        Vector2.zero, new Vector2(130f, 54f), 17f, Color.white,
                        FontStyles.Bold);
                }
                float angle = (index + 0.5f) * Mathf.PI * 2f / segmentCount;
                label.rectTransform.anchoredPosition =
                    new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * 112f;
                label.rectTransform.localRotation =
                    Quaternion.Euler(0f, 0f, -index * 360f / segmentCount);
                label.text = reward != null
                    ? $"{reward.GetDisplayName()}\n{shopData.GetWheelRewardDisplayPercent(reward):0.##}%"
                    : "?";
            }

            for (int index = segmentCount; index < 100; index++)
            {
                Transform extra = wheelRoot.Find($"Wheel Segment {index + 1}");
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
