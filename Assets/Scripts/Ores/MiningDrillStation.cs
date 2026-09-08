using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Fixed world station that can be purchased and upgraded through five levels.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningDrillStation : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Systems")]
        [Tooltip("Optional. If empty, the single MiningGameManager is resolved once in Awake.")]
        [SerializeField] private MiningGameManager gameManager;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private MiningDrillData drillData;
        [SerializeField] private MiningDrillPanel drillPanel;

        [Header("Blender Model References")]
        [Tooltip("Assign the imported Blender/FBX model root here.")]
        [SerializeField] private Transform modelRoot;
        [Tooltip("Assign the rotating drill-head transform from the Blender model here.")]
        [SerializeField] private Transform drillHead;
        [SerializeField] private Renderer[] tintRenderers = Array.Empty<Renderer>();
        [SerializeField] private Light statusLight;

        [Header("Runtime State")]
        [SerializeField, Range(0, MiningDrillData.LevelCount)] private int currentLevel;

        private readonly MaterialPropertyBlock materialProperties = new();
        private float productionTimer;
        private bool affordable;

        public MiningDrillData DrillData => drillData;
        public int CurrentLevel => currentLevel;
        public bool IsPurchased => currentLevel > 0;
        public bool IsMaximumLevel => currentLevel >= MiningDrillData.LevelCount;
        public MiningDrillLevel CurrentDefinition => IsPurchased && drillData != null
            ? drillData.GetLevel(currentLevel)
            : null;
        public MiningDrillLevel NextDefinition => !IsMaximumLevel && drillData != null
            ? drillData.GetLevel(Mathf.Max(1, currentLevel + 1))
            : null;
        public int NextCost => drillData == null
            ? int.MaxValue
            : IsPurchased
                ? (IsMaximumLevel ? 0 : drillData.GetLevel(currentLevel).UpgradeCost)
                : drillData.PurchaseCost;
        public bool CanPurchaseOrUpgrade => drillData != null && wallet != null &&
                                            !IsMaximumLevel && wallet.CurrentMoney >= NextCost;

        public event Action StateChanged;
        public event Action<int> MoneyProduced;

        private void Awake()
        {
            ResolveSystems();
            currentLevel = Mathf.Clamp(currentLevel, 0, MiningDrillData.LevelCount);
            drillPanel?.Bind(this);
            RefreshAffordability(true);
        }

        private void OnEnable()
        {
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.MoneyChanged += HandleMoneyChanged;
            }

            if (rebirthSystem != null)
            {
                rebirthSystem.RebirthCompleted -= HandleRebirth;
                rebirthSystem.RebirthCompleted += HandleRebirth;
            }

            RefreshAffordability(true);
        }

        private void OnDisable()
        {
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
            }

            if (rebirthSystem != null)
            {
                rebirthSystem.RebirthCompleted -= HandleRebirth;
            }
        }

        private void Update()
        {
            AnimateModel();
            AnimateStatusLight();

            MiningDrillLevel level = CurrentDefinition;
            if (level == null || wallet == null)
            {
                productionTimer = 0f;
                return;
            }

            productionTimer += Time.deltaTime;
            if (productionTimer < level.SecondsPerCycle)
            {
                return;
            }

            productionTimer %= level.SecondsPerCycle;
            int amount = drillData.ApplyMoneyRewardBoosts && upgradeSystem != null
                ? upgradeSystem.CalculateMiningReward(level.MoneyPerCycle)
                : level.MoneyPerCycle;
            wallet.AddMoney(amount);
            MoneyProduced?.Invoke(amount);
        }

        public void Interact()
        {
            drillPanel?.OpenFor(this);
        }

        public bool TryPurchaseOrUpgrade()
        {
            if (!CanPurchaseOrUpgrade || !wallet.TrySpend(NextCost))
            {
                return false;
            }

            currentLevel++;
            productionTimer = 0f;
            gameManager?.AudioManager?.PlayButtonSfx();
            RefreshAffordability(true);
            StateChanged?.Invoke();
            return true;
        }

        private void AnimateModel()
        {
            MiningDrillLevel level = CurrentDefinition;
            if (drillHead == null || level == null)
            {
                return;
            }

            drillHead.Rotate(Vector3.up,
                level.DrillRotationDegreesPerSecond * Time.deltaTime, Space.Self);
        }

        private void AnimateStatusLight()
        {
            if (statusLight == null || drillData == null)
            {
                return;
            }

            float baseIntensity = affordable && !IsPurchased
                ? drillData.AffordableLightIntensity
                : IsPurchased ? drillData.ActiveLightIntensity : 0f;
            float pulse = affordable && !IsPurchased
                ? 1f + Mathf.Sin(Time.unscaledTime * drillData.GlowPulseSpeed) *
                  drillData.GlowPulseAmount
                : 1f;
            statusLight.intensity = Mathf.Max(0f, baseIntensity * pulse);
        }

        private void HandleMoneyChanged(int money)
        {
            RefreshAffordability(false);
        }

        private void HandleRebirth(int count)
        {
            if (drillData == null || !drillData.ResetOnRebirth)
            {
                return;
            }

            currentLevel = 0;
            productionTimer = 0f;
            RefreshAffordability(true);
            StateChanged?.Invoke();
        }

        private void RefreshAffordability(bool forceVisualRefresh)
        {
            bool wasAffordable = affordable;
            affordable = CanPurchaseOrUpgrade;
            if (!forceVisualRefresh && wasAffordable == affordable)
            {
                return;
            }

            Color color = drillData == null
                ? Color.white
                : IsPurchased
                    ? drillData.ActiveColor
                    : affordable ? drillData.AffordableColor : drillData.LockedColor;

            foreach (Renderer targetRenderer in tintRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(materialProperties);
                materialProperties.SetColor(BaseColorId, color);
                materialProperties.SetColor(ColorId, color);
                targetRenderer.SetPropertyBlock(materialProperties);
            }

            if (statusLight != null && drillData != null)
            {
                statusLight.color = affordable && !IsPurchased
                    ? drillData.AffordableColor
                    : drillData.ActiveColor;
            }

            StateChanged?.Invoke();
        }

        private void OnValidate()
        {
            currentLevel = Mathf.Clamp(currentLevel, 0, MiningDrillData.LevelCount);
            if (modelRoot == null && drillHead != null)
            {
                modelRoot = drillHead.root;
            }
        }

        private void ResolveSystems()
        {
            gameManager ??= FindFirstObjectByType<MiningGameManager>();
            if (gameManager == null)
            {
                return;
            }

            wallet ??= gameManager.Wallet;
            upgradeSystem ??= gameManager.UpgradeSystem;
            rebirthSystem ??= gameManager.RebirthSystem;
            drillPanel?.SetPanelCoordinator(gameManager.PanelCoordinator);
        }
    }
}
