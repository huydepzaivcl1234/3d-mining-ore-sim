using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Presentation-only, unscaled background drift for the showcase Main Menu.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningMainMenuShowcaseMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform background;
        [Min(0f), SerializeField] private float horizontalTravel = 9f;
        [Min(0.1f), SerializeField] private float cycleSeconds = 8f;
        [Range(1f, 1.1f), SerializeField] private float backgroundScale = 1.035f;

        private Vector2 authoredPosition;
        private bool cached;

        public void Configure(RectTransform target, float travel = 9f, float seconds = 8f,
            float scale = 1.035f)
        {
            background = target;
            horizontalTravel = Mathf.Max(0f, travel);
            cycleSeconds = Mathf.Max(0.1f, seconds);
            backgroundScale = Mathf.Clamp(scale, 1f, 1.1f);
            CacheAuthoredState();
            ApplyScale();
        }

        private void Awake()
        {
            CacheAuthoredState();
            ApplyScale();
        }

        private void OnEnable()
        {
            CacheAuthoredState();
            ApplyScale();
        }

        private void Update()
        {
            if (background == null)
            {
                return;
            }

            float phase = Time.unscaledTime * Mathf.PI * 2f / cycleSeconds;
            background.anchoredPosition = authoredPosition +
                                          Vector2.right * (Mathf.Sin(phase) * horizontalTravel);
        }

        private void OnDisable()
        {
            RestoreAuthoredState();
        }

        private void CacheAuthoredState()
        {
            if (background == null || cached)
            {
                return;
            }
            authoredPosition = background.anchoredPosition;
            cached = true;
        }

        private void ApplyScale()
        {
            if (background != null)
            {
                background.localScale = Vector3.one * backgroundScale;
            }
        }

        private void RestoreAuthoredState()
        {
            if (background == null || !cached)
            {
                return;
            }
            background.anchoredPosition = authoredPosition;
            background.localScale = Vector3.one * backgroundScale;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            horizontalTravel = Mathf.Max(0f, horizontalTravel);
            cycleSeconds = Mathf.Max(0.1f, cycleSeconds);
            backgroundScale = Mathf.Clamp(backgroundScale, 1f, 1.1f);
        }
#endif
    }
}
