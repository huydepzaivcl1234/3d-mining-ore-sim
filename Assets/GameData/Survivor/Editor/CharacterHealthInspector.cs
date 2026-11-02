using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using MiningSimulator.Ores;

[CustomEditor(typeof(CharacterHealth))]
public sealed class CharacterHealthInspector : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root=new VisualElement();
        var health=(CharacterHealth)target;
        var stats=health.GetComponent<PlayerStats>();
        bool linked=stats&&stats.CoreData;
        if(linked){
            root.Add(new HelpBox("Player health, defenses and regeneration come from Player Stats → Core Data. The values below include active bonuses.",HelpBoxMessageType.Info));
            root.Add(new Button(()=>{Selection.activeObject=stats.CoreData;EditorGUIUtility.PingObject(stats.CoreData);}){text="Edit Survivor Stats"});
            var totals=new Label();root.Add(totals);
            void Refresh(){if(!health)return;totals.text=$"Health: {health.Health:0.##} / {health.MaxHealth:0.##}\nArmor: {health.Armor:0.##}   Magic resistance: {health.MagicResistance:0.##}\nRegeneration: {health.EffectiveRegenAmount:0.##} HP / {health.RegenInterval:0.##} s";}
            Refresh();root.schedule.Execute(Refresh).Every(200);
        }
        var property=serializedObject.GetIterator();bool enter=true;
        while(property.NextVisible(enter)){
            enter=false;
            if(linked&&(property.name=="maxHealth"||property.name=="armor"||property.name=="magicResistance"||property.name=="resistanceScale"||property.name=="regenAmount"||property.name=="regenInterval"))continue;
            var field=new PropertyField(property.Copy());if(property.name=="m_Script")field.SetEnabled(false);root.Add(field);
        }
        return root;
    }
}
