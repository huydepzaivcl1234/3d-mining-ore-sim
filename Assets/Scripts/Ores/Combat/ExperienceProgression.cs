using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Player XP arithmetic, independent of input, movement, potions and saves.</summary>
    public sealed class ExperienceProgression
    {
        public int Level { get; private set; } = 1;
        public float Experience { get; private set; }
        public float Required { get; private set; } = 100f;

        public void Set(int level, float experience, float required)
        {
            Level = Mathf.Max(1, level);
            Required = Mathf.Max(1f, required);
            Experience = Mathf.Clamp(experience, 0f, Required);
        }

        public void Add(float amount, float growth)
        {
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            growth = Mathf.Max(1f, growth);
            double remaining = (double)Experience + amount;
            if (growth == 1f && remaining >= Required)
            {
                int levels = (int)Math.Min(int.MaxValue - Level, Math.Floor(remaining / Required));
                Level += levels;
                remaining -= (double)levels * Required;
            }
            while (remaining >= Required && Level < int.MaxValue)
            {
                remaining -= Required;
                Level++;
                Required = Mathf.Min(float.MaxValue, Required * growth);
            }
            Experience = (float)Math.Min(remaining, Required);
        }
    }
}
