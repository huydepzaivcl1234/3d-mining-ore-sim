#if UNITY_EDITOR
using UnityEditor;

namespace MiningSimulator.Ores.Editor
{
    [CustomEditor(typeof(ShopSmoothScrollRect)), CanEditMultipleObjects]
    public sealed class ShopSmoothScrollRectEditor : UnityEditor.UI.ScrollRectEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Smooth mouse wheel", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelStep"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelSmoothTime"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
