using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class MushnightSettings
    {
        public bool enabled;
        [Min(.1f)] public float stealSeconds = 3f;
        [Min(0f)] public float goldAtLevelOne = 10f;
        [Min(0f)] public float goldPerLevel = 5f;
        [Min(.1f)] public float stealRange = .65f;
        [Min(0f)] public float approachSpeed = 2f;
        [Min(0f)] public float escapeSpeed = 3.5f;
        [Min(.1f)] public float fleeFromPlayerRange = 5f;
        [Min(.1f)] public float safeFromPlayerRange = 8f;
        [Min(.1f)] public float escapeDistanceFromChest = 18f;
        [Min(.1f)] public float fleeGoalDistance = 5f;
        [Min(.05f)] public float fleeRepathSeconds = .6f;
        [Min(.1f)] public float minimumEscapeSeconds = 3f;
        public string idleState = "Idle", moveState = "Walk";
        [Header("Stamina (movement consumes it; exhausted thieves stand and recover)")]
        [Min(1f)] public float maxStamina = 100f;
        [Min(0f)] public float staminaPerMovingSecond = 10f;
        [Min(.1f)] public float staminaRecoveryPerSecond = 25f;
        [Min(0f)] public float staminaRecoveryDelay = 1f;
        [Range(.01f, 1f)] public float resumeStaminaFraction = .5f;
        [Header("Stolen gold above the head")]
        [Min(.1f)] public float goldLabelWidth = 2.2f;
        [Min(.1f)] public float goldLabelHeight = .42f;
        [Min(8f)] public float goldLabelFontSize = 28f;
        [Min(0f)] public float goldLabelHeadOffset = .35f;
        public Color goldLabelColor = new(1f, .8f, .16f, 1f);
        public TMPro.TMP_FontAsset goldLabelFont;

        public float GoldAtLevel(int level)
        {
            double amount = Safe(goldAtLevelOne) + (double)Safe(goldPerLevel) * Math.Max(0L, (long)level - 1L);
            return (float)Math.Min(float.MaxValue, amount);
        }
        private static float Safe(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    }
}
