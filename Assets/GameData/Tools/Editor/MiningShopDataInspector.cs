#if UNITY_EDITOR
using UnityEditor;
namespace MiningSimulator.Ores.Editor
{
    [CustomEditor(typeof(MiningShopData))]
    public sealed class MiningShopDataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Add products under Shop Products. Existing authored layout is preserved.", MessageType.Info);
        }
    }
}
#endif
