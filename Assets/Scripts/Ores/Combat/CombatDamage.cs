using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum CombatDamageType { Physical, Magic, True }

    /// <summary>Shared resistance rule; callers supply the receiving actor's stats.</summary>
    public static class CombatDamage
    {
        public static float NonNegative(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);

        public static float ReductionFraction(float resistance, float scale)
        {
            double rating = NonNegative(resistance);
            double curve = Mathf.Max(.01f, NonNegative(scale));
            return (float)(rating / (curve + rating));
        }

        public static float Resolve(float amount, CombatDamageType type, float armor,
            float magicResistance, float scale)
        {
            amount = NonNegative(amount);
            if (type == CombatDamageType.True) return amount;
            double rating = NonNegative(type == CombatDamageType.Magic ? magicResistance : armor);
            double curve = Mathf.Max(.01f, NonNegative(scale));
            return (float)(amount * curve / (curve + rating));
        }
    }
}
