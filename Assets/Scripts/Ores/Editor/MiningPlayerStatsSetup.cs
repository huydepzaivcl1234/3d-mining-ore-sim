using MiningSimulator.Ores;
using StarterAssets;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MiningPlayerStatsSetup
{
    private const string DataPath = "Assets/GameData/Player/PlayerStatsData.asset";
    [MenuItem("Mining Simulator/Setup/Player Stats GameData And Panel")]
    private static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogWarning("Stop Play Mode before setting up player stats."); return; }
        var movement = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<ThirdPersonController>() : null;
        if (movement == null) movement = Object.FindAnyObjectByType<ThirdPersonController>(FindObjectsInactive.Include);
        var upgrade = Object.FindAnyObjectByType<MiningUpgradePanel>(FindObjectsInactive.Include);
        var upgradeButton = upgrade != null ? new SerializedObject(upgrade).FindProperty("openButton").objectReferenceValue as Button : null;
        var canvas = upgradeButton != null ? upgradeButton.GetComponentInParent<Canvas>() : null;
        if (movement == null || canvas == null || upgradeButton == null)
        { Debug.LogError("Player or Upgrade button is missing. Open the gameplay scene first."); return; }
        var stats = movement.GetComponent<MiningPlayerStats>();
        var data = stats != null ? stats.Data : null;
        if (data == null) data = AssetDatabase.LoadAssetAtPath<MiningPlayerStatsData>(DataPath);
        if (data == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/GameData/Player")) AssetDatabase.CreateFolder("Assets/GameData", "Player");
            data = ScriptableObject.CreateInstance<MiningPlayerStatsData>();
            Copy(movement, data, "MoveSpeed", "SprintSpeed", "RotationSmoothTime", "SpeedChangeRate", "JumpHeight", "Gravity", "JumpTimeout", "FallTimeout");
            Copy(movement.GetComponent<MiningCharacterHealth>(), data, "maxHealth", "regenAmount", "regenInterval");
            Copy(movement.GetComponent<PlayerCombatInput>(), data, "damage", "attackRange", "attackAngle", "attackSpeed", "combatBlendSeconds", "hitTime", "hitOriginOffset");
            Copy(movement.GetComponent<PlayerDeathRespawn>(), data, "respawnSeconds");
            AssetDatabase.CreateAsset(data, DataPath);
        }
        if (stats == null) stats = Undo.AddComponent<MiningPlayerStats>(movement.gameObject);
        Set(stats, "data", data);
        var stamina = movement.GetComponent<MiningPlayerStamina>();
        if (stamina == null) stamina = Undo.AddComponent<MiningPlayerStamina>(movement.gameObject);
        EnsureStaminaHud(canvas.transform, stamina);
        EnsureHealthLabel(movement.GetComponent<MiningCharacterHealth>());
        var controllerTransform = canvas.transform.Find("Player Stats UI");
        GameObject controllerObject = controllerTransform != null ? controllerTransform.gameObject : Create("Player Stats UI", canvas.transform);
        var presenter = controllerObject.GetComponent<MiningPlayerStatsPanel>();
        if (presenter == null) presenter = Undo.AddComponent<MiningPlayerStatsPanel>(controllerObject);
        Transform existingButton = upgradeButton.transform.parent.Find("Player Stats Button");
        Button open = existingButton != null ? existingButton.GetComponent<Button>() : null;
        if (open == null)
        {
            open = Button("Player Stats Button", upgradeButton.transform.parent, "STATS", new Vector2(165, 64));
            var reference = (RectTransform)upgradeButton.transform;
            var rect = (RectTransform)open.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = reference.localScale;
            rect.localPosition = reference.localPosition + new Vector3(reference.rect.xMax * reference.localScale.x + rect.rect.width * rect.localScale.x * 0.5f + 12, 0, 0);
            open.transform.SetSiblingIndex(upgradeButton.transform.GetSiblingIndex() + 1);
        }
        var panelTransform = canvas.transform.Find("Player Stats Panel");
        RectTransform panel;
        if (panelTransform == null)
        {
            panel = (RectTransform)Create("Player Stats Panel", canvas.transform).transform;
            panel.sizeDelta = new Vector2(650, 550);
            var background = Undo.AddComponent<Image>(panel.gameObject);
            background.color = new Color(0.07f, 0.09f, 0.13f, 0.98f);
            Undo.AddComponent<CanvasGroup>(panel.gameObject);
            var outline = Undo.AddComponent<Outline>(panel.gameObject);
            outline.effectColor = new Color(0.85f, 0.57f, 0.13f);
            outline.effectDistance = new Vector2(3, -3);
            Text("Title", panel, "PLAYER STATS", new Vector2(590, 60), new Vector2(0, 220), 32, TextAlignmentOptions.Center);
            Text("Values", panel, "Player stats", new Vector2(570, 640), new Vector2(0, 10), 25, TextAlignmentOptions.TopLeft);
            var close = Button("Close", panel, "CLOSE", new Vector2(240, 60));
            ((RectTransform)close.transform).anchoredPosition = new Vector2(0, -220);
        }
        else panel = (RectTransform)panelTransform;
        EnsureProgressLayout(panel, presenter);
        var closeButton = panel.Find("Close")?.GetComponent<Button>();
        Set(presenter, "player", stats);
        Set(presenter, "panel", panel);
        Set(presenter, "openButton", open);
        Set(presenter, "closeButton", closeButton);
        Set(presenter, "title", panel.Find("Title")?.GetComponent<TMP_Text>());
        Set(presenter, "body", panel.Find("Values")?.GetComponent<TMP_Text>());
        Set(presenter, "openLabel", open.GetComponentInChildren<TMP_Text>(true));
        Set(presenter, "closeLabel", closeButton != null ? closeButton.GetComponentInChildren<TMP_Text>(true) : null);
        Set(presenter, "coordinator", Object.FindAnyObjectByType<MiningUiPanelCoordinator>(FindObjectsInactive.Include));
        EditorSceneManager.MarkSceneDirty(movement.gameObject.scene);
        AssetDatabase.SaveAssets();
        Selection.activeObject = data;
        Debug.Log("Stats migrated without changing existing values. Edit Assets/GameData/Player/PlayerStatsData.asset. Stats button and Player Stats Panel are under the HUD Canvas. Save the scene.");
    }
    private static void EnsureHealthLabel(MiningCharacterHealth health)
    {
        if (health == null) return;
        var serialized = new SerializedObject(health);
        var bar = serialized.FindProperty("healthBar").objectReferenceValue as Transform;
        if (bar == null) bar = health.transform.Find("Combat Health Bar");
        if (bar == null) return;
        var label = serialized.FindProperty("healthLabel").objectReferenceValue as TMP_Text;
        if (label == null) label = bar.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            var obj = new GameObject("Health Value", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(obj, "Create health value text");
            obj.transform.SetParent(bar, false);
            label = Undo.AddComponent<TextMeshPro>(obj);
            label.fontSize = 2; label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white; label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(3, 0.35f);
            label.transform.localPosition = new Vector3(0, 0, -0.02f);
            label.text = "Lv. 1 | 100 / 100";
        }
        Set(health, "healthBar", bar); Set(health, "healthLabel", label);
    }
    private static void EnsureStaminaHud(Transform canvas, MiningPlayerStamina stamina)
    {
        var existing = canvas.Find("Player Stamina Bar");
        var track = existing != null ? existing.gameObject : Create("Player Stamina Bar", canvas);
        var rect = (RectTransform)track.transform;
        var bar = track.GetComponent<Slider>();
        if (bar == null)
        {
            Undo.RecordObject(rect, "Position stamina bar");
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 40);
            rect.sizeDelta = new Vector2(360, 34);
            var background = Undo.AddComponent<Image>(track);
            background.color = new Color(0.025f, 0.035f, 0.05f, 0.95f); background.raycastTarget = false;
            var fill = Create("Fill", track.transform);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var image = Undo.AddComponent<Image>(fill);
            image.color = new Color(0.1f, 0.65f, 0.95f); image.raycastTarget = false;
            bar = Undo.AddComponent<Slider>(track);
            bar.fillRect = fillRect; bar.minValue = 0; bar.maxValue = 1;
            bar.interactable = false; bar.navigation = new Navigation { mode = Navigation.Mode.None };
            Text("Value", track.transform, "Stamina 100 / 100", new Vector2(350, 32), Vector2.zero, 21, TextAlignmentOptions.Center);
        }
        var hud = track.GetComponent<MiningPlayerStaminaHud>();
        if (hud == null) hud = Undo.AddComponent<MiningPlayerStaminaHud>(track);
        Set(hud, "player", stamina); Set(hud, "bar", bar);
        Set(hud, "label", track.transform.Find("Value")?.GetComponent<TMP_Text>());
    }
    private static void EnsureProgressLayout(RectTransform panel, MiningPlayerStatsPanel presenter)
    {
        var level = panel.Find("Level")?.GetComponent<TMP_Text>();
        if (level == null)
        {
            level = Text("Level", panel, "Lv. 1", new Vector2(570, 48), Vector2.zero, 30, TextAlignmentOptions.Left);
            PlaceFromTop((RectTransform)level.transform, 105);
        }
        var trackTransform = panel.Find("Experience Progress");
        UnityEngine.UI.Slider bar;
        if (trackTransform == null)
        {
            var track = Create("Experience Progress", panel);
            var rect = (RectTransform)track.transform;
            rect.sizeDelta = new Vector2(570, 35);
            PlaceFromTop(rect, 163);
            var background = Undo.AddComponent<UnityEngine.UI.Image>(track);
            background.color = new Color(0.025f, 0.03f, 0.04f);
            background.raycastTarget = false;
            var fill = Create("Fill", track.transform);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var image = Undo.AddComponent<UnityEngine.UI.Image>(fill);
            image.color = new Color(0.1f, 0.7f, 0.35f); image.raycastTarget = false;
            bar = Undo.AddComponent<UnityEngine.UI.Slider>(track);
            bar.fillRect = fillRect; bar.minValue = 0; bar.maxValue = 1;
            bar.interactable = false;
            bar.navigation = new Navigation { mode = Navigation.Mode.None };
            Text("Experience", track.transform, "0 / 100 XP", new Vector2(560, 32), Vector2.zero, 21, TextAlignmentOptions.Center);
            var body = panel.Find("Values") as RectTransform;
            if (body != null)
            {
                Undo.RecordObject(body, "Update compact stats layout");
                body.sizeDelta = new Vector2(570, 220);
                PlaceFromTop(body, 228);
                body.pivot = new Vector2(0.5f, 1);
            }
        }
        else bar = trackTransform.GetComponent<UnityEngine.UI.Slider>();
        Set(presenter, "levelLabel", level);
        Set(presenter, "experienceBar", bar);
        Set(presenter, "experienceLabel", panel.Find("Experience Progress/Experience")?.GetComponent<TMP_Text>());
    }
    private static void PlaceFromTop(RectTransform rect, float offset)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -offset);
    }
    private static void Copy(Component source, MiningPlayerStatsData destination, params string[] names)
    {
        if (source == null) return;
        var from = new SerializedObject(source);
        var to = new SerializedObject(destination);
        foreach (string name in names)
        {
            var a = from.FindProperty(name); var b = to.FindProperty(name);
            if (a == null || b == null) continue;
            if (a.propertyType == SerializedPropertyType.Vector3) b.vector3Value = a.vector3Value;
            else b.floatValue = a.floatValue;
        }
        to.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Set(Object target, string name, Object value)
    {
        Undo.RecordObject(target, "Wire Player Stats");
        var serialized = new SerializedObject(target);
        serialized.FindProperty(name).objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
    private static GameObject Create(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(obj, "Create Player Stats UI");
        obj.transform.SetParent(parent, false);
        return obj;
    }
    private static Button Button(string name, Transform parent, string label, Vector2 size)
    {
        var obj = Create(name, parent);
        ((RectTransform)obj.transform).sizeDelta = size;
        var image = Undo.AddComponent<Image>(obj);
        image.color = new Color(0.27f, 0.17f, 0.07f);
        var button = Undo.AddComponent<Button>(obj);
        button.targetGraphic = image;
        var text = Text("Label", obj.transform, label, size - new Vector2(12, 8), Vector2.zero, 25, TextAlignmentOptions.Center);
        text.color = new Color(1, 0.86f, 0.5f);
        return button;
    }
    private static TMP_Text Text(string name, Transform parent, string label, Vector2 size, Vector2 position, float fontSize, TextAlignmentOptions alignment)
    {
        var obj = Create(name, parent);
        var rect = (RectTransform)obj.transform;
        rect.sizeDelta = size; rect.anchoredPosition = position;
        var text = Undo.AddComponent<TextMeshProUGUI>(obj);
        text.text = label; text.fontSize = fontSize; text.alignment = alignment;
        text.color = Color.white; text.raycastTarget = false;
        text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = fontSize;
        return text;
    }
}
