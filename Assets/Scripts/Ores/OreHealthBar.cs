using Microlight.MicroBar;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Connects an ore's durability to a world-space MicroBar.</summary>
    [DisallowMultipleComponent]
    public sealed class OreHealthBar : MonoBehaviour
    {
        [SerializeField] private Ore ore;
        [SerializeField] private MicroBar healthBar;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Camera targetCamera;

        private bool initialized;
        private int initializedMaxHealth;

        public void Configure(Ore targetOre, MicroBar targetBar, Transform targetVisualRoot)
        {
            ore = targetOre;
            healthBar = targetBar;
            visualRoot = targetVisualRoot;
        }

        private void Awake()
        {
            ore ??= GetComponentInParent<Ore>();
            healthBar ??= GetComponent<MicroBar>();
            visualRoot ??= transform;
            targetCamera ??= Camera.main;
        }

        private void OnEnable()
        {
            if (ore != null)
            {
                ore.DurabilityChanged -= HandleDurabilityChanged;
                ore.DurabilityChanged += HandleDurabilityChanged;
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

            if (targetCamera != null && visualRoot != null)
            {
                visualRoot.rotation = Quaternion.LookRotation(
                    visualRoot.position - targetCamera.transform.position,
                    targetCamera.transform.up);
            }
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

        private void InitializeBarIfNeeded()
        {
            if (initialized || ore == null || ore.Data == null || healthBar == null)
            {
                return;
            }

            initializedMaxHealth = Mathf.Max(1, ore.MaxDurability);
            healthBar.Initialize(initializedMaxHealth);
            healthBar.UpdateBar(ore.CurrentDurability, true);
            initialized = true;
        }
    }
}
