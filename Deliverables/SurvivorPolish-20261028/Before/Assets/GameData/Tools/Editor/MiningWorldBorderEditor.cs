using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(MiningWorldBorder)), CanEditMultipleObjects]
public sealed class MiningWorldBorderEditor : Editor
{
    private readonly BoxBoundsHandle boundsHandle = new BoxBoundsHandle();
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Select the border and drag its cyan Scene handles. X/Z = playable width/depth; Y = wall height. Move/rotate/scale the whole object normally.", MessageType.Info);
        if (GUILayout.Button("Create / repair border walls"))
            foreach (var item in targets) CreateWalls((MiningWorldBorder)item);
    }
    private void OnSceneGUI()
    {
        var border = (MiningWorldBorder)target;
        using (new Handles.DrawingScope(border.transform.localToWorldMatrix))
        {
            boundsHandle.wireframeColor = Color.cyan;
            boundsHandle.handleColor = Color.cyan;
            boundsHandle.center = Vector3.up * border.Size.y * 0.5f;
            boundsHandle.size = border.Size;
            EditorGUI.BeginChangeCheck();
            boundsHandle.DrawHandle();
            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(border, "Resize world border");
            Undo.RecordObject(border.transform, "Move world border edge");
            foreach (var collider in border.GetComponents<BoxCollider>())
                Undo.RecordObject(collider, "Resize border walls");
            Vector3 bottomCenter = boundsHandle.center - Vector3.up * boundsHandle.size.y * 0.5f;
            border.transform.position += border.transform.TransformVector(bottomCenter);
            border.SetSize(boundsHandle.size);
            foreach (var collider in border.GetComponents<BoxCollider>())
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            PrefabUtility.RecordPrefabInstancePropertyModifications(border);
            PrefabUtility.RecordPrefabInstancePropertyModifications(border.transform);
            EditorUtility.SetDirty(border);
        }
    }
    private static void CreateWalls(MiningWorldBorder border)
    {
        var so = new SerializedObject(border);
        var refs = so.FindProperty("walls");
        refs.arraySize = 4;
        for (int i = 0; i < 4; i++)
            if (refs.GetArrayElementAtIndex(i).objectReferenceValue == null)
                refs.GetArrayElementAtIndex(i).objectReferenceValue = Undo.AddComponent<BoxCollider>(border.gameObject);
        so.ApplyModifiedProperties();
        foreach (var collider in border.GetComponents<BoxCollider>()) Undo.RecordObject(collider, "Configure border wall");
        border.RebuildWalls();
        EditorUtility.SetDirty(border);
    }
    [MenuItem("Mining Simulator/Setup/Create World Border")]
    private static void CreateBorder()
    {
        var go = new GameObject("World Border");
        Undo.RegisterCreatedObjectUndo(go, "Create world border");
        var border = Undo.AddComponent<MiningWorldBorder>(go);
        CreateWalls(border);
        Selection.activeGameObject = go;
    }
}
