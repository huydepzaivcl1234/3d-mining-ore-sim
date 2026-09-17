using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MiningSimulator.Ores
{
    /// <summary>Presentation-only, unscaled background drift for the showcase Main Menu.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningMainMenuShowcaseMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform logo;
        [Min(0f), SerializeField] private float horizontalTravel = 9f;
        [Min(0f), SerializeField] private float verticalTravel = 5f;
        [Min(0f), SerializeField] private float pointerTravel = 18f;
        [Min(0.1f), SerializeField] private float followSharpness = 7f;
        [Min(0.1f), SerializeField] private float cycleSeconds = 8f;
        [Range(1f, 1.1f), SerializeField] private float backgroundScale = 1.035f;

        private Vector2 authoredPosition;
        private Vector2 logoAuthoredPosition;
        private Vector3 logoAuthoredScale = Vector3.one;
        private Quaternion logoAuthoredRotation = Quaternion.identity;
        private Vector2 displayedOffset;
        private bool cached;
        private bool logoCached;
        private bool cinematicActive;

        public void BeginCinematic()
        {
            cinematicActive = true;
            displayedOffset = Vector2.zero;
            if (background != null && cached)
            {
                background.anchoredPosition = authoredPosition;
            }
            RestoreLogo();
        }

        public void EndCinematic()
        {
            cinematicActive = false;
            displayedOffset = Vector2.zero;
        }

        public void Configure(RectTransform target, RectTransform logoTarget = null,
            float travel = 9f, float seconds = 8f, float scale = 1.035f)
        {
            if (background != target)
            {
                cached = false;
            }
            if (logo != logoTarget)
            {
                logoCached = false;
            }
            background = target;
            logo = logoTarget;
            horizontalTravel = Mathf.Max(0f, travel);
            cycleSeconds = Mathf.Max(0.1f, seconds);
            backgroundScale = Mathf.Clamp(scale, 1f, 1.1f);
            CacheAuthoredState();
            CacheLogoState();
            ApplyScale();
        }

        private void Awake()
        {
            CacheAuthoredState();
            CacheLogoState();
            ApplyScale();
        }

        private void OnEnable()
        {
            CacheAuthoredState();
            ApplyScale();
        }

        private void Update()
        {
            if (background == null || cinematicActive)
            {
                return;
            }

            float phase = Time.unscaledTime * Mathf.PI * 2f / cycleSeconds;
            Vector2 pointer = ReadNormalizedPointer();
            Vector2 ambient = new(Mathf.Sin(phase) * horizontalTravel,
                Mathf.Sin(phase * 0.73f + 0.8f) * verticalTravel);
            Vector2 parallax = new(-pointer.x * pointerTravel, -pointer.y * pointerTravel * 0.7f);
            Vector2 targetOffset = ambient + parallax;
            float blend = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
            displayedOffset = Vector2.Lerp(displayedOffset, targetOffset, blend);
            background.anchoredPosition = authoredPosition + displayedOffset;

            if (logo != null && logoCached)
            {
                float logoFloat = Mathf.Sin(phase * 0.83f + 0.35f);
                Vector2 logoParallax = new(pointer.x * 7f, pointer.y * 4f);
                logo.anchoredPosition = logoAuthoredPosition + logoParallax +
                                        new Vector2(0f, logoFloat * 10f);
                logo.localRotation = logoAuthoredRotation *
                                     Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.61f) * 1.4f);
                float pulse = 1f + Mathf.Sin(phase * 0.92f) * 0.012f;
                logo.localScale = logoAuthoredScale * pulse;
            }
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

        private void CacheLogoState()
        {
            if (logo == null || logoCached)
            {
                return;
            }
            logoAuthoredPosition = logo.anchoredPosition;
            logoAuthoredScale = logo.localScale;
            logoAuthoredRotation = logo.localRotation;
            logoCached = true;
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
            displayedOffset = Vector2.zero;
            RestoreLogo();
        }

        private void RestoreLogo()
        {
            if (logo == null || !logoCached)
            {
                return;
            }
            logo.anchoredPosition = logoAuthoredPosition;
            logo.localScale = logoAuthoredScale;
            logo.localRotation = logoAuthoredRotation;
        }

        private static Vector2 ReadNormalizedPointer()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return Vector2.zero;
            }

#if ENABLE_INPUT_SYSTEM
            Vector2 position = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#else
            Vector2 position = Input.mousePosition;
#endif
            return new Vector2(
                Mathf.Clamp((position.x / Screen.width - 0.5f) * 2f, -1f, 1f),
                Mathf.Clamp((position.y / Screen.height - 0.5f) * 2f, -1f, 1f));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            horizontalTravel = Mathf.Max(0f, horizontalTravel);
            verticalTravel = Mathf.Max(0f, verticalTravel);
            pointerTravel = Mathf.Max(0f, pointerTravel);
            followSharpness = Mathf.Max(0.1f, followSharpness);
            cycleSeconds = Mathf.Max(0.1f, cycleSeconds);
            backgroundScale = Mathf.Clamp(backgroundScale, 1f, 1.1f);
        }
#endif
    }
}
