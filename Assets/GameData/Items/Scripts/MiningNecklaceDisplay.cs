using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>One editable pedestal offer. Inventory owns purchase, equipment and persistence.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningNecklaceDisplay : MonoBehaviour
    {
        [SerializeField] private MiningItemData item;
        [SerializeField] private Transform floatingModel;
        [SerializeField] private Canvas panel;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private UnityEngine.UI.Button buyButton;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text bonusLabel;
        [SerializeField] private TMP_Text buyLabel;
        [SerializeField] private MiningItemSystem items;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningCharacterHealth player;
        [SerializeField] private Camera viewCamera;
        [Header("World-space presentation")]
        [Min(0f), SerializeField] private float rotationDegreesPerSecond = 35f;
        [Min(0f), SerializeField] private float bobHeight = .04f;
        [Min(0f), SerializeField] private float bobCyclesPerSecond = .5f;
        [Min(0f), SerializeField] private float interactionDistance = 4f;
        [Min(.01f), SerializeField] private float fadeSeconds = .2f;
        private Vector3 modelRestPosition;
        private Quaternion modelRestRotation;
        private float phase;
        private bool inRange;
        private MiningAudioManager audioManager;

        private void Awake()
        {
            if (items == null) items = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            if (wallet == null) wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            var stats = FindFirstObjectByType<MiningPlayerStats>(FindObjectsInactive.Include);
            if (player == null && stats != null) player = stats.GetComponent<MiningCharacterHealth>();
            if (viewCamera == null) viewCamera = Camera.main;
            audioManager = FindFirstObjectByType<MiningAudioManager>(FindObjectsInactive.Include);
            if (floatingModel != null)
            {
                modelRestPosition = floatingModel.localPosition;
                modelRestRotation = floatingModel.localRotation;
            }
            if (panel != null) panel.worldCamera = viewCamera;
            if (panelGroup != null) { panelGroup.alpha = 0f; panelGroup.blocksRaycasts = false; }
        }

        private void OnEnable()
        {
            buyButton?.onClick.AddListener(Buy);
            if (items != null) items.InventoryChanged += Refresh;
            if (wallet != null) wallet.GemsChanged += HandleGems;
            MiningLocalization.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            buyButton?.onClick.RemoveListener(Buy);
            if (items != null) items.InventoryChanged -= Refresh;
            if (wallet != null) wallet.GemsChanged -= HandleGems;
            MiningLocalization.LanguageChanged -= Refresh;
            if (floatingModel != null)
            {
                floatingModel.localPosition = modelRestPosition;
                floatingModel.localRotation = modelRestRotation;
            }
        }

        private void LateUpdate()
        {
            phase += Time.deltaTime;
            if (floatingModel != null)
            {
                floatingModel.localPosition = modelRestPosition + Vector3.up * (Mathf.Sin(phase * bobCyclesPerSecond * Mathf.PI * 2f) * bobHeight);
                floatingModel.localRotation = Quaternion.AngleAxis(phase * rotationDegreesPerSecond, Vector3.up) * modelRestRotation;
            }
            if (panel != null && viewCamera != null) panel.transform.rotation = viewCamera.transform.rotation;
            bool nearby = player != null && player.Health > 0f &&
                (player.transform.position - transform.position).sqrMagnitude <= interactionDistance * interactionDistance;
            if (inRange != nearby) { inRange = nearby; Refresh(); }
            if (panelGroup == null) return;
            panelGroup.alpha = Mathf.MoveTowards(panelGroup.alpha, inRange ? 1f : 0f, Time.unscaledDeltaTime / fadeSeconds);
            panelGroup.interactable = panelGroup.blocksRaycasts = inRange && panelGroup.alpha > .95f;
        }

        private void HandleGems(float amount) => Refresh();
        private void Refresh()
        {
            if (item == null) return;
            if (titleLabel != null) titleLabel.text = item.DisplayName;
            if (bonusLabel != null) bonusLabel.text = item.GetEffectSummary();
            bool owned = items != null && items.OwnsEquipment(item);
            if (buyLabel != null) buyLabel.text = owned ? MiningLocalization.Text("Owned", "Đã sở hữu") :
                string.Format(MiningLocalization.Text("Buy • {0} gems", "Mua • {0} gem"), MiningMoneyFormatter.Format(item.EquipmentGemPrice));
            if (buyButton != null) buyButton.interactable = inRange && !owned && items != null && wallet != null &&
                wallet.CurrentGems >= item.EquipmentGemPrice && items.CanAddItem(item);
        }

        public void Buy()
        {
            // Revalidate distance and life on click: a fading panel is not purchase authority.
            if (player == null || player.Health <= 0f ||
                (player.transform.position - transform.position).sqrMagnitude > interactionDistance * interactionDistance) return;
            if (items != null && items.TryBuyEquipment(item, wallet)) audioManager?.PlayButtonSfx();
            Refresh();
        }
    }
}
