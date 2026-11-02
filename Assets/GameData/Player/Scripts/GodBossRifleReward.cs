using MiningSimulator.Ores;
using UnityEngine;

namespace GameData.Player.Script
{
    /// <summary>Add only to the future God Boss, not to ordinary boss variants. No model/attacks/spawn policy.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterHealth))]
    public sealed class GodBossRifleReward : MonoBehaviour
    {
        [SerializeField] private DayNightSystem clock;
        private CharacterHealth health;
        public int EncounterDay { get; private set; }
        public bool DefeatedByPlayer => health && health.Health <= 0f && MonsterKillCredit.IsPlayerOwned(health.LastDamageSource);
        private void OnEnable()
        {
            health = GetComponent<CharacterHealth>();
            if (!clock) clock = FindAnyObjectByType<DayNightSystem>();
            EncounterDay = clock ? clock.DayNumber : 0;
            health.Died += OnDeath;
        }
        private void OnDisable() { if (health) health.Died -= OnDeath; }
        private void OnDeath()
        {
            if (!DefeatedByPlayer) return;
            var player = health.LastDamageSource.GetComponentInParent<PlayerWeaponProgression>();
            if (!player) player = FindAnyObjectByType<PlayerWeaponProgression>();
            if (player) player.TryGrantRifle(this);
        }
    }
}
