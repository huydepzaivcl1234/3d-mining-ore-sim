using UnityEngine;
using UnityEditor;
using MiningSimulator.Ores;
[CustomEditor(typeof(MonsterSpawnSettings))]
public sealed class MonsterSpawnSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var settings=(MonsterSpawnSettings)target;
        EditorGUILayout.LabelField($"Spawn rate: start {settings.RateAtSeconds(0):0.##}/s | 1 min {settings.RateAtSeconds(60):0.##}/s | 5 min {settings.RateAtSeconds(300):0.##}/s",EditorStyles.wordWrappedLabel);
        EditorGUILayout.HelpBox("Distances are in metres. Scene rings: yellow = minimum, cyan = maximum. Choose the spawn origin with Follow Player. God Boss and its summons use GodBossData.",MessageType.Info);
        EditorGUILayout.LabelField($"Level 15 at 5 min: {settings.RateAtSeconds(300,15):0.##}/s | Level 50: {settings.RateAtSeconds(300,50):0.##}/s",EditorStyles.wordWrappedLabel);
        EditorGUILayout.HelpBox("No per-frame or maximum-per-second spawn cap. Rate grows with time and player level. Maximum Living Monsters only limits the total population.",MessageType.Info);
        EditorGUILayout.LabelField($"Monsters per group: level 1 = {settings.GroupSizeAtLevel(1)} | level 50 = {settings.GroupSizeAtLevel(50)} | level 100 = {settings.GroupSizeAtLevel(100)}",EditorStyles.wordWrappedLabel);
        if(GUI.changed)SceneView.RepaintAll();
    }
    [DrawGizmo(GizmoType.Selected|GizmoType.NonSelected)]
    private static void DrawRanges(MonsterSpawnZone zone,GizmoType type)
    {
        if((type&GizmoType.Selected)==0 && Selection.activeObject!=zone.SpawnSettings)return;
        var center=zone.SpawnCenter+Vector3.up*.05f;var previous=Handles.color;
        Handles.color=Color.yellow;Handles.DrawWireDisc(center,Vector3.up,zone.SpawnMinimumDistance,2);
        Handles.Label(center+Vector3.right*zone.SpawnMinimumDistance,$"Minimum spawn: {zone.SpawnMinimumDistance:0.##} m");
        Handles.color=Color.cyan;Handles.DrawWireDisc(center,Vector3.up,zone.SpawnMaximumDistance,2);
        Handles.Label(center+Vector3.left*zone.SpawnMaximumDistance,$"Maximum spawn: {zone.SpawnMaximumDistance:0.##} m");Handles.color=previous;
    }
}


