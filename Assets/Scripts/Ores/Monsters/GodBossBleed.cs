using UnityEngine;

namespace MiningSimulator.Ores
{
    // Refreshes rather than stacking; a lethal hit/respawn clears the old effect.
    public sealed class GodBossBleed : MonoBehaviour
    {
        private MiningCharacterHealth health;
        private GameObject source;
        private double elapsed, duration;
        private int nextTick;
        private float percent;
        private void Awake() { health = GetComponent<MiningCharacterHealth>(); health.Died += Clear; }
        public void Apply(float percentPerSecond, float seconds, GameObject attacker)
        { percent = Mathf.Max(0, percentPerSecond); duration = Mathf.Max(0, seconds); elapsed = 0; nextTick = 1; source = attacker; }
        private void Update()
        {
            if (elapsed >= duration || health.Health <= 0) return;
            elapsed = System.Math.Min(duration, elapsed + Time.deltaTime);
            while (nextTick <= elapsed && health.Health > 0)
            { nextTick++; health.DealDamage(health.MaxHealth * percent * .01f, CombatDamageType.True, source); }
        }
        public void Clear() { elapsed = duration = 0; nextTick = 1; }
        private void OnDisable() => Clear();
        private void OnDestroy() { if (health) health.Died -= Clear; }
    }
}
