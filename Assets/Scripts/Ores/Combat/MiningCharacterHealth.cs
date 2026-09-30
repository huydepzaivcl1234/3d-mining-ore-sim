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
        private PlayerCombatInput combatInput;
        private bool displayedCombatMode;
        private float health;
        private float healingBonusPercent;
        private float burnDamagePerTick;
        private float burnTickSeconds;
        private float burnRemaining;
        private float burnTimer;
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
            ClearBurn();
            Refresh(true);
        }
        public void ApplyDamage(float amount)
        {
            DealDamage(amount);
        }

        // The actual health removed drives life steal; overkill never heals the attacker.
        public float DealDamage(float amount)
        {
            if (amount <= 0f || health <= 0f) return 0f;
            float dealt = Mathf.Min(health, amount);
            health = Mathf.Max(0f, health - amount);
            Refresh(false, UpdateAnim.Damage);
            Damaged?.Invoke();
            if (health <= 0f)
            {
                ClearBurn();
                Died?.Invoke();
            }
            return dealt;
        }

        public void ConfigureHealingBonus(float percent) => healingBonusPercent = Mathf.Max(0f, percent);

        // Repeated hits refresh the duration; only the strongest DPS remains active.
        public void ApplyBurn(float damagePerTick, float tickSeconds, float duration)
        {
            if (health <= 0f || damagePerTick <= 0f || tickSeconds <= 0f || duration <= 0f) return;
            tickSeconds = Mathf.Max(0.1f, tickSeconds);
            bool active = burnRemaining > 0f;
            if (burnRemaining <= 0f || damagePerTick / tickSeconds >= burnDamagePerTick / burnTickSeconds)
            {
                // Preserve tick progress on refresh so rapid attacks cannot postpone DOT.
                float progress = active ? burnTimer / burnTickSeconds : 0f;
                burnDamagePerTick = damagePerTick;
                burnTickSeconds = tickSeconds;
                burnTimer = progress * tickSeconds;
            }
            burnRemaining = Mathf.Max(burnRemaining, duration);
        }

        private void ClearBurn()
        {
            burnDamagePerTick = burnTickSeconds = burnRemaining = burnTimer = 0f;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || health <= 0f) return;
            float bonus = Stats != null ? Stats.healingBonusPercent : healingBonusPercent;
            health = Mathf.Min(MaxHealth, health + amount * (1f + Mathf.Max(0f, bonus) * 0.01f));
            Refresh(false, UpdateAnim.Heal);
        }

        private void OnEnable()
        {
            regenTimer = 0f;
            MiningLocalization.LanguageChanged += RefreshLabel;
        }

        private void OnDisable() => MiningLocalization.LanguageChanged -= RefreshLabel;

        private void Update()
        {
            TickBurn(Time.deltaTime);
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

        private void TickBurn(float deltaTime)
        {
            if (burnRemaining > 0f && health > 0f)
            {
                float activeTime = Mathf.Min(Mathf.Max(0f, deltaTime), burnRemaining);
                burnRemaining -= activeTime;
                burnTimer += activeTime;
                while (burnTickSeconds > 0f && burnTimer >= burnTickSeconds && health > 0f)
                {
                    burnTimer -= burnTickSeconds;
                    DealDamage(burnDamagePerTick);
                }
                if (burnRemaining <= 0f) ClearBurn();
            }
        }

        private void Awake()
        {
            combatInput = GetComponent<PlayerCombatInput>();
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
            bool mode = combatInput != null && combatInput.IsCombatMode;
            if (mode != displayedCombatMode) RefreshLabel();
            if (healthBar == null) return;
            if (healthCamera == null) healthCamera = Camera.main;
            if (healthCamera != null)
                healthBar.rotation = Quaternion.LookRotation(
                    healthBar.position - healthCamera.transform.position,
                    healthCamera.transform.up);
        }

        private void Refresh(bool skipAnimation, UpdateAnim updateType = UpdateAnim.Damage)
        {
            bool showBar = health < MaxHealth - 0.001f;
            // Enable before updating MicroBar so the first hit after spawning
            // animates correctly; hide the whole bar (including its text) at full HP.
            if (showBar && healthBar != null && !healthBar.gameObject.activeSelf)
                healthBar.gameObject.SetActive(true);
            // Only initialization/respawn snaps. Damage and healing use the
            // animation authored on the MicroBar (Flash, Fill, etc.).
            if (microBar != null) microBar.UpdateBar(health, skipAnimation, updateType);
            RefreshLabel();
            if (!showBar && healthBar != null && healthBar.gameObject.activeSelf)
                healthBar.gameObject.SetActive(false);
        }

        private void RefreshLabel()
        {
            displayedCombatMode = combatInput != null && combatInput.IsCombatMode;
            if (healthLabel != null)
            {
                var player = GetComponent<MiningPlayerStats>();
                var monster = GetComponent<MushroomMonster>();
                int level = player != null ? player.Level : monster != null ? monster.Level : 1;
                string status = combatInput != null
                    ? MiningLocalization.Text(displayedCombatMode ? "Combat" : "Standing") + "\n"
                    : string.Empty;
                healthLabel.text = $"{status}Lv. {level} | {Mathf.Ceil(health):0} / {Mathf.Ceil(MaxHealth):0}";
            }
        }

        private void OnValidate() => maxHealth = Mathf.Max(1f, maxHealth);
    }
}
