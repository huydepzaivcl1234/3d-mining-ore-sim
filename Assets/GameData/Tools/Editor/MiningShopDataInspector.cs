using UnityEditor;
using UnityEngine.UIElements;
namespace MiningSimulator.Ores.Editor
{
    [CustomEditor(typeof(MiningShopData))]
    public sealed class MiningShopDataInspector : UnityEditor.Editor
    {public override VisualElement CreateInspectorGUI()=>GroupedGachaInspector.Create(this,serializedObject,target);}
}
