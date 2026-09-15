using Lean.Localization;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Single entry point for language selection. Lean owns the translated text.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LeanLocalization))]
    public sealed class MiningLanguageController : MonoBehaviour
    {
        private const string SaveKey = "MiningSimulator.Language.v2";
        private const string English = "English";

        [SerializeField] private bool enableLanguageSwitching;
        [SerializeField] private string defaultLanguage = English;

        private LeanLocalization localization;
        private bool applying;

        public bool LanguageSwitchingEnabled => enableLanguageSwitching;
        public string CurrentLanguage => localization != null ? localization.CurrentLanguage : English;

        private void Awake()
        {
            localization = GetComponent<LeanLocalization>();
            MiningLocalization.LanguageSwitchingEnabled = enableLanguageSwitching;
        }

        private void OnEnable()
        {
            LeanLocalization.OnLocalizationChanged += EnforceLanguage;
        }

        private void Start()
        {
            if (localization == null)
            {
                Debug.LogError("A LeanLocalization component is required.", this);
                enabled = false;
                return;
            }

            // Lean's own save/detection would compete with this controller's explicit choice.
            localization.SaveLoad = LeanLocalization.SaveLoadType.None;
            localization.DetectLanguage = LeanLocalization.DetectType.None;
            localization.DefaultLanguage = English;
            string initial = enableLanguageSwitching
                ? PlayerPrefs.GetString(SaveKey, defaultLanguage)
                : English;
            if (string.IsNullOrWhiteSpace(initial) ||
                !LeanLocalization.CurrentLanguages.ContainsKey(initial))
            {
                Debug.LogWarning($"Saved/default Lean language '{initial}' is unavailable; using English.", this);
                initial = English;
            }
            SetLanguageInternal(initial);
        }

        private void OnDisable()
        {
            LeanLocalization.OnLocalizationChanged -= EnforceLanguage;
        }

        public bool TrySetLanguage(string languageName)
        {
            if (!enableLanguageSwitching || localization == null ||
                string.IsNullOrWhiteSpace(languageName))
            {
                return false;
            }

            languageName = languageName.Trim();
            if (!LeanLocalization.CurrentLanguages.ContainsKey(languageName))
            {
                Debug.LogWarning($"Lean language '{languageName}' is not registered.", this);
                return false;
            }

            SetLanguageInternal(languageName);
            PlayerPrefs.SetString(SaveKey, languageName);
            PlayerPrefs.Save();
            return true;
        }

        private void SetLanguageInternal(string languageName)
        {
            applying = true;
            try
            {
                // SetCurrentLanguage is an INSTANCE method in the vendored Lean version.
                localization.SetCurrentLanguage(languageName);
            }
            finally
            {
                applying = false;
            }
        }

        private void EnforceLanguage()
        {
            if (!applying && !enableLanguageSwitching && localization != null &&
                localization.CurrentLanguage != English)
            {
                SetLanguageInternal(English);
            }
        }
    }
}
