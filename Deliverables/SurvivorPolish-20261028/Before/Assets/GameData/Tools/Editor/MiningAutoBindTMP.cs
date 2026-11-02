using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lean.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MiningAutoBindTMP
{
    [MenuItem("Mining Simulator/Localization/Auto Bind Scene TMP")]
    private static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        LeanLocalization lean = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<LeanLocalization>(true))
            .FirstOrDefault();

        if (lean == null)
        {
            Debug.LogError("No LeanLocalization found in the open scene.");
            return;
        }

        string englishPath = Path.Combine(
            Application.dataPath, "GameData/Localization/English.txt");
        string vietnamesePath = Path.Combine(
            Application.dataPath, "GameData/Localization/Vietnamese.txt");

        if (!File.Exists(englishPath) || !File.Exists(vietnamesePath))
        {
            Debug.LogError("English.txt or Vietnamese.txt is missing.");
            return;
        }

        Dictionary<string, string> english = ReadTranslations(englishPath);
        Dictionary<string, string> vietnamese = ReadTranslations(vietnamesePath);

        Transform phrases = lean.transform.Find("Phrases");
        if (phrases == null)
        {
            GameObject container = new GameObject("Phrases");
            Undo.RegisterCreatedObjectUndo(container, "Create phrase container");
            Undo.SetTransformParent(container.transform, lean.transform,
                "Parent phrase container");
            phrases = container.transform;
        }

        int bound = 0;
        int skipped = 0;
        var usedKeys = new HashSet<string>(
            lean.GetComponentsInChildren<LeanPhrase>(true)
                .Select(phrase => phrase.name));

        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (TextMeshProUGUI label in
                     root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label.GetComponent<LeanLocalizedTextMeshProUGUI>() != null)
                    continue;

                string currentText = label.text?.Trim();

                // Bind only text with an existing English AND Vietnamese translation.
                if (string.IsNullOrEmpty(currentText) ||
                    !english.TryGetValue(currentText, out string englishText) ||
                    !vietnamese.TryGetValue(currentText, out string vietnameseText) ||
                    string.IsNullOrWhiteSpace(vietnameseText))
                {
                    skipped++;
                    continue;
                }

                string baseKey = "UI_" + MakeKey(
                    label.transform.parent.name + "_" + label.name);
                string key = baseKey;
                for (int suffix = 2; usedKeys.Contains(key); suffix++)
                    key = baseKey + "_" + suffix;

                usedKeys.Add(key);

                GameObject phraseObject = new GameObject(key);
                Undo.RegisterCreatedObjectUndo(phraseObject, "Create UI phrase");
                Undo.SetTransformParent(phraseObject.transform, phrases,
                    "Parent UI phrase");

                LeanPhrase phrase = Undo.AddComponent<LeanPhrase>(phraseObject);
                Undo.RecordObject(phrase, "Add translations");
                phrase.AddEntry("English", englishText);
                phrase.AddEntry("Vietnamese", vietnameseText);
                EditorUtility.SetDirty(phrase);

                LeanLocalizedTextMeshProUGUI binding =
                    Undo.AddComponent<LeanLocalizedTextMeshProUGUI>(
                        label.gameObject);
                Undo.RecordObject(binding, "Bind TMP translation");
                binding.FallbackText = englishText;
                binding.TranslationName = key;
                EditorUtility.SetDirty(binding);

                bound++;
            }

        LeanLocalization.UpdateTranslations();
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"Bound {bound} TMP labels. Skipped {skipped} labels without " +
                  "a matching translation. Review the new Phrases, then save the scene.");
    }

    private static Dictionary<string, string> ReadTranslations(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string line in File.ReadAllLines(path))
        {
            int separator = line.IndexOf(" = ", StringComparison.Ordinal);
            if (separator < 0) continue;

            string source = line.Substring(0, separator).Trim();
            string value = line.Substring(separator + 3).Trim();
            if (source.Length > 0 && value.Length > 0)
                result[source] = value;
        }

        return result;
    }

    private static string MakeKey(string value)
    {
        var key = new StringBuilder();
        bool lastWasUnderscore = false;

        foreach (char character in value)
        {
            if (character <= 127 && char.IsLetterOrDigit(character))
            {
                key.Append(char.ToUpperInvariant(character));
                lastWasUnderscore = false;
            }
            else if (key.Length > 0 && !lastWasUnderscore)
            {
                key.Append('_');
                lastWasUnderscore = true;
            }
        }

        return key.ToString().TrimEnd('_');
    }
}