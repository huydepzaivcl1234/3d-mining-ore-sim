using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Creates a scale-safe money display and NPC purchase button.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningHud : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private NpcShop npcShop;

        private Text moneyText;
        private Text npcCountText;
        private Text statusText;
        private Button buyButton;
        private Font font;

        private void Awake()
        {
            BuildHud();
        }

        private void OnEnable()
        {
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.MoneyChanged += HandleMoneyChanged;
            }

            if (npcShop != null)
            {
                npcShop.NpcCountChanged -= HandleNpcCountChanged;
                npcShop.NpcCountChanged += HandleNpcCountChanged;
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(BuyNpc);
                buyButton.onClick.AddListener(BuyNpc);
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
            }

            if (npcShop != null)
            {
                npcShop.NpcCountChanged -= HandleNpcCountChanged;
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(BuyNpc);
            }
        }

        private void BuildHud()
        {
            if (transform.Find("Mining HUD Canvas") != null)
            {
                return;
            }

            EnsureEventSystem();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasObject = new("Mining HUD Canvas", typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = CreateUiObject("NPC Shop", canvasObject.transform, typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(330f, 190f);
            panel.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.06f, 0.94f);

            moneyText = CreateText(panel.transform, "Money", new Vector2(18f, -16f), 26);
            npcCountText = CreateText(panel.transform, "NPC Count", new Vector2(18f, -52f), 21);
            buyButton = CreateButton(panel.transform, new Vector2(18f, -88f));
            statusText = CreateText(panel.transform, "Status", new Vector2(18f, -151f), 17);
            statusText.color = new Color(1f, 0.82f, 0.28f);
            statusText.text = "Chuột phải: xoay • WASD: di chuyển";
        }

        private Text CreateText(Transform parent, string name, Vector2 position, int size)
        {
            GameObject textObject = CreateUiObject(name, parent, typeof(Text));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(294f, 32f);

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        private Button CreateButton(Transform parent, Vector2 position)
        {
            GameObject buttonObject = CreateUiObject("Buy Mining NPC", parent, typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(294f, 54f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.95f, 0.57f, 0.1f);
            Button button = buttonObject.GetComponent<Button>();
            Text label = CreateText(buttonObject.transform, "Label", Vector2.zero, 22);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.08f, 0.06f, 0.03f);
            return button;
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
        {
            GameObject result = new(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            foreach (System.Type component in components)
            {
                result.AddComponent(component);
            }
            return result;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            Object.DontDestroyOnLoad(eventSystem);
        }

        private void BuyNpc()
        {
            bool purchased = npcShop != null && npcShop.TryBuyNpc();
            statusText.text = purchased ? "Đã mua NPC đào quặng!" : "Không đủ tiền hoặc thiếu cấu hình NPC.";
            Refresh();
        }

        private void HandleMoneyChanged(int money)
        {
            Refresh();
        }

        private void HandleNpcCountChanged(int count)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (moneyText == null || npcCountText == null || buyButton == null)
            {
                return;
            }

            int money = wallet != null ? wallet.CurrentMoney : 0;
            int count = npcShop != null ? npcShop.PurchasedCount : 0;
            int cost = npcShop != null ? npcShop.NpcCost : 0;
            moneyText.text = $"Tiền: {money}";
            npcCountText.text = $"NPC đào quặng: {count}";
            buyButton.GetComponentInChildren<Text>().text = $"Mua NPC đào ({cost})";
            buyButton.interactable = npcShop != null && npcShop.CanBuy;
        }
    }
}
