#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace MiningSimulator.Ores.Editor
{
    [CustomEditor(typeof(UpgradePanelView))]
    public sealed class UpgradePanelViewEditor : UnityEditor.Editor
    {
        bool details;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("Edit Card Prefab to change all three cards. Use Game Data Icons to replace upgrade icons, or Authored Images to keep images from your prefab.",MessageType.Info);
            foreach(string field in new[]{"cardPrefab","cardsRoot","iconSource"})EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
            serializedObject.ApplyModifiedProperties();
            var view=(UpgradePanelView)target;
            using(new EditorGUI.DisabledScope(!view.cardPrefab||!view.cardsRoot||Application.isPlaying)){
                if(GUILayout.Button("Open Card Prefab")){AssetDatabase.OpenAsset(view.cardPrefab.gameObject);}
                if(GUILayout.Button("Apply Card Prefab To All 3 Choices"))view.ApplyCardPrefab();
            }
            details=EditorGUILayout.Foldout(details,"Panel bindings / optional sections",true);
            if(details){serializedObject.Update();var iterator=serializedObject.GetIterator();bool children=true;while(iterator.NextVisible(children)){children=false;if(iterator.name is "m_Script" or "cardPrefab" or "cardsRoot" or "iconSource")continue;EditorGUILayout.PropertyField(iterator,true);}serializedObject.ApplyModifiedProperties();}
        }
    }
}
#endif
