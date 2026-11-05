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
        EditorGUILayout.HelpBox("A fresh run spawns one monster, then waits for its defeat. Regular spawns then arrive one at a time. Day/night and player level do not accelerate this pacing.",MessageType.Info);
        EditorGUILayout.LabelField($"Interval: first kill {settings.IntervalAfterKills(1):0.##}s | 10 kills {settings.IntervalAfterKills(10):0.##}s | 50 kills {settings.IntervalAfterKills(50):0.##}s | 120 kills {settings.IntervalAfterKills(120):0.##}s",EditorStyles.wordWrappedLabel);
        EditorGUILayout.HelpBox("Distances are metres. Yellow = minimum, cyan = maximum. Existing boss/summon rules remain separate after the first kill.",MessageType.Info);
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


