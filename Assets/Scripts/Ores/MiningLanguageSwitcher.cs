using Lean.Localization;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LeanLocalization))]
public sealed class MiningLanguageSwitcher : MonoBehaviour
{
    private const string SaveKey = "MiningSimulator.Language.v2";
    private const string English = "English";

    [SerializeField] private bool enableLanguageSwitching = false;

    private LeanLocalization localization;
    private bool applyingLanguage;

    private void Awake()
    {
        localization = GetComponent<LeanLocalization>();
    }

    private void OnEnable()
    {
        LeanLocalization.OnLocalizationChanged += KeepEnglishWhileDisabled;
    }

    private void Start()
    {
        // Prevent Lean's device detection and save from competing with this choice.
        localization.DetectLanguage = LeanLocalization.DetectType.None;
        localization.SaveLoad = LeanLocalization.SaveLoadType.None;
        localization.DefaultLanguage = English;

        string language = enableLanguageSwitching
            ? PlayerPrefs.GetString(SaveKey, English)
            : English;

        if (!LeanLocalization.CurrentLanguages.ContainsKey(language))
            language = English;

        ApplyLanguage(language);
    }

    private void OnDisable()
    {
        LeanLocalization.OnLocalizationChanged -= KeepEnglishWhileDisabled;
    }

    // Assign this method to a Unity Button and enter "Vietnamese" or "English".
    public void SetLanguage(string language)
    {
        if (!enableLanguageSwitching || string.IsNullOrWhiteSpace(language))
            return;

        language = language.Trim();
        if (!LeanLocalization.CurrentLanguages.ContainsKey(language))
        {
            Debug.LogWarning($"Lean language '{language}' is not registered.", this);
            return;
        }

        ApplyLanguage(language);
        PlayerPrefs.SetString(SaveKey, language);
        PlayerPrefs.Save();
    }

    private void ApplyLanguage(string language)
    {
        applyingLanguage = true;
        try
        {
            localization.SetCurrentLanguage(language);
        }
        finally
        {
            applyingLanguage = false;
        }
    }

    private void KeepEnglishWhileDisabled()
    {
        if (!applyingLanguage && !enableLanguageSwitching &&
            localization != null && localization.CurrentLanguage != English)
        {
            ApplyLanguage(English);
        }
    }
}