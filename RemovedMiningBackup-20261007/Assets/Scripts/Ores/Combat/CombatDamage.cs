using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum CombatDamageType { Physical, Magic, True }

    /// <summary>Shared resistance rule; callers supply the receiving actor's stats.</summary>
    public static class CombatDamage
    {
        public static float NonNegative(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);

        // Rating equal to scale halves damage. Default scale 100 matches LoL's positive-defense curve.
        private static double CurveScale(float scale) => Mathf.Max(.01f, NonNegative(scale));

        public static float ReductionFraction(float resistance, float scale = 100f)
        {
            double rating = NonNegative(resistance);
            return (float)(rating / (CurveScale(scale) + rating));
        }

        public static float Resolve(float amount, CombatDamageType type, float armor,
            float magicResistance, float scale)
        {
            amount = NonNegative(amount);
            if (type == CombatDamageType.True) return amount;
            double rating = NonNegative(type == CombatDamageType.Magic ? magicResistance : armor);
            double curve = CurveScale(scale);
            return (float)(amount * curve / (curve + rating));
        }
    }
}
