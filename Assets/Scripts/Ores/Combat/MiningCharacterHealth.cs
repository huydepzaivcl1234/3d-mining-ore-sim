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
        [Header("Micro Bar labels (optional styled layout)")]
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private string displayName;
        [SerializeField] private bool screenSpaceBar;
        private int displayedLevel = -1;
        private Camera healthCamera;
        private PlayerCombatInput combatInput;
        private bool displayedCombatMode;
        private float health;
        private bool damageEnabled = true;
        public void SetDamageEnabled(bool value) => damageEnabled = value;
        private int displayedBossSeconds = -1;
        private float healingBonusPercent;
        private MiningUpgradeSystem playerUpgrades;
        private MiningItemSystem equipmentItems;
        private MiningEquipmentBonuses EquipmentBonuses => equipmentItems != null ? equipmentItems.EquipmentBonuses : null;
        private readonly DamageOverTime burn = new DamageOverTime();
        private System.Action<float> burnDamage;
        public float Health => health;
        public Transform HealthBar => healthBar;
        private MiningPlayerStats playerStats;
        private MiningPlayerStats PlayerStats => playerStats != null ? playerStats : playerStats = GetComponent<MiningPlayerStats>();
        private MiningPlayerStatsData Stats => PlayerStats != null ? PlayerStats.Data : null;
        private float spawnedMaxHealth;
        public float MaxHealth => Mathf.Max(1f, Stats != null ? PlayerStats.MaxHealth : spawnedMaxHealth > 0 ? spawnedMaxHealth : maxHealth);
        public void ConfigureSpawnHealth(float value)
        {
            spawnedMaxHealth = Mathf.Max(1, value);
            initializedMaxHealth = MaxHealth;
            health = MaxHealth;
            if (microBar != null) microBar.Initialize(MaxHealth);
            Refresh(true);
        }
        public float RegenAmount => Mathf.Max(0f, Stats != null ? Stats.regenAmount : regenAmount);
        public float RegenInterval => Mathf.Max(0.1f,
            (Stats != null ? Stats.regenInterval : regenInterval) *
            (Stats != null && playerUpgrades != null
                ? playerUpgrades.GetMultiplier(MiningUpgradeType.RegenIntervalReduction) : 1f) *
            (Stats != null ? PlayerStats.CardRegenIntervalMultiplier : 1f) *
            (Stats != null && EquipmentBonuses != null ? EquipmentBonuses.RegenIntervalMultiplier : 1f));
        public float HealingMultiplier => 1f + (Mathf.Max(0f,
            Stats != null ? (EquipmentBonuses != null ? EquipmentBonuses.healingBonusPercent : 0f) : healingBonusPercent) +
            (Stats != null && playerUpgrades != null
                ? playerUpgrades.GetAddedPercent(MiningUpgradeType.HealingEffectiveness) : 0f) +
            (Stats != null ? PlayerStats.CardHealingPercent : 0f)) * 0.01f;
        public float EffectiveRegenAmount => RegenAmount * HealingMultiplier;
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
            if (!damageEnabled || amount <= 0f || health <= 0f) return 0f;
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
            burn.Apply(damagePerTick, tickSeconds, duration);
        }

        private void ClearBurn()
        {
            burn.Clear();
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || health <= 0f) return;
            health = Mathf.Min(MaxHealth, health + amount * HealingMultiplier);
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
            if (health > 0f) burn.Tick(deltaTime, burnDamage);
        }

        private void Awake()
        {
            burnDamage = ApplyDamage;
            combatInput = GetComponent<PlayerCombatInput>();
            if (GetComponent<MiningPlayerStats>() != null)
            {
                playerUpgrades = FindFirstObjectByType<MiningUpgradeSystem>(FindObjectsInactive.Include);
                equipmentItems = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            }
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
            var monster = GetComponent<MushroomMonster>();
            if (monster != null && monster.IsBoss)
            {
                int seconds = Mathf.CeilToInt(monster.CombatTimeRemaining);
                if (displayedBossSeconds != seconds) { displayedBossSeconds = seconds; RefreshLabel(); }
            }
            bool mode = combatInput != null && combatInput.IsCombatMode;
            if (mode != displayedCombatMode) RefreshLabel();
            if (PlayerStats != null && PlayerStats.Level != displayedLevel) RefreshLabel();
            if (healthBar == null) return;
            if (screenSpaceBar) return;
            if (healthCamera == null) healthCamera = Camera.main;
            if (healthCamera != null)
                healthBar.rotation = Quaternion.LookRotation(
                    healthBar.position - healthCamera.transform.position,
                    healthCamera.transform.up);
        }

        private void Refresh(bool skipAnimation, UpdateAnim updateType = UpdateAnim.Damage)
        {
            var monster = GetComponent<MushroomMonster>();
            bool showBar = screenSpaceBar || monster != null && monster.IsBoss || health < MaxHealth - 0.001f;
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
                var player = PlayerStats;
                var monster = GetComponent<MushroomMonster>();
                int level = player != null ? player.Level : monster != null ? monster.Level : 1;
                displayedLevel = level;
                string status = combatInput != null
                    ? MiningLocalization.Text(displayedCombatMode ? "Combat" : "Standing") + "\n"
                    : string.Empty;
                if (monster != null && monster.IsBoss)
                {
                    int seconds = Mathf.CeilToInt(monster.CombatTimeRemaining);
                    status = $"{MiningLocalization.TextKey("MONSTER_BOSS", "BOSS")} {seconds / 60:00}:{seconds % 60:00}\n";
                }
                if (levelLabel != null && nameLabel != null)
                {
                    levelLabel.text = level.ToString();
                    string title = string.IsNullOrWhiteSpace(displayName) ? gameObject.name.Replace("(Clone)", "") : displayName;
                    nameLabel.text = string.IsNullOrEmpty(status) ? MiningLocalization.Text(title) : status.Trim();
                    healthLabel.text = $"{Mathf.Ceil(health):0} / {Mathf.Ceil(MaxHealth):0}";
                }
                else
                    healthLabel.text = $"{status}Lv. {level} | {Mathf.Ceil(health):0} / {Mathf.Ceil(MaxHealth):0}";
            }
        }

        private void OnValidate() => maxHealth = Mathf.Max(1f, maxHealth);
    }
}
