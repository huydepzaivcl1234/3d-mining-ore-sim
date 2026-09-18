using System.Collections;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Runtime-only night Point Light automatically added to every mining NPC.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningNpcHeadlamp : MonoBehaviour
    {
        private const string LampName = "Miner Headlamp";
        private const float FallbackHeadHeight = 1.45f;
        private const float FlickerDuration = 2f;
        private const float MinimumFlickerInterval = 0.06f;
        private const float MaximumFlickerInterval = 0.16f;

        private DayNightSystem dayNightSystem;
        private Transform headAnchor;
        private Light headlamp;
        private Coroutine flickerRoutine;
        private Vector3 localOffset = new(0f, 0.08f, 0.12f);
        private float steadyIntensity = 5.5f;
        private float lightRange = 12f;
        private Color lightColor = new(1f, 0.93f, 0.72f);

        public void Configure(Vector3 offset, float intensity, float range, Color color)
        {
            localOffset = offset;
            steadyIntensity = Mathf.Max(0f, intensity);
            lightRange = Mathf.Max(0.1f, range);
            lightColor = color;
            ApplyConfiguration();
        }

        private void Awake()
        {
            headAnchor = FindHeadAnchor();
            CreateHeadlamp();
        }

        private void Start()
        {
            dayNightSystem = FindFirstObjectByType<DayNightSystem>(FindObjectsInactive.Include);
            if (dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged += HandlePeriodChanged;
                ApplyPeriod(dayNightSystem.CurrentPeriod);
            }
            else
            {
                SetOff();
            }
        }

        private void OnDisable()
        {
            if (dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged -= HandlePeriodChanged;
            }

            StopFlicker();
            SetOff();
        }

        private void OnDestroy()
        {
            if (dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged -= HandlePeriodChanged;
            }
        }

        private void HandlePeriodChanged(MiningTimePeriod period)
        {
            ApplyPeriod(period);
        }

        private void ApplyPeriod(MiningTimePeriod period)
        {
            if (period == MiningTimePeriod.Night)
            {
                StartNightFlicker();
            }
            else
            {
                StopFlicker();
                SetOff();
            }
        }

        private void CreateHeadlamp()
        {
            Transform existing = transform.Find(LampName);
            if (existing != null)
            {
                headlamp = existing.GetComponent<Light>();
            }

            if (headlamp == null)
            {
                GameObject lampObject = new(LampName, typeof(Light));
                headlamp = lampObject.GetComponent<Light>();
            }

            ApplyConfiguration();
            headlamp.enabled = false;
        }

        private void ApplyConfiguration()
        {
            if (headlamp == null)
            {
                return;
            }

            Transform anchor = headAnchor != null ? headAnchor : transform;
            headlamp.transform.SetParent(anchor, false);
            headlamp.transform.localPosition = headAnchor != null
                ? localOffset
                : localOffset + Vector3.up * FallbackHeadHeight;
            headlamp.transform.localRotation = Quaternion.identity;
            headlamp.type = LightType.Point;
            headlamp.color = lightColor;
            headlamp.range = lightRange;
            headlamp.intensity = steadyIntensity;
            headlamp.shadows = LightShadows.None;
        }

        private Transform FindHeadAnchor()
        {
            Transform bestMatch = null;
            foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
            {
                string candidateName = candidate.name;
                if (candidateName.Equals("Miner Head", System.StringComparison.OrdinalIgnoreCase) ||
                    candidateName.Equals("Head", System.StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }

                if (bestMatch == null && candidateName.IndexOf("head",
                        System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bestMatch = candidate;
                }
            }

            return bestMatch;
        }

        private void StartNightFlicker()
        {
            StopFlicker();
            flickerRoutine = StartCoroutine(FlickerThenStayOn());
        }

        private IEnumerator FlickerThenStayOn()
        {
            float elapsed = 0f;
            while (elapsed < FlickerDuration)
            {
                if (headlamp == null)
                {
                    yield break;
                }

                bool lit = Random.value > 0.34f;
                headlamp.enabled = lit;
                headlamp.intensity = lit
                    ? steadyIntensity * Random.Range(0.45f, 1f)
                    : 0f;

                float interval = Random.Range(MinimumFlickerInterval, MaximumFlickerInterval);
                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }

            if (headlamp != null)
            {
                headlamp.enabled = true;
                headlamp.intensity = steadyIntensity;
            }

            flickerRoutine = null;
        }

        private void StopFlicker()
        {
            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
                flickerRoutine = null;
            }
        }

        private void SetOff()
        {
            if (headlamp == null)
            {
                return;
            }

            headlamp.enabled = false;
            headlamp.intensity = steadyIntensity;
        }
    }
}
