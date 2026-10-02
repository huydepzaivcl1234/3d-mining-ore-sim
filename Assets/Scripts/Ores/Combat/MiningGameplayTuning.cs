using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>GameManager tuning bridge. GameData remains the source of player progression defaults.</summary>
    [DefaultExecutionOrder(-2000), DisallowMultipleComponent]
    public sealed class MiningGameplayTuning : MonoBehaviour
    {
        public static MiningGameplayTuning Current { get; private set; }
        [Header("Experience gain (1 = original, 0 = disabled)")]
        [Min(0), SerializeField] private float playerXpMultiplier = 1f;
        [Min(0), SerializeField] private float miningXpMultiplier = 1f;
        public float PlayerXpMultiplier => SafeMultiplier(playerXpMultiplier);
        public float MiningXpMultiplier => SafeMultiplier(miningXpMultiplier);
        private static float SafeMultiplier(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Max(0f, value);
        [Header("Boss testing - opt in, turn off before release")]
        [Tooltip("Allows boss variants below their configured level. Does not change player level or save.")]
        [SerializeField] private bool ignoreBossLevelRequirement;
        [Tooltip("Scheduled spawns become bosses when eligible. Ignores boss chance, not Boss Enabled.")]
        [SerializeField] private bool forceBossSpawns;
        public bool IgnoreBossLevel => ignoreBossLevelRequirement;
        public bool ForceBossSpawns => forceBossSpawns;
        [SerializeField] private MonsterSpawnZone monsterSpawner;
        [SerializeField] private MushroomMonster debugBossPrefab;
        [Header("Player progression - source asset and live player")]
        [SerializeField] private MiningPlayerStats player;
        [SerializeField] private MiningPlayerStatsData playerData;
        public MiningPlayerStats Player => player;
        public MiningPlayerStatsData PlayerData => playerData;
        [Header("Explicit Play Mode progress edit (saved only when applied)")]
        [Min(1), SerializeField] private int editPlayerLevel = 1;
        [Min(0), SerializeField] private float editPlayerExperience;
        [Min(1), SerializeField] private float editRequiredExperience = 100f;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCurrent() => Current = null;
        private void OnEnable()
        {
            if (Current != null && Current != this) { Debug.LogWarning("Only one enabled Gameplay Tuning is allowed.", this); enabled = false; return; }
            Current = this;
        }
        private void OnDisable() { if (Current == this) Current = null; }
        [ContextMenu("Debug/Spawn selected boss now")]
        public void SpawnSelectedBoss()
        {
            if (!Application.isPlaying || monsterSpawner == null || debugBossPrefab == null) { Debug.LogWarning("Enter Play Mode and assign Spawner and Debug Boss Prefab.", this); return; }
            if (!monsterSpawner.DebugSpawnBoss(debugBossPrefab)) Debug.LogWarning("Boss spawn failed: check boss enabled/level, mine surface and free spawn space. Enable Ignore Boss Level Requirement to test below the level gate.", this);
        }
        [ContextMenu("Player/Read current progress into edit fields")]
        public void ReadPlayerProgress()
        {
            if (player == null) return;
            editPlayerLevel = player.Level; editPlayerExperience = player.Experience; editRequiredExperience = player.ExperienceRequired;
        }
        [ContextMenu("Player/Apply edit fields to live progress (will be saved)")]
        public void ApplyPlayerProgress()
        {
            if (!Application.isPlaying || player == null) return;
            if (float.IsNaN(editPlayerExperience) || float.IsInfinity(editPlayerExperience) || float.IsNaN(editRequiredExperience) || float.IsInfinity(editRequiredExperience)) return;
            player.SetProgress(editPlayerLevel, editPlayerExperience, editRequiredExperience);
        }
    }
}
