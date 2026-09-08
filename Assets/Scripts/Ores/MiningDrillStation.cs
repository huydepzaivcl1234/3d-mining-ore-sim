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

        private MaterialPropertyBlock materialProperties;
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
            materialProperties = new MaterialPropertyBlock();
            ResolveSystems();
            EnsureVisibleModel();
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

        private void EnsureVisibleModel()
        {
            if (modelRoot == null)
            {
                GameObject mount = new("Generated Drill Model");
                mount.transform.SetParent(transform, false);
                modelRoot = mount.transform;
            }

            Renderer[] modelRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            if (modelRenderers.Length == 0)
            {
                CreateFallbackVisual();
                modelRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
                Debug.LogWarning(
                    "MiningDrillStation had no model Renderer. A non-destructive low-poly fallback was created at runtime. " +
                    "Add an imported Blender/FBX model under Blender Model Mount to replace it automatically.",
                    this);
            }

            if (tintRenderers == null || tintRenderers.Length == 0)
            {
                tintRenderers = modelRenderers;
            }
        }

        private void CreateFallbackVisual()
        {
            CreatePrimitivePart(PrimitiveType.Cube, "Base", modelRoot,
                new Vector3(0f, 0.2f, 0f), Vector3.zero, new Vector3(2.6f, 0.4f, 2f));
            CreatePrimitivePart(PrimitiveType.Cube, "Column", modelRoot,
                new Vector3(-0.75f, 1.55f, 0f), Vector3.zero, new Vector3(0.45f, 2.7f, 0.55f));
            CreatePrimitivePart(PrimitiveType.Cube, "Top Arm", modelRoot,
                new Vector3(0f, 2.75f, 0f), Vector3.zero, new Vector3(2f, 0.45f, 0.6f));
            CreatePrimitivePart(PrimitiveType.Cube, "Control Housing", modelRoot,
                new Vector3(0.7f, 1.1f, 0f), Vector3.zero, new Vector3(0.7f, 0.8f, 0.75f));

            GameObject headPivot = new("Generated Drill Head");
            headPivot.transform.SetParent(modelRoot, false);
            headPivot.transform.localPosition = new Vector3(0.65f, 2.25f, 0f);
            drillHead = headPivot.transform;

            CreatePrimitivePart(PrimitiveType.Cylinder, "Chuck", drillHead,
                Vector3.zero, Vector3.zero, new Vector3(0.4f, 0.35f, 0.4f));
            CreatePrimitivePart(PrimitiveType.Cylinder, "Drill Shaft", drillHead,
                new Vector3(0f, -0.62f, 0f), Vector3.zero, new Vector3(0.16f, 0.9f, 0.16f));
            CreatePrimitivePart(PrimitiveType.Cube, "Rotation Marker", drillHead,
                new Vector3(0.22f, -0.85f, 0f), new Vector3(0f, 0f, 35f),
                new Vector3(0.35f, 0.12f, 0.12f));
        }

        private static Transform CreatePrimitivePart(PrimitiveType type, string partName,
            Transform parent, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localEulerAngles = localEulerAngles;
            part.transform.localScale = localScale;

            Collider generatedCollider = part.GetComponent<Collider>();
            if (generatedCollider != null)
            {
                generatedCollider.enabled = false;
                Destroy(generatedCollider);
            }

            return part.transform;
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
