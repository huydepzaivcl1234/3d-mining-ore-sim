using MiningSimulator.Ores;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiningLanguageSwitcher : MonoBehaviour
{
    // Existing Unity Button bindings can keep calling this component.
    // MiningLocalization owns the saved selection and updates every Lean source.
    public void SetLanguage(string language)
    {
        if (!string.IsNullOrWhiteSpace(language))
        {
            MiningLocalization.SetLanguage(language);
        }
    }
}
