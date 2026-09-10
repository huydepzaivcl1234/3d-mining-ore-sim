using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Presents one configured gift-box roll and grants its selected reward.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningGiftBoxWheelPanel : MonoBehaviour
    {
        private readonly List<GameObject> rewardCards = new();

        private MiningItemSystem itemSystem;
        private PlayerWallet wallet;
        private MiningUiPanelCoordinator panelCoordinator;
        private MiningUiData uiData;
        private RectTransform returnPanel;
        private RectTransform wheel;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI statusLabel;
        private TextMeshProUGUI spinLabel;
        private Button spinButton;
        private Button closeButton;
        private MiningItemData giftBox;
        private MiningGiftReward selectedReward;
        private int sourceSlotIndex = -1;
        private bool spinning;
        private Tween spinTween;

        public void Configure(MiningItemSystem system, PlayerWallet playerWallet,
            MiningUiPanelCoordinator coordinator, MiningUiData data, RectTransform inventoryPanel)
        {
            itemSystem = system;
            wallet = playerWallet;
            panelCoordinator = coordinator;
            uiData = data;
            returnPanel = inventoryPanel;
            EnsureUi();
        }

        public void Show(int slotIndex, MiningItemData item)
        {
            if (item == null || item.UseType != MiningItemUseType.GiftBox)
            {
                return;
            }

            EnsureUi();
            giftBox = item;
            selectedReward = null;
            sourceSlotIndex = slotIndex;
            spinning = false;
            wheel.localRotation = Quaternion.identity;
            spinButton.interactable = true;
            closeButton.interactable = true;
            RebuildRewardCards();
            RefreshLabels();

            RectTransform panel = (RectTransform)transform;
            if (panelCoordinator != null)
            {
                panelCoordinator.OpenPanel(panel);
            }
            else
            {
                gameObject.SetActive(true);
                returnPanel?.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            MiningLocalization.LanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            if (spinTween.isAlive)
            {
                spinTween.Stop();
            }
            if (spinning && giftBox != null && itemSystem != null)
            {
                // A modal interruption or Scene shutdown must never silently eat the box.
                itemSystem.TryAddItem(giftBox);
            }
            spinning = false;
        }

        private void StartSpin()
        {
            if (spinning || giftBox == null || itemSystem == null || wallet == null ||
                !giftBox.TryRollGiftReward(out int rewardIndex, out MiningGiftReward reward))
            {
                statusLabel.text = MiningLocalization.Text(
                    "This gift box has no valid rewards.",
                    "Hộp quà chưa có phần thưởng hợp lệ.");
                return;
            }

            if (reward.RewardType == MiningGiftRewardType.Item &&
                !CanFitItemRewardAfterConsumption(reward))
            {
                statusLabel.text = MiningLocalization.Text(
                    "Inventory is full. Free a slot before spinning.",
                    "Túi đồ đã đầy. Hãy dọn một ô trước khi quay.");
                return;
            }
            if (!itemSystem.TryConsumeGiftBox(sourceSlotIndex, giftBox))
            {
                statusLabel.text = MiningLocalization.Text(
                    "The gift box is no longer in this slot.",
                    "Hộp quà không còn trong ô này.");
                return;
            }

            selectedReward = reward;
            spinning = true;
            spinButton.interactable = false;
            closeButton.interactable = false;
            statusLabel.text = MiningLocalization.Text("Spinning...", "Đang quay...");

            int rewardCount = Mathf.Max(1, giftBox.GiftRewards.Count);
            float step = 360f / rewardCount;
            float startAngle = wheel.localEulerAngles.z;
            float selectedAngle = Mathf.Repeat(-rewardIndex * step, 360f);
            float alignment = Mathf.Repeat(selectedAngle - Mathf.Repeat(startAngle, 360f), 360f);
            float endAngle = startAngle + giftBox.GiftSpinRotations * 360f + alignment;
            spinTween = Tween.Custom(this, startAngle, endAngle,
                    giftBox.GiftSpinDurationSeconds,
                    static (panel, angle) => panel.wheel.localRotation =
                        Quaternion.Euler(0f, 0f, angle),
                    Ease.OutQuart, useUnscaledTime: true)
                .OnComplete(this, static panel => panel.CompleteSpin());
        }

        private bool CanFitItemRewardAfterConsumption(MiningGiftReward reward)
        {
            if (itemSystem.CanAddItem(reward.Item, reward.ItemAmount))
            {
                return true;
            }

            MiningItemSystem.InventorySlotView source = itemSystem.GetSlot(sourceSlotIndex);
            return source.Item == giftBox && source.Count == 1 &&
                   reward.ItemAmount <= reward.Item.MaximumStack;
        }

        private void CompleteSpin()
        {
            spinning = false;
            closeButton.interactable = true;
            if (selectedReward == null)
            {
                return;
            }

            bool granted;
            if (selectedReward.RewardType == MiningGiftRewardType.Money)
            {
                wallet.AddMoney(selectedReward.MoneyAmount);
                granted = true;
            }
            else
            {
                granted = itemSystem.TryAddItem(selectedReward.Item, selectedReward.ItemAmount);
            }

            if (granted)
            {
                statusLabel.text = MiningLocalization.Text(
                    $"YOU WON: {selectedReward.GetDisplayName()}",
                    $"BẠN NHẬN ĐƯỢC: {selectedReward.GetDisplayName()}");
                spinLabel.text = MiningLocalization.Text("REWARD RECEIVED", "ĐÃ NHẬN THƯỞNG");
            }
            else
            {
                itemSystem.TryAddItem(giftBox);
                statusLabel.text = MiningLocalization.Text(
                    "Inventory changed during the spin. The gift box was returned.",
                    "Túi đồ đã thay đổi khi quay. Hộp quà đã được hoàn lại.");
                spinLabel.text = MiningLocalization.Text("SPIN FAILED", "QUAY THẤT BẠI");
            }
        }

        private void ReturnToInventory()
        {
            if (spinning)
            {
                return;
            }

            if (panelCoordinator != null && returnPanel != null)
            {
                panelCoordinator.OpenPanel(returnPanel);
            }
            else
            {
                gameObject.SetActive(false);
                returnPanel?.gameObject.SetActive(true);
            }
        }

        private void HandleLanguageChanged()
        {
            if (!spinning)
            {
                RebuildRewardCards();
                RefreshLabels();
            }
        }

        private void RefreshLabels()
        {
            titleLabel.text = giftBox != null
                ? MiningLocalization.Text("RARE GIFT BOX", "HỘP QUÀ HIẾM")
                : string.Empty;
            if (selectedReward == null)
            {
                statusLabel.text = MiningLocalization.Text(
                    "The pointer decides your reward. Press SPIN.",
                    "Kim chỉ phần thưởng của bạn. Bấm QUAY.");
                spinLabel.text = MiningLocalization.Text("SPIN", "QUAY");
            }
        }

        private void RebuildRewardCards()
        {
            foreach (GameObject card in rewardCards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
            rewardCards.Clear();
            if (giftBox == null || wheel == null)
            {
                return;
            }

            int count = giftBox.GiftRewards.Count;
            float radius = uiData != null ? uiData.GiftWheelRewardRadius : 142f;
            Vector2 cardSize = uiData != null ? uiData.GiftWheelRewardSize : new Vector2(150f, 58f);
            for (int index = 0; index < count; index++)
            {
                MiningGiftReward reward = giftBox.GiftRewards[index];
                if (reward == null || !reward.IsValid)
                {
                    continue;
                }

                float angle = index * 360f / Mathf.Max(1, count);
                float radians = angle * Mathf.Deg2Rad;
                GameObject card = CreateImage(wheel, $"Reward {index + 1:00}", reward.WheelColor);
                RectTransform rect = (RectTransform)card.transform;
                SetCenteredRect(rect,
                    new Vector2(Mathf.Sin(radians) * radius, Mathf.Cos(radians) * radius), cardSize);
                TextMeshProUGUI label = CreateLabel(rect, "Label", 17f, Color.white);
                label.text = $"{reward.GetDisplayName()}\n{giftBox.GetGiftRewardDisplayPercent(reward):0.##}%";
                rewardCards.Add(card);
            }
        }

        private void EnsureUi()
        {
            if (wheel != null)
            {
                return;
            }

            RectTransform panel = (RectTransform)transform;
            SetCenteredRect(panel, Vector2.zero,
                uiData != null ? uiData.GiftWheelPanelSize : new Vector2(720f, 620f));
            Image panelImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            panelImage.color = uiData != null ? uiData.GiftWheelPanelColor :
                new Color(0.06f, 0.075f, 0.11f, 0.98f);

            GameObject header = CreateImage(panel, "Header",
                uiData != null ? uiData.GiftWheelHeaderColor : new Color(0.48f, 0.24f, 0.75f));
            RectTransform headerRect = (RectTransform)header.transform;
            Vector2 headerSize = uiData != null ? uiData.GiftWheelHeaderSize : new Vector2(720f, 72f);
            SetTopRect(headerRect, Vector2.zero, headerSize);
            titleLabel = CreateLabel(headerRect, "Title", 30f, Color.white);

            closeButton = CreateButton(panel, "Close", "X", new Vector2(324f, 274f),
                new Vector2(50f, 50f), new Color(0.96f, 0.08f, 0.34f));
            closeButton.onClick.AddListener(ReturnToInventory);

            GameObject wheelObject = CreateImage(panel, "Wheel", new Color(0.11f, 0.14f, 0.20f, 1f));
            wheel = (RectTransform)wheelObject.transform;
            SetCenteredRect(wheel, new Vector2(0f, 32f),
                uiData != null ? uiData.GiftWheelSize : new Vector2(360f, 360f));
            TextMeshProUGUI center = CreateLabel(wheel, "Center", 22f, new Color(1f, 0.78f, 0.16f));
            center.text = "RARE";
            center.rectTransform.sizeDelta = new Vector2(100f, 44f);

            TextMeshProUGUI pointer = CreateLabel(panel, "Pointer", 42f,
                new Color(1f, 0.82f, 0.12f));
            pointer.text = "▼";
            pointer.rectTransform.anchoredPosition = new Vector2(0f, 236f);
            pointer.rectTransform.sizeDelta = new Vector2(60f, 50f);

            statusLabel = CreateLabel(panel, "Status", 21f, Color.white);
            statusLabel.rectTransform.anchoredPosition = new Vector2(0f, -201f);
            statusLabel.rectTransform.sizeDelta = new Vector2(650f, 50f);

            Vector2 spinSize = uiData != null ? uiData.GiftWheelSpinButtonSize : new Vector2(260f, 56f);
            spinButton = CreateButton(panel, "Spin", string.Empty, new Vector2(0f, -265f), spinSize,
                uiData != null ? uiData.GiftWheelSpinButtonColor : new Color(1f, 0.62f, 0.08f));
            spinLabel = spinButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            spinButton.onClick.AddListener(StartSpin);
        }

        private static GameObject CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject child = new(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            child.transform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.color = color;
            Outline outline = child.AddComponent<Outline>();
            outline.effectColor = new Color(0.025f, 0.055f, 0.09f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);
            return child;
        }

        private static Button CreateButton(Transform parent, string objectName, string text,
            Vector2 position, Vector2 size, Color color)
        {
            GameObject buttonObject = CreateImage(parent, objectName, color);
            RectTransform rect = (RectTransform)buttonObject.transform;
            SetCenteredRect(rect, position, size);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            TextMeshProUGUI label = CreateLabel(rect, "Label", 21f, Color.white);
            label.text = text;
            return button;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string objectName,
            float fontSize, Color color)
        {
            GameObject labelObject = new(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            SetCenteredRect(label.rectTransform, Vector2.zero, Vector2.zero);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
