using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Permanent bonuses while equipped; never a consumable or player baseline.</summary>
    [Serializable]
    public sealed class MiningEquipmentBonuses
    {
        [Min(0f)] public float armor;
        [Min(0f)] public float magicResistance;
        [Range(0f, 100f)] public float lifeStealPercent;
        [Min(0f)] public float healingBonusPercent;
        [Range(0f, 99f)] public float regenIntervalReductionPercent;
        [Min(0f)] public float burnDamagePerTick;
        [Min(.1f)] public float burnTickSeconds = 1f;
        [Min(0f)] public float burnDurationSeconds;

        public float LifeStealFraction => Mathf.Clamp(lifeStealPercent, 0f, 100f) * .01f;
        public float RegenIntervalMultiplier => 1f - Mathf.Clamp(regenIntervalReductionPercent, 0f, 99f) * .01f;

        public string Summary()
        {
            var lines = new List<string>(3);
            if (armor > 0f) lines.Add(string.Format(MiningLocalization.TextKey("EQUIPMENT_ARMOR_BONUS", "Armor +{0}"), armor.ToString("0.##")));
            if (magicResistance > 0f) lines.Add(string.Format(MiningLocalization.TextKey("EQUIPMENT_MR_BONUS", "MR +{0}"), magicResistance.ToString("0.##")));
            if (lifeStealPercent > 0f) lines.Add(string.Format(MiningLocalization.Text("Life steal +{0}%", "Hút máu +{0}%"), lifeStealPercent.ToString("0.##")));
            if (healingBonusPercent > 0f) lines.Add(string.Format(MiningLocalization.Text("Healing +{0}%", "Hồi máu +{0}%"), healingBonusPercent.ToString("0.##")));
            if (regenIntervalReductionPercent > 0f) lines.Add(string.Format(MiningLocalization.Text("Regen interval -{0}%", "Thời gian hồi máu -{0}%"), regenIntervalReductionPercent.ToString("0.##")));
            if (burnDamagePerTick > 0f && burnDurationSeconds > 0f)
                lines.Add(string.Format(MiningLocalization.Text("Burn {0} HP / {1}s for {2}s", "Đốt {0} máu / {1}s trong {2}s"), burnDamagePerTick.ToString("0.##"), burnTickSeconds.ToString("0.##"), burnDurationSeconds.ToString("0.##")));
            return string.Join("\n", lines);
        }
    }
}
