using UnityEngine;

namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName = "Mining Simulator/Monsters/Skullclaw")]
    public sealed class SkullclawData : MonsterRewardData
    {
        [Header("Night encounter: roll once, at most one per night")]
        [Range(0f, 100f)] public float nightSpawnChance = 30f;
        [Range(0f, .95f)] public float nightStartProgress = .05f;
        [Header("Skullclaw: right swipe, left swipe, jump area")]
        public AnimationClip idleClip, walkClip, rightSwipeClip, leftSwipeClip, jumpClip;
        [Range(0f, 1f)] public float rightContact = .49f;
        [Range(0f, 1f)] public float leftContact = .49f;
        [Range(0f, 1f)] public float jumpContact = .44f;
        [Range(0f, 1f)] public float takeoff = .15f;
        [Range(0f, 1f)] public float landing = .43f;
        [Min(.1f)] public float jumpRange = 5f;
        [Min(0f)] public float jumpApproachDelay = .5f;
        [Min(.1f)] public float jumpAreaRadius = 2.2f;
        [Tooltip("Normalized, extracted from the supplied jump clip. The motor owns this displacement, not the mesh.")]
        public AnimationCurve jumpHeight = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        public string AttackState(int step) => step == 2 ? "Jump Attack" : step == 1 ? "Swipe Left" : "Swipe Right";
        public float Contact(int step) => step == 2 ? jumpContact : step == 1 ? leftContact : rightContact;
    }

    /// <summary>Only completed attack starts advance the three-step sequence.</summary>
    public sealed class SkullclawCombo
    {
        public int Next { get; private set; }
        public int Current { get; private set; } = -1;
        public int Begin() { Current = Next; Next = (Next + 1) % 3; return Current; }
        public int Begin(bool gapCloser)
        {
            // Close combat alternates swipes. The third attack is a distance-gated approach only.
            if (!gapCloser && Next == 2) Next = 0;
            // A distant approach leap does not consume the waiting melee step.
            if (gapCloser && Next != 2) return Current = 2;
            return Begin();
        }
        public void Reset() { Next = 0; Current = -1; }
    }

    public sealed class SkullclawJumpGate
    {
        public float ReadySeconds { get; private set; }
        public void Reset() => ReadySeconds = 0f;
        public void Tick(bool inApproachBand, float dt)
            => ReadySeconds = inApproachBand ? ReadySeconds + Mathf.Max(0f, dt) : 0f;
        public bool Ready(float delay) => ReadySeconds >= Mathf.Max(0f, delay);
    }
}
