using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GameData.Player.Script;

[CustomEditor(typeof(SurvivorAbilityData))]
public sealed class SurvivorAbilityDataEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var data=(SurvivorAbilityData)target;
        var root=new VisualElement();
        root.Add(new HelpBox("All abilities are configured here. Base Value is the level-1 amount. Each later upgrade adds Per Level once (0.25, 0.50, 0.75...). Growth is additive, up to level 99. English card text is generated from these values. Max-level abilities cannot be offered again. Rarity probabilities and item weights stay in SurvivorUpgradeData.",HelpBoxMessageType.Info));
        var descriptions=new List<(Label label,SurvivorUpgradeId id)>();
        foreach(string groupName in new[]{"common","uncommon","rare","epic"}){
            var groupProperty=serializedObject.FindProperty(groupName);
            var groupValue=typeof(SurvivorAbilityData).GetField(groupName).GetValue(data);
            var fold=new Foldout{text=ObjectNames.NicifyVariableName(groupName),value=groupName=="common"};root.Add(fold);
            foreach(var field in groupValue.GetType().GetFields()){
                var value=field.GetValue(groupValue);SurvivorUpgradeId id=SurvivorUpgradeId.Count;
                foreach(SurvivorUpgradeId candidate in Enum.GetValues(typeof(SurvivorUpgradeId)))if(ReferenceEquals(data.Find(candidate),value)){id=candidate;break;}
                var property=groupProperty.FindPropertyRelative(field.Name);property.isExpanded=true;
                fold.Add(new PropertyField(property,ObjectNames.NicifyVariableName(field.Name)));
                var label=new Label(data.Description(id));label.style.whiteSpace=WhiteSpace.Normal;label.style.marginLeft=16;label.style.marginBottom=14;fold.Add(label);descriptions.Add((label,id));
            }
        }
        root.TrackSerializedObjectValue(serializedObject,_=>{foreach(var entry in descriptions)entry.label.text=data.Description(entry.id);});
        return root;
    }
}
