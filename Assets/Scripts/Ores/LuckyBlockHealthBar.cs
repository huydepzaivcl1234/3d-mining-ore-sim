using Microlight.MicroBar;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Connects a Lucky Block's durability to the existing world-space MicroBar style.</summary>
    [DisallowMultipleComponent]
    public sealed class LuckyBlockHealthBar : MonoBehaviour
    {
        [SerializeField] private LuckyBlock luckyBlock;
        [SerializeField] private MicroBar healthBar;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Camera targetCamera;

        private int initializedMaximum;
        private bool initialized;

        public void Configure(LuckyBlock targetBlock, MicroBar targetBar, Transform targetVisualRoot)
        {
            luckyBlock = targetBlock;
            healthBar = targetBar;
            visualRoot = targetVisualRoot;
        }

        private void Awake()
        {
            luckyBlock ??= GetComponentInParent<LuckyBlock>();
            healthBar ??= GetComponent<MicroBar>();
            visualRoot ??= transform;
            targetCamera ??= Camera.main;
        }

        private void OnEnable()
        {
            if (luckyBlock == null)
            {
                return;
            }

            luckyBlock.DurabilityChanged -= HandleDurabilityChanged;
            luckyBlock.DurabilityChanged += HandleDurabilityChanged;
            RefreshFromBlock();
        }

        private void LateUpdate()
        {
            LuckyBlockData settings = luckyBlock != null ? luckyBlock.Settings : null;
            if (settings == null || visualRoot == null)
            {
                return;
            }

            visualRoot.position = luckyBlock.GetWorldTopCenter() + settings.HealthBarWorldOffset;
            SetWorldScale(settings.HealthBarScale);
            targetCamera ??= Camera.main;
            if (targetCamera != null)
            {
                visualRoot.rotation = Quaternion.LookRotation(
                    visualRoot.position - targetCamera.transform.position,
                    targetCamera.transform.up);
            }
        }

        private void OnDisable()
        {
            if (luckyBlock != null)
            {
                luckyBlock.DurabilityChanged -= HandleDurabilityChanged;
            }
        }

        private void RefreshFromBlock()
        {
            if (luckyBlock == null || luckyBlock.MaximumDurability <= 0)
            {
                return;
            }

            HandleDurabilityChanged(luckyBlock.CurrentDurability, luckyBlock.MaximumDurability);
        }

        private void HandleDurabilityChanged(int current, int maximum)
        {
            if (healthBar == null || maximum <= 0)
            {
                return;
            }

            if (!initialized)
            {
                initializedMaximum = Mathf.Max(1, maximum);
                healthBar.Initialize(initializedMaximum);
                initialized = true;
            }
            else if (initializedMaximum != maximum)
            {
                initializedMaximum = Mathf.Max(1, maximum);
                healthBar.SetNewMaxHP(initializedMaximum, true);
            }

            healthBar.UpdateBar(Mathf.Clamp(current, 0, initializedMaximum), true);
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
    }
}
