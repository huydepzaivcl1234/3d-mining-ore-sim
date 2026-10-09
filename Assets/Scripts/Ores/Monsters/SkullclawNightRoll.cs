using UnityEngine;

namespace MiningSimulator.Ores
{
    // One chance roll per night; failed placement does not consume or reroll it.
    public sealed class SkullclawNightRoll
    {
        private int day = -1;
        public bool Pending { get; private set; }
        public bool IsRolledFor(int night) => day == night;
        public void Roll(int night, float percent, float sample)
        {
            if (IsRolledFor(night)) return;
            day = night;
            Pending = Mathf.Clamp(percent, 0f, 100f) > 0f && sample < Mathf.Clamp01(percent * .01f);
        }
        public void MarkSpawned() => Pending = false;
    }
}
