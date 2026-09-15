using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lean.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Explicit migration commands; never rewrites dynamic labels or vendor assets.</summary>
    public static class MiningLanguageCleanupTool
    {
        private const string Menu = "Mining Simulator/Localization/";

        [MenuItem(Menu + "Prepare English Base")]
        private static void PrepareEnglishBase()
        {
            LeanLocalization lean = UnityEngine.Object.FindFirstObjectByType<LeanLocalization>(
                FindObjectsInactive.Include);
            if (lean == null)
            {
                Debug.LogError("No LeanLocalization in the open scene. Add one before migration.");
                return;
            }

            if (lean.GetComponent<MiningLanguageController>() == null)
            {
                Undo.AddComponent<MiningLanguageController>(lean.gameObject);
            }

            Undo.RecordObject(lean, "Pin Lean Localization to English");
            lean.DetectLanguage = LeanLocalization.DetectType.None;
            lean.DefaultLanguage = "English";
            lean.SaveLoad = LeanLocalization.SaveLoadType.None;
            lean.SetCurrentLanguage("English");
            EditorUtility.SetDirty(lean);
            ClearLegacyLanguagePreferences();
            Debug.Log("English base prepared. Language switching is disabled on the new controller.");
        }

        [MenuItem(Menu + "Clear Legacy Language Preferences")]
        private static void ClearLegacyLanguagePreferences()
        {
            foreach (string key in new[]
                     {
                         "Mining.Language", "Mining.LanguageName",
                         "LeanLocalization.CurrentLanguage",
                         "LeanLocalization.CurrentLanguageAlt"
                     })
            {
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }

        [MenuItem(Menu + "Bind Selected Static TMP Labels To Keys")]
        private static void BindSelectedLabels()
        {
            LeanLocalization lean = UnityEngine.Object.FindFirstObjectByType<LeanLocalization>(
                FindObjectsInactive.Include);
            if (lean == null)
            {
                Debug.LogError("No LeanLocalization in the open scene.");
                return;
            }

            TextMeshProUGUI[] labels = Selection.gameObjects
                .SelectMany(root => root.GetComponentsInChildren<TextMeshProUGUI>(true))
                .Distinct().Where(label => label != null && !string.IsNullOrWhiteSpace(label.text) &&
                    label.GetComponent<LeanLocalizedTextMeshProUGUI>() == null).ToArray();
            if (labels.Length == 0)
            {
                Debug.Log("Select a static TMP label or a hierarchy containing static labels.");
                return;
            }

            if (!EditorUtility.DisplayDialog("Bind static labels",
                    $"Bind {labels.Length} selected TMP labels? Only select labels whose text is " +
                    "static; scripts that assign text at runtime need keyed formats instead.",
                    "Bind", "Cancel")) return;

            Transform container = lean.transform.Find("Phrases");
            if (container == null)
            {
                GameObject root = new GameObject("Phrases");
                Undo.RegisterCreatedObjectUndo(root, "Create phrase container");
                Undo.SetTransformParent(root.transform, lean.transform, "Parent phrases");
                container = root.transform;
            }

            HashSet<string> used = new HashSet<string>(
                lean.GetComponentsInChildren<LeanPhrase>(true).Select(phrase => phrase.name),
                StringComparer.Ordinal);
            used.UnionWith(LeanLocalization.CurrentTranslations.Keys);
            int count = 0;
            foreach (TextMeshProUGUI label in labels)
            {
                string english = label.text;
                string baseKey = "UI_" + Sanitize(label.transform.parent != null
                    ? label.transform.parent.name + "_" + label.name
                    : label.name);
                string key = baseKey;
                for (int suffix = 2; used.Contains(key); suffix++) key = baseKey + "_" + suffix;
                used.Add(key);

                GameObject phraseObject = new GameObject(key);
                Undo.RegisterCreatedObjectUndo(phraseObject, "Create English phrase");
                Undo.SetTransformParent(phraseObject.transform, container, "Parent phrase");
                LeanPhrase phrase = Undo.AddComponent<LeanPhrase>(phraseObject);
                Undo.RecordObject(phrase, "Add English translation");
                phrase.AddEntry("English", english);
                EditorUtility.SetDirty(phrase);

                LeanLocalizedTextMeshProUGUI binding =
                    Undo.AddComponent<LeanLocalizedTextMeshProUGUI>(label.gameObject);
                Undo.RecordObject(binding, "Bind TMP phrase key");
                binding.FallbackText = english;
                binding.TranslationName = key;
                EditorUtility.SetDirty(binding);
                count++;
            }

            LeanLocalization.UpdateTranslations();
            Debug.Log($"Bound {count} static TMP labels. Check generated UI_ keys and English values " +
                      "in the Phrases hierarchy; add other languages there later.");
        }

        [MenuItem(Menu + "Audit Legacy Language Calls")]
        private static void AuditLegacyCalls()
        {
            string root = Path.Combine(Application.dataPath, "Scripts", "Ores");
            if (!Directory.Exists(root)) return;
            var report = new StringBuilder("Remaining MiningLocalization calls:\n");
            int total = 0;
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                         .Where(path => !path.EndsWith("MiningLocalization.cs", StringComparison.Ordinal)))
            {
                int count = File.ReadAllLines(file).Count(line => line.Contains("MiningLocalization."));
                if (count == 0) continue;
                total += count;
                report.AppendLine($"{count,3}  {file.Substring(Application.dataPath.Length + 1)}");
            }
            Debug.Log($"{total} legacy call lines remain. Migrate dynamic text to explicit " +
                      "phrase keys and formats before removing MiningLocalization.\n" + report);
        }

        private static string Sanitize(string value)
        {
            var result = new StringBuilder();
            bool underscore = false;
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character) && character <= 127)
                {
                    result.Append(char.ToUpperInvariant(character));
                    underscore = false;
                }
                else if (!underscore && result.Length > 0)
                {
                    result.Append('_');
                    underscore = true;
                }
            }
            return result.ToString().TrimEnd('_');
        }
    }
}
