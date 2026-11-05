using Microlight.MicroBar;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "MiningSimulator.Ores", "Assembly-CSharp", "MiningCharacterHealth")]
    public sealed partial class CharacterHealth : MonoBehaviour
    {
        [Min(1f), SerializeField] private float maxHealth = 100f;
        [Header("Fallback defenses for non-player actors (player uses Core Data)")]
        [Min(0f), SerializeField] private float armor;
        [Min(0f), SerializeField] private float magicResistance;
        [Min(.01f), SerializeField] private float resistanceScale = 100f;
        [Header("Fallback regeneration for non-player actors")]
        [Tooltip("HP restored per tick. Set 0 to disable; dead characters never regenerate.")]
        [Min(0f), SerializeField] private float regenAmount = 5f;
        [Min(0.1f), SerializeField] private float regenInterval = 5f;
        public string WorldSaveId {get;set;}
        private float regenTimer;
        public event System.Action Damaged;
        public static event System.Action<CharacterHealth,float,GameObject,bool,CombatDamageType> AnyDamaged;
        private GameData.Player.Script.GunSpecialUpgrades survival;
        public bool IsTargetable=>health>0&&!(survival&&survival.ReprieveActive);
        public static event System.Action<CharacterHealth,GameObject> AnyDied;
        private static readonly System.Collections.Generic.HashSet<CharacterHealth> activeMonsters=new();
        public static System.Collections.Generic.IReadOnlyCollection<CharacterHealth> ActiveMonsters=>activeMonsters;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDamageObservers() { AnyDamaged=null;AnyDied=null;activeMonsters.Clear(); }
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
        private float health;
        private bool damageEnabled = true;
        private GameData.Player.Script.SurvivorAbilities survivorAbilities;
        private GameData.Player.Script.SurvivorAbilities Abilities=>CrowdOwner?null:survivorAbilities?survivorAbilities:survivorAbilities=GetComponent<GameData.Player.Script.SurvivorAbilities>();
        public void SetDamageEnabled(bool value) => damageEnabled = value;
        private int displayedBossSeconds = -1;
        private float healingBonusPercent;
        
        private MiningItemSystem equipmentItems;
        private MiningEquipmentBonuses EquipmentBonuses => equipmentItems != null ? equipmentItems.EquipmentBonuses : null;
        private readonly DamageOverTime burn = new DamageOverTime();
        private System.Action<float> burnDamage;
        private GameObject burnSource;
        private GameObject burnEffect;
        private GameData.Player.Script.SurvivorAbilityData.LoopVisual burnVisual;
        public GameObject LastDamageSource { get; private set; }
        // Presentation context of this event, restored across nested on-hit damage.
        public bool IsPeriodicDamage { get; private set; }
        public bool IsBurnDamage { get; private set; }
        public bool IsAbilityProcDamage { get; private set; }
        public bool IsGunShot { get; private set; }
        public float Health => Application.isPlaying ? health : MaxHealth;
        public Transform HealthBar => healthBar;
        internal void AttachCrowdBar(CharacterHealth template, Transform bar)
        {
            healthBar=bar;microBar=bar.GetComponentInChildren<MicroBar>(true);
            TMP_Text Map(TMP_Text original){
                if(!original)return null;var names=new System.Collections.Generic.Stack<string>();var t=original.transform;
                while(t&&t!=template.healthBar){names.Push(t.name);t=t.parent;}
                if(!t)return null;t=bar;while(names.Count>0&&t)t=t.Find(names.Pop());return t?t.GetComponent<TMP_Text>():null;
            }
            healthLabel=Map(template.healthLabel);levelLabel=Map(template.levelLabel);nameLabel=Map(template.nameLabel);displayName=template.displayName;healthValueSuffix=template.healthValueSuffix;
            if(microBar)microBar.Initialize(MaxHealth);Refresh(false);
        }

        public SurvivorCrowd CrowdOwner { get; internal set; }
        public int CrowdSlot { get; internal set; }
        internal GameData.Player.Script.SurvivorEnemyStatus CrowdStatus;
        private MushroomMonster combatOwner;
        private GodBossController godOwner;
        public bool IsMonster => CrowdOwner != null || combatOwner != null || godOwner != null;
        private MonsterCombatSettings MonsterCombat => combatOwner != null ? combatOwner.CombatData : null;
        private PlayerStats playerStats;
        private WorldActorVisibility visibility;
        private static readonly Unity.Profiling.ProfilerMarker HealthMarker=new("Combat.HealthUpdate");
        private PlayerStats PlayerStats => CrowdOwner||combatOwner||godOwner ? null : playerStats != null ? playerStats : playerStats = GetComponent<PlayerStats>();
        private PlayerStatsData Stats => PlayerStats != null ? PlayerStats.Data : null;
        private bool HasPlayerStats => PlayerStats != null && (PlayerStats.CoreData != null || Stats != null);
        public float Armor => CombatDamage.NonNegative(HasPlayerStats ? PlayerStats.Armor : armor) +
            (HasPlayerStats && EquipmentBonuses != null ? CombatDamage.NonNegative(EquipmentBonuses.armor) : 0f)+(Abilities?Abilities.ArmorBonus:0);
        public float MagicResistance => CombatDamage.NonNegative(HasPlayerStats ? PlayerStats.MagicResistance : magicResistance) +
            (HasPlayerStats && EquipmentBonuses != null ? CombatDamage.NonNegative(EquipmentBonuses.magicResistance) : 0f)+(Abilities?Abilities.MagicResistanceBonus:0);
        public float ResistanceScale => HasPlayerStats ? PlayerStats.ResistanceScale : resistanceScale;
        public void ConfigureDefenses(float armorRating, float magicRating, float scale = 100f)
        {
            armor = CombatDamage.NonNegative(armorRating);
            magicResistance = CombatDamage.NonNegative(magicRating);
            resistanceScale = Mathf.Max(.01f, CombatDamage.NonNegative(scale));
        }
        private float spawnedMaxHealth;
        public float MaxHealth => Mathf.Max(1f, PlayerStats != null && (Stats != null || PlayerStats.CoreData != null) ? PlayerStats.MaxHealth : spawnedMaxHealth > 0 ? spawnedMaxHealth : MonsterCombat != null ? MonsterCombat.maxHealth : maxHealth);
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

        public float RegenAmount => Mathf.Max(0f, HasPlayerStats ? PlayerStats.RegenAmount : MonsterCombat != null ? MonsterCombat.regenAmount : regenAmount);
        public float RegenInterval => Mathf.Max(0.1f,
            (HasPlayerStats ? PlayerStats.RegenInterval : MonsterCombat != null ? MonsterCombat.regenInterval : regenInterval) *
            
            (HasPlayerStats ? PlayerStats.CardRegenIntervalMultiplier : 1f) *
            (HasPlayerStats && EquipmentBonuses != null ? EquipmentBonuses.RegenIntervalMultiplier : 1f));
        public float HealingMultiplier => 1f + (Mathf.Max(0f,
            HasPlayerStats ? (EquipmentBonuses != null ? EquipmentBonuses.healingBonusPercent : 0f) : healingBonusPercent) +
            
            (HasPlayerStats ? PlayerStats.CardHealingPercent : 0f)) * 0.01f;
        public float EffectiveRegenAmount => RegenAmount * HealingMultiplier;
        private float initializedMaxHealth;
        public void Respawn()
        {
            LastDamageSource = null;
            health = MaxHealth;
            regenTimer = 0f;
            ClearBurn();
            if(Abilities)Abilities.ResetLife();
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
        public float DealPeriodicDamage(float amount, CombatDamageType type, GameObject source)
            => DealDamageWithTrueBonus(amount, type, source, 0f, false, true);

        // One damage event for a normal strike plus its unmitigated on-hit bonus.
        public float DealDamageWithTrueBonus(float amount, CombatDamageType type, GameObject source, float trueBonus, bool basicAttack=false, bool periodic=false,bool abilityProc=false)
        {
            if(source&&source.TryGetComponent<GameData.Player.Script.SurvivorEnemyStatus>(out var status))amount=Mathf.Max(0,amount-status.DamageReduction);
            if(TryGetComponent<GameData.Player.Script.SurvivorEnemyStatus>(out var victimStatus))amount*=victimStatus.DamageTakenMultiplier;
            amount = CombatDamage.Resolve(amount, type, Armor, MagicResistance, ResistanceScale);
            amount += CombatDamage.NonNegative(trueBonus);
            if (PlayerStats != null) amount *= 1f - PlayerStats.ConsumableDamageReduction;
            if (!damageEnabled || amount <= 0f || health <= 0f || survival&&survival.ReprieveActive) return 0f;
            if(Abilities){amount=Abilities.ReceiveHit(amount,source);if(amount<=0)return 0;}
            float previous=health;
            LastDamageSource = source;
            health = Mathf.Max(0f, health - amount);
            if(health<=0&&survival&&survival.TryPreventDeath())health=Mathf.Min(1,MaxHealth);
            float dealt=previous-health;
            // A damage listener may apply a separate lethal proc. Only the hit that
            // actually crossed zero owns death notification (avoid nested double Died).
            bool killedByThisHit = health <= 0f;
            Refresh(false, UpdateAnim.Damage);
            bool previousProc=IsAbilityProcDamage;IsAbilityProcDamage=abilityProc;
            bool previousPeriodic = IsPeriodicDamage;
            bool previousGunShot = IsGunShot;
            IsPeriodicDamage = periodic;
            IsGunShot = basicAttack && !periodic && source && (source.GetComponent<GameData.Player.Script.PistolShooting>() != null || source.GetComponent<GameData.Player.Script.SurvivorAutoCombat>() != null);
            try
            {
                Damaged?.Invoke();
                AnyDamaged?.Invoke(this,dealt,source,basicAttack,type);
            }
            finally { IsAbilityProcDamage=previousProc;IsPeriodicDamage = previousPeriodic; IsGunShot = previousGunShot; }
            if (killedByThisHit)
            {
                ClearBurn();
                Died?.Invoke();
                AnyDied?.Invoke(this,source);
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
            bool ignited=!burn.IsActive;
            if (!burn.IsActive || damagePerTick / Mathf.Max(.1f, tickSeconds) >= burn.DamagePerSecond)
                burnSource = source;
            burn.Apply(damagePerTick, tickSeconds, duration);
            if(source&&source.TryGetComponent<GameData.Player.Script.SurvivorAutoCombat>(out var combat)){
                if(ignited)combat.PlayIgnite(transform.position);
                var stats=source.GetComponent<PlayerStats>();burnVisual=stats?stats.AbilityData.common.ignite.visual:GameData.Player.Script.SurvivorAbilityData.Fallback.common.ignite.visual;
                GameData.Player.Script.SurvivorLoopVfx.Set(ref burnEffect,burnVisual,transform,IsMonster&&burn.IsActive);
            }
        }

        private void ClearBurn()
        {
            burn.Clear();
            if(burnEffect)burnEffect.SetActive(false);
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
            if(IsMonster)activeMonsters.Add(this);
            regenTimer = 0f;
            MiningLocalization.LanguageChanged += RefreshLabel;
        }

        private void OnDisable() { if(burnEffect)burnEffect.SetActive(false);activeMonsters.Remove(this);MiningLocalization.LanguageChanged -= RefreshLabel; }

        private void Update()
        {
            if(CrowdOwner)return;
            TickCrowdHealth(Time.deltaTime);
        }
        internal bool NeedsCrowdHealthTick=>burn.IsActive||health<initializedMaxHealth;
        internal void TickCrowdHealth(float deltaTime)
        {
            if(visibility&&visibility.SimulationPaused)return;
            using var healthSample=HealthMarker.Auto();
            TickBurn(deltaTime);
            if(survival&&survival.ReprieveActive)return;
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
            regenTimer += deltaTime;
            float interval = RegenInterval;
            if (regenTimer < interval) return;
            int ticks = Mathf.FloorToInt(regenTimer / interval);
            regenTimer -= ticks * interval;
            Heal(RegenAmount * ticks);
        }

        private void TickBurn(float deltaTime)
        {
            if (health > 0f) burn.Tick(deltaTime, burnDamage);
            GameData.Player.Script.SurvivorLoopVfx.Set(ref burnEffect,burnVisual,transform,IsMonster&&health>0&&burn.IsActive);
        }

        private void Awake()
        {
            survival=GetComponent<GameData.Player.Script.GunSpecialUpgrades>();
            combatOwner = GetComponent<MushroomMonster>();
            godOwner = GetComponent<GodBossController>();
            if (IsMonster && Application.isPlaying && !CrowdOwner)
            {
                if (!GetComponent<MonsterHitFeedback>()) gameObject.AddComponent<MonsterHitFeedback>();
                WorldActorVisibility.Attach(gameObject);
                visibility=GetComponent<WorldActorVisibility>();
            }
            burnDamage = ApplyBurnDamage;
            if (GetComponent<PlayerStats>() != null)
            {
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
            if(CrowdOwner)return;
            if(visibility&&visibility.SimulationPaused)return;
            using var healthSample=HealthMarker.Auto();
            var monster = combatOwner;
            if (monster != null && monster.IsBoss)
            {
                int seconds = Mathf.CeilToInt(monster.CombatTimeRemaining);
                if (displayedBossSeconds != seconds) { displayedBossSeconds = seconds; RefreshLabel(); }
            }
            if (PlayerStats != null && PlayerStats.Level != displayedLevel) RefreshLabel();
            if (healthBar == null || !healthBar.gameObject.activeInHierarchy) return;
            if (screenSpaceBar) return;
            if (healthCamera == null) healthCamera = Camera.main;
            if (healthCamera != null)
                healthBar.rotation = Quaternion.LookRotation(
                    healthBar.position - healthCamera.transform.position,
                    healthCamera.transform.up);
        }

        private void Refresh(bool skipAnimation, UpdateAnim updateType = UpdateAnim.Damage)
        {
            var monster = combatOwner;
            bool showBar = screenSpaceBar || godOwner != null || monster != null && monster.IsBoss || health < MaxHealth - 0.001f;
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
            if (healthLabel != null)
            {
                var player = PlayerStats;
                var monster = combatOwner;
                int level = player != null ? player.Level : monster != null ? monster.Level : CrowdOwner ? CrowdOwner.LevelOf(CrowdSlot) : 1;
                displayedLevel = level;
                string status = string.Empty;
                if (monster != null && monster.IsBoss)
                {
                    int seconds = Mathf.CeilToInt(monster.CombatTimeRemaining);
                    status = MiningLocalization.TextKey("MONSTER_BOSS", "BOSS")+"\n";
                }
                // The player HUD has a level badge without a name label.
                // Update each optional label independently.
                if (levelLabel != null)
                {
                    levelLabel.text = level.ToString();
                }
                if (nameLabel != null)
                {
                    string title = string.IsNullOrWhiteSpace(displayName) ? gameObject.name.Replace("(Clone)", "") : displayName;
                    nameLabel.text = string.IsNullOrEmpty(status) ? MiningLocalization.Text(title) : status.Trim();
                }
                if (levelLabel != null)
                {
                    healthLabel.text = $"{Mathf.Ceil(health):0} / {Mathf.Ceil(MaxHealth):0}{healthValueSuffix}";
                }
                else if (godOwner != null)
                    healthLabel.text = $"GOD BOSS | {Mathf.Ceil(health):0} / {Mathf.Ceil(MaxHealth):0}";
                else
                    healthLabel.text = $"{status}Lv. {level} | {Mathf.Ceil(health):0} / {Mathf.Ceil(MaxHealth):0}";
            }
        }

        private void OnValidate() => maxHealth = Mathf.Max(1f, maxHealth);
        private void ApplyBurnDamage(float amount)
        {
            bool previous=IsBurnDamage;IsBurnDamage=true;
            try{DealPeriodicDamage(amount,CombatDamageType.Magic,burnSource);}
            finally{IsBurnDamage=previous;}
        }
    }
}
