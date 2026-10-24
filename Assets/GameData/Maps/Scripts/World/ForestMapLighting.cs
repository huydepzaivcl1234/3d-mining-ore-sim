using UnityEngine;
namespace MiningSimulator.Ores
{
    /// <summary>Local warm guide light only; does not replace the existing day/night clock.</summary>
    public sealed class ForestMapLighting : MonoBehaviour
    {
        public Light guide;
        [Min(0)] public float dayIntensity = .15f;
        [Min(0)] public float nightIntensity = 3;
        private DayNightSystem clock;
        private float nextLookup;
        private void Update()
        {
            if (clock == null && Time.unscaledTime >= nextLookup)
            { clock = FindAnyObjectByType<DayNightSystem>(); nextLookup = Time.unscaledTime + 2; }
            if (guide == null) return;
            float target = clock != null && clock.CurrentPeriod == MiningTimePeriod.Night ? nightIntensity : dayIntensity;
            guide.intensity = Mathf.MoveTowards(guide.intensity, target, Time.deltaTime * 2);
        }
    }
}
