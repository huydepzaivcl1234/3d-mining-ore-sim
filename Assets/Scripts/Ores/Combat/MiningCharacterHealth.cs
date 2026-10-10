using Microlight.MicroBar;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent]
    public sealed class MiningCharacterHealth : MonoBehaviour
    {
        [Min(1f), SerializeField] private float maxHealth = 100f;
        [Header("Defenses (player uses PlayerStatsData + equipped items)")]
        [Min(0f), SerializeField] private float armor;
        [Min(0f), SerializeField] private float magicResistance;
        [Min(.01f), SerializeField] private float resistanceScale = 100f;
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
        [SerializeField] private string healthValueSuffix = "";
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
        private GameObject burnSource;
        public GameObject LastDamageSource { get; private set; }
        public float Health => health;
        public Transform HealthBar => healthBar;
        private MushroomMonster combatOwner;
        private MonsterCombatSettings MonsterCombat => combatOwner != null ? combatOwner.CombatData : null;
        private MiningPlayerStats playerStats;
        private MiningPlayerStats PlayerStats => playerStats != null ? playerStats : playerStats = GetComponent<MiningPlayerStats>();
        private MiningPlayerStatsData Stats => PlayerStats != null ? PlayerStats.Data : null;
        public float Armor => CombatDamage.NonNegative(Stats != null ? PlayerStats.Armor : armor) +
            (Stats != null && EquipmentBonuses != null ? CombatDamage.NonNegative(EquipmentBonuses.armor) : 0f);
        public float MagicResistance => CombatDamage.NonNegative(Stats != null ? PlayerStats.MagicResistance : magicResistance) +
            (Stats != null && EquipmentBonuses != null ? CombatDamage.NonNegative(EquipmentBonuses.magicResistance) : 0f);
        public float ResistanceScale => Stats != null ? Stats.resistanceScale : resistanceScale;
        public void ConfigureDefenses(float armorRating, float magicRating, float scale = 100f)
        {
            armor = CombatDamage.NonNegative(armorRating);
            magicResistance = CombatDamage.NonNegative(magicRating);
            resistanceScale = Mathf.Max(.01f, CombatDamage.NonNegative(scale));
        }
        private float spawnedMaxHealth;
        public float MaxHealth => Mathf.Max(1f, Stats != null ? PlayerStats.MaxHealth : spawnedMaxHealth > 0 ? spawnedMaxHealth : MonsterCombat != null ? MonsterCombat.maxHealth : maxHealth);
        public void ConfigureSpawnHealth(float value)
        {
            spawnedMaxHealth = Mathf.Max(1, value);
            initializedMaxHealth = MaxHealth;
            health = MaxHealth;
            if (microBar != null) microBar.Initialize(MaxHealth);
            Refresh(true);
        }

        // Loading is not a hit: do not fire damage/death feedback or flash the HP bar.
        public void RestoreSavedHealth(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            health = Mathf.Clamp(value, 0f, MaxHealth);
            regenTimer = 0f;
            ClearBurn();
            Refresh(true);
        }

public void ConfigureMaximumHealth(float value, bool preserveDamage = true)
        {
            float oldMax = MaxHealth;
            bool alive = health > 0f;
            spawnedMaxHealth = Mathf.Max(1f, value);
            initializedMaxHealth = MaxHealth;
            health = alive ? Mathf.Clamp(preserveDamage ? health + MaxHealth - oldMax : MaxHealth, 0f, MaxHealth) : 0f;
            if (microBar != null) microBar.Initialize(MaxHealth);
            Refresh(true);
        }
        public void ConfigureRegeneration(float amount, float interval)
        { regenAmount = Mathf.Max(0f, amount); regenInterval = Mathf.Max(.1f, interval); regenTimer = 0f; }

        public float RegenAmount => Mathf.Max(0f, Stats != null ? Stats.regenAmount : MonsterCombat != null ? MonsterCombat.regenAmount : regenAmount);
        public float RegenInterval => Mathf.Max(0.1f,
            (Stats != null ? Stats.regenInterval : MonsterCombat != null ? MonsterCombat.regenInterval : regenInterval) *
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
            LastDamageSource = null;
            health = MaxHealth;
            regenTimer = 0f;
            ClearBurn();
            Refresh(true);
        }
        public void ApplyDamage(float amount)
        {
            DealDamage(amount);
        }
        public void ApplyDamage(float amount, CombatDamageType type) => DealDamage(amount, type);

        // The actual health removed drives life steal; overkill never heals the attacker.
        public float DealDamage(float amount) => DealDamage(amount, CombatDamageType.Physical);

        public float DealDamage(float amount, CombatDamageType type) => DealDamage(amount, type, null);

        public float DealDamage(float amount, CombatDamageType type, GameObject source)
            => DealDamageWithTrueBonus(amount, type, source, 0f);

        // One damage event for a normal strike plus its unmitigated on-hit bonus.
        public float DealDamageWithTrueBonus(float amount, CombatDamageType type, GameObject source, float trueBonus)
        {
            amount = CombatDamage.Resolve(amount, type, Armor, MagicResistance, ResistanceScale);
            amount += CombatDamage.NonNegative(trueBonus);
            if (PlayerStats != null) amount *= 1f - PlayerStats.ConsumableDamageReduction;
            if (!damageEnabled || amount <= 0f || health <= 0f) return 0f;
            float dealt = Mathf.Min(health, amount);
            LastDamageSource = source;
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
            => ApplyBurn(damagePerTick, tickSeconds, duration, null);

        public void ApplyBurn(float damagePerTick, float tickSeconds, float duration, GameObject source)
        {
            if (health <= 0f || damagePerTick <= 0f || tickSeconds <= 0f || duration <= 0f) return;
            if (!burn.IsActive || damagePerTick / Mathf.Max(.1f, tickSeconds) >= burn.DamagePerSecond)
                burnSource = source;
            burn.Apply(damagePerTick, tickSeconds, duration);
        }

        private void ClearBurn()
        {
            burn.Clear();
            burnSource = null;
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
                // Expiring maximum-HP buffs must not silently kill a low-health player.
                if (health > 0) health = Mathf.Clamp(health + Mathf.Max(0, MaxHealth - initializedMaxHealth), 0, MaxHealth);
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
            combatOwner = GetComponent<MushroomMonster>();
            burnDamage = ApplyBurnDamage;
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
                    healthLabel.text = $"{Mathf.Ceil(health):0} / {Mathf.Ceil(MaxHealth):0}{healthValueSuffix}";
                }
                else
                    healthLabel.text = $"{status}Lv. {level} | {Mathf.Ceil(health):0} / {Mathf.Ceil(MaxHealth):0}";
            }
        }

        private void OnValidate() => maxHealth = Mathf.Max(1f, maxHealth);
        private void ApplyBurnDamage(float amount) => DealDamage(amount, CombatDamageType.Magic, burnSource);
    }
}
