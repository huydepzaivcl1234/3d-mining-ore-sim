using System;
using System.Globalization;
using Lean.Localization;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Key-based translations for labels whose values are composed at runtime.</summary>
    public static class MiningPhrase
    {
        public static string Text(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning("A localization key was empty.");
                return string.Empty;
            }

            string result = LeanLocalization.GetTranslationText(key, replaceTokens: false);
            if (!string.IsNullOrEmpty(result)) return result;

            Debug.LogWarning($"Missing or empty Lean phrase: {key}");
            return key;
        }

        public static string Format(string key, params object[] values)
        {
            string pattern = Text(key);
            try
            {
                return string.Format(CultureInfo.CurrentCulture, pattern, values);
            }
            catch (FormatException exception)
            {
                Debug.LogWarning($"Invalid format for phrase {key}: {exception.Message}");
                return pattern;
            }
        }
    }
}
