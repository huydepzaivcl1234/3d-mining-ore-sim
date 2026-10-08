using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonsterRewardData))]
public sealed class MonsterRewardDataEditor : Editor
{
    private void OnEnable()
    {
        var combat = serializedObject.FindProperty("combat");
        if (combat != null) combat.isExpanded = true;
    }

    public override void OnInspectorGUI()
    {
        var data = (MonsterRewardData)target;
        if (!data.HasCombatData)
        {
            EditorGUILayout.HelpBox("Legacy combat values still apply. Migrate an existing species before enabling defaults.", MessageType.Warning);
            if (GUILayout.Button("Enable combat settings for a NEW species"))
            {
                Undo.RecordObject(data, "Enable monster combat data");
                data.combat ??= new MonsterCombatSettings();
                data.combatSettingsVersion = 1;
                EditorUtility.SetDirty(data);
            }
        }
        DrawDefaultInspector();
        if (data.HasCombatData)
            EditorGUILayout.HelpBox($"Attack speed: {data.combat.SafeAttackSpeed:0.##}x. Cooldown: {data.combat.attackCooldown / data.combat.SafeAttackSpeed:0.###}s before boss buffs.", MessageType.Info);
    }
}
