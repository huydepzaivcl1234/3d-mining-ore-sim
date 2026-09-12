using Microlight.MicroBar;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Connects an ore's durability to a world-space MicroBar.</summary>
    [DisallowMultipleComponent]
    public sealed class OreHealthBar : MonoBehaviour
    {
        [SerializeField] private Ore ore;
        [SerializeField] private MicroBar healthBar;
        [SerializeField] private TextMeshPro healthText;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Camera targetCamera;

        private bool initialized;
        private int initializedMaxHealth;
        private Collider[] oreColliders;

        public void Configure(Ore targetOre, MicroBar targetBar, Transform targetVisualRoot)
        {
            ore = targetOre;
            healthBar = targetBar;
            visualRoot = targetVisualRoot;
            CacheOreColliders();
        }

        private void Awake()
        {
            ore ??= GetComponentInParent<Ore>();
            healthBar ??= GetComponent<MicroBar>();
            healthText ??= GetComponentInChildren<TextMeshPro>(true);
            visualRoot ??= transform;
            targetCamera ??= Camera.main;
            CacheOreColliders();
        }

        private void OnEnable()
        {
            if (ore != null)
            {
                ore.DurabilityChanged -= HandleDurabilityChanged;
                ore.DurabilityChanged += HandleDurabilityChanged;
                InitializeBarIfNeeded();
                if (initialized)
                {
                    HandleDurabilityChanged(ore.CurrentDurability, ore.MaxDurability);
                }
            }
        }

        private void Start()
        {
            InitializeBarIfNeeded();
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (visualRoot == null || ore == null || ore.Data == null)
            {
                return;
            }

            visualRoot.position = GetWorldAnchor() + ore.Data.HealthBarWorldOffset;
            SetWorldScale(ore.Data.HealthBarScale);

            if (targetCamera != null)
            {
                visualRoot.rotation = Quaternion.LookRotation(
                    visualRoot.position - targetCamera.transform.position,
                    targetCamera.transform.up);
            }
        }

        private void SetWorldScale(float uniformScale)
        {
            Transform parent = visualRoot.parent;
            if (parent == null)
            {
                visualRoot.localScale = Vector3.one * uniformScale;
                return;
            }

            Vector3 parentScale = parent.lossyScale;
            visualRoot.localScale = new Vector3(
                SafeDivide(uniformScale, parentScale.x),
                SafeDivide(uniformScale, parentScale.y),
                SafeDivide(uniformScale, parentScale.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > Mathf.Epsilon ? value / divisor : value;
        }

        private void CacheOreColliders()
        {
            oreColliders = ore != null ? ore.GetComponentsInChildren<Collider>(true) : null;
        }

        private Vector3 GetWorldAnchor()
        {
            if (oreColliders == null || oreColliders.Length == 0)
            {
                CacheOreColliders();
            }

            bool hasBounds = false;
            Bounds combinedBounds = default;
            if (oreColliders != null)
            {
                foreach (Collider candidate in oreColliders)
                {
                    if (candidate == null || !candidate.enabled || candidate.isTrigger ||
                        (visualRoot != null && candidate.transform.IsChildOf(visualRoot)))
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        combinedBounds = candidate.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(candidate.bounds);
                    }
                }
            }

            return hasBounds
                ? new Vector3(combinedBounds.center.x, combinedBounds.max.y, combinedBounds.center.z)
                : ore.transform.position;
        }

        private void OnDisable()
        {
            if (ore != null)
            {
                ore.DurabilityChanged -= HandleDurabilityChanged;
            }
        }

        private void HandleDurabilityChanged(int current, int maximum)
        {
            RefreshHealthText(current, maximum);
            InitializeBarIfNeeded();
            if (!initialized)
            {
                return;
            }

            if (maximum != initializedMaxHealth)
            {
                initializedMaxHealth = Mathf.Max(1, maximum);
                healthBar.SetNewMaxHP(initializedMaxHealth, true);
            }

            healthBar.UpdateBar(current);
        }

        private void RefreshHealthText(int current, int maximum)
        {
            if (healthText == null)
            {
                return;
            }

            int safeMaximum = Mathf.Max(0, maximum);
            int safeCurrent = Mathf.Clamp(current, 0, safeMaximum);
            healthText.text = $"{MiningMoneyFormatter.Format(safeCurrent)} / " +
                              MiningMoneyFormatter.Format(safeMaximum);
        }

        private void InitializeBarIfNeeded()
        {
            if (initialized || ore == null || ore.Data == null || healthBar == null)
            {
                return;
            }

            initializedMaxHealth = Mathf.Max(1, ore.MaxDurability);
            healthBar.Initialize(initializedMaxHealth);
            healthBar.UpdateBar(ore.CurrentDurability, true);
            RefreshHealthText(ore.CurrentDurability, ore.MaxDurability);
            initialized = true;
        }
    }
}
