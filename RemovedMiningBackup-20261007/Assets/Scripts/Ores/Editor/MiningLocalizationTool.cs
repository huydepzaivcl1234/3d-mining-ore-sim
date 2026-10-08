using System;
using System.IO;
using System.Linq;
using Lean.Localization;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class MiningLocalizationTool : EditorWindow
{
    private string phraseKey = "UI_Inventory";

    [MenuItem("Mining Simulator/Localization/Open Tool")]
    private static void Open()
    {
        GetWindow<MiningLocalizationTool>("Mining Localization");
    }

    private void OnGUI()
    {
        GUILayout.Label("English-base migration", EditorStyles.boldLabel);

        if (GUILayout.Button("Prepare English Base"))
            PrepareEnglishBase();

        EditorGUILayout.Space();
        phraseKey = EditorGUILayout.TextField("Phrase key", phraseKey);

        TextMeshProUGUI label = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<TextMeshProUGUI>()
            : null;

        EditorGUILayout.ObjectField("Selected TMP label", label,
            typeof(TextMeshProUGUI), true);

        if (GUILayout.Button("Bind Selected Static TMP Label"))
            BindLabel(label, phraseKey);

        EditorGUILayout.Space();

        if (GUILayout.Button("Audit Old Language Code"))
            AuditOldCode();
    }

    private static LeanLocalization FindLocalization()
    {
        LeanLocalization lean =
            UnityEngine.Object.FindFirstObjectByType<LeanLocalization>(
                FindObjectsInactive.Include);

        if (lean == null)
            Debug.LogError("Open the game scene containing LeanLocalization first.");

        return lean;
    }

    private static void PrepareEnglishBase()
    {
        LeanLocalization lean = FindLocalization();
        if (lean == null) return;

        Undo.RecordObject(lean, "Prepare English localization");
        lean.DetectLanguage = LeanLocalization.DetectType.None;
        lean.SaveLoad = LeanLocalization.SaveLoadType.None;
        lean.DefaultLanguage = "English";
        lean.SetCurrentLanguage("English");
        EditorUtility.SetDirty(lean);

        Debug.Log("English base prepared. Saved player language remains intact. " +
            "Review and save the scene.");
    }

    private static void BindLabel(TextMeshProUGUI label, string key)
    {
        LeanLocalization lean = FindLocalization();
        if (lean == null) return;

        if (label == null)
        {
            Debug.LogWarning("Select one static TextMeshProUGUI GameObject.");
            return;
        }

        key = key?.Trim();
        if (string.IsNullOrWhiteSpace(key) || !key.StartsWith("UI_",
                StringComparison.Ordinal))
        {
            Debug.LogWarning("Enter a semantic key beginning with UI_.");
            return;
        }

        string english = label.text;
        if (string.IsNullOrWhiteSpace(english))
        {
            Debug.LogWarning("The selected label needs English text to import.");
            return;
        }

        // A script-owned counter or status label must use a keyed format in code.
        if (!EditorUtility.DisplayDialog("Confirm static label",
                $"Bind '{label.name}' to '{key}' using English text:\n\n{english}\n\n" +
                "Confirm that no script writes to this label at runtime.",
                "Bind", "Cancel"))
        {
            return;
        }

        LeanLocalizedTextMeshProUGUI existingBinding =
            label.GetComponent<LeanLocalizedTextMeshProUGUI>();

        if (existingBinding != null &&
            !string.IsNullOrEmpty(existingBinding.TranslationName) &&
            existingBinding.TranslationName != key)
        {
            Debug.LogWarning(
                $"Label already uses '{existingBinding.TranslationName}'. " +
                "Review that binding before replacing it.");
            return;
        }

        LeanPhrase phrase = lean.GetComponentsInChildren<LeanPhrase>(true)
            .FirstOrDefault(candidate => candidate.name == key);

        if (phrase == null)
        {
            if (LeanLocalization.CurrentTranslations.ContainsKey(key))
            {
                Debug.LogWarning(
                    $"Key '{key}' already exists in another Lean source.");
                return;
            }

            Transform container = lean.transform.Find("Phrases");
            if (container == null)
            {
                GameObject containerObject = new GameObject("Phrases");
                Undo.RegisterCreatedObjectUndo(
                    containerObject, "Create phrase container");
                Undo.SetTransformParent(containerObject.transform,
                    lean.transform, "Parent phrase container");
                container = containerObject.transform;
            }

            GameObject phraseObject = new GameObject(key);
            Undo.RegisterCreatedObjectUndo(phraseObject, "Create phrase");
            Undo.SetTransformParent(phraseObject.transform,
                container, "Parent phrase");

            phrase = Undo.AddComponent<LeanPhrase>(phraseObject);
            Undo.RecordObject(phrase, "Add English translation");
            phrase.AddEntry("English", english);
            EditorUtility.SetDirty(phrase);
        }
        else
        {
            LeanPhrase.Entry entry = null;
            if (!phrase.TryFindTranslation("English", ref entry) ||
                entry.Text != english)
            {
                Debug.LogWarning(
                    $"Phrase '{key}' has different or missing English text. " +
                    "Review the Phrase manually; nothing was overwritten.");
                return;
            }
        }

        LeanLocalizedTextMeshProUGUI binding = existingBinding != null
            ? existingBinding
            : Undo.AddComponent<LeanLocalizedTextMeshProUGUI>(
                label.gameObject);

        Undo.RecordObject(binding, "Bind localized TMP label");
        binding.FallbackText = english;
        binding.TranslationName = key;
        EditorUtility.SetDirty(binding);

        LeanLocalization.UpdateTranslations();
        Debug.Log($"Bound '{label.name}' to '{key}'.");
    }

    private static void AuditOldCode()
    {
        string scripts = Path.Combine(
            Application.dataPath, "Scripts", "Ores");

        if (!Directory.Exists(scripts))
        {
            Debug.LogWarning("Assets/Scripts/Ores was not found.");
            return;
        }

        var files = Directory.GetFiles(scripts, "*.cs",
                SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(
                "MiningLocalization.cs", StringComparison.Ordinal))
            .Select(path => new
            {
                Path = path,
                Count = File.ReadAllLines(path).Count(
                    line => line.Contains("MiningLocalization."))
            })
            .Where(item => item.Count > 0)
            .ToArray();

        int total = files.Sum(item => item.Count);
        string details = string.Join("\n", files.Select(item =>
            $"{item.Count,3}  " +
            item.Path.Substring(Application.dataPath.Length + 1)));

        Debug.Log($"{total} legacy language call lines remain:\n{details}");
    }
}
