using UnityEngine;

namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName = "Mining Simulator/Game Data/Monster Hit Feedback")]
    public sealed class MonsterHitFeedbackData : ScriptableObject
    {
        public bool enabled = true;
        [Min(.01f)] public float duration = .16f;
        [Range(0, .25f)] public float squash = .045f;
        [Range(0, 1)] public float flashStrength = .22f;
        public Color flashColor = Color.white;
        [Min(0)] public float emissionIntensity = .8f;
        private static MonsterHitFeedbackData current;
        private static bool loaded;
        public static MonsterHitFeedbackData Current
        {
            get { if (!loaded) { current = Resources.Load<MonsterHitFeedbackData>("MonsterHitFeedbackData"); loaded = true; } return current; }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() { loaded = false; current = null; }
    }
}
