using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum CombatDamageType { Physical, Magic, True }

    /// <summary>Shared resistance rule; callers supply the receiving actor's stats.</summary>
    public static class CombatDamage
    {
        public static float NonNegative(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);

        // Scale is retained for existing callers; flat defenses do not use a curve.
        public static float Resolve(float amount, CombatDamageType type, float armor,
            float magicResistance, float scale)
        {
            amount = NonNegative(amount);
            if (type == CombatDamageType.True) return amount;
            float rating = NonNegative(type == CombatDamageType.Magic ? magicResistance : armor);
            return Mathf.Max(0f, amount - rating);
        }
    }
}
