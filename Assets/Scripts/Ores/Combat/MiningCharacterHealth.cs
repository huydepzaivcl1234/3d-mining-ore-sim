using Microlight.MicroBar;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent]
    public sealed class MiningCharacterHealth : MonoBehaviour
    {
        [Min(1f), SerializeField] private float maxHealth = 100f;
        [Header("Regeneration")]
        [Tooltip("HP restored per tick. Set 0 to disable; dead characters never regenerate.")]
        [Min(0f), SerializeField] private float regenAmount = 5f;
        [Min(0.1f), SerializeField] private float regenInterval = 5f;
        private float regenTimer;
        public event System.Action Damaged;
        public event System.Action Died;
        [SerializeField] private Transform healthBar;
        [SerializeField] private MicroBar microBar;
        [SerializeField] private TMP_Text healthLabel;
        private Camera healthCamera;
        private float health;
        public float Health => health;
        public Transform HealthBar => healthBar;
        private MiningPlayerStatsData Stats => MiningPlayerStats.For(this);
        private float spawnedMaxHealth;
        public float MaxHealth => Mathf.Max(1f, Stats != null ? GetComponent<MiningPlayerStats>().MaxHealth : spawnedMaxHealth > 0 ? spawnedMaxHealth : maxHealth);
        public void ConfigureSpawnHealth(float value)
        {
            spawnedMaxHealth = Mathf.Max(1, value);
            initializedMaxHealth = MaxHealth;
            health = MaxHealth;
            if (microBar != null) microBar.Initialize(MaxHealth);
            Refresh(true);
        }
        public float RegenAmount => Mathf.Max(0f, Stats != null ? Stats.regenAmount : regenAmount);
        public float RegenInterval => Mathf.Max(0.1f, Stats != null ? Stats.regenInterval : regenInterval);
        private float initializedMaxHealth;
        public void Respawn()
        {
            health = MaxHealth;
            regenTimer = 0f;
            Refresh(true);
        }
        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || health <= 0f) return;
            health = Mathf.Max(0f, health - amount);
            Refresh(false, UpdateAnim.Damage);
            Damaged?.Invoke();
            if (health <= 0f) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || health <= 0f) return;
            health = Mathf.Min(MaxHealth, health + amount);
            Refresh(false, UpdateAnim.Heal);
        }

        private void OnEnable() => regenTimer = 0f;

        private void Update()
        {
            if (initializedMaxHealth != MaxHealth)
            {
                if (health > 0) health = Mathf.Clamp(health + MaxHealth - initializedMaxHealth, 0, MaxHealth);
                initializedMaxHealth = MaxHealth;
                if (microBar != null) microBar.Initialize(MaxHealth);
                Refresh(true);
            }
            if (health <= 0f || health >= MaxHealth || RegenAmount <= 0f)
            {
                regenTimer = 0f;
                return;
            }
            regenTimer += Time.deltaTime;
            float interval = RegenInterval;
            if (regenTimer < interval) return;
            int ticks = Mathf.FloorToInt(regenTimer / interval);
            regenTimer -= ticks * interval;
            Heal(RegenAmount * ticks);
        }

        private void Awake()
        {
            health = MaxHealth;
            initializedMaxHealth = MaxHealth;
            if (microBar == null && healthBar != null)
                microBar = healthBar.GetComponent<MicroBar>();
            if (healthLabel == null && healthBar != null) healthLabel = healthBar.GetComponentInChildren<TMP_Text>(true);
            if (microBar != null) microBar.Initialize(MaxHealth);
            Refresh(true);
        }

        private void LateUpdate()
        {
            if (healthBar == null) return;
            if (healthCamera == null) healthCamera = Camera.main;
            if (healthCamera != null)
                healthBar.rotation = Quaternion.LookRotation(
                    healthBar.position - healthCamera.transform.position,
                    healthCamera.transform.up);
        }

        private void Refresh(bool skipAnimation, UpdateAnim updateType = UpdateAnim.Damage)
        {
            // Only initialization/respawn snaps. Damage and healing use the
            // animation authored on the MicroBar (Flash, Fill, etc.).
            if (microBar != null) microBar.UpdateBar(health, skipAnimation, updateType);
            if (healthLabel != null)
            {
                var player = GetComponent<MiningPlayerStats>();
                var monster = GetComponent<MushroomMonster>();
                int level = player != null ? player.Level : monster != null ? monster.Level : 1;
                healthLabel.text = $"Lv. {level} | {Mathf.Ceil(health):0} / {Mathf.Ceil(MaxHealth):0}";
            }
        }

        private void OnValidate() => maxHealth = Mathf.Max(1f, maxHealth);
    }
}
