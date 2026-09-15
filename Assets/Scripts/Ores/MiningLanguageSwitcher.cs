using MiningSimulator.Ores;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiningLanguageSwitcher : MonoBehaviour
{
    // Existing Unity Button bindings can keep calling this component.
    // MiningLocalization owns the saved selection and updates every Lean source.
    private void Start()
    {
        // LeanLocalization has completed its OnEnable registration by Start.
        // Applying here removes the initialization-order race that left some
        // panels in Vietnamese while Lean reported English (and vice versa).
        MiningLocalization.InitializeRuntime();
    }

    public void SetLanguage(string language)
    {
        if (!string.IsNullOrWhiteSpace(language))
        {
            MiningLocalization.SetLanguage(language);
        }
    }
}
