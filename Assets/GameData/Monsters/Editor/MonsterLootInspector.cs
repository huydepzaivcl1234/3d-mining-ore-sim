using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// Reused by the existing species IMGUI inspectors.
public static class MonsterLootInspector
{
    public static void DrawSpecies(SerializedObject so)
    {
        var settings=MonsterLootSettings.Current;
        bool shared=settings&&settings.shared==so.targetObject;
        if(settings&&!shared&&(settings.dropMode==MonsterDataMode.AllMonsters||settings.rewardMode==MonsterDataMode.AllMonsters)){
            EditorGUILayout.HelpBox("Shared channels use Monster Loot Settings. Individual values below are kept for Per Species mode.",MessageType.Info);
            if(GUILayout.Button("Open Monster Loot Settings"))Selection.activeObject=settings;
        }
        EditorGUILayout.LabelField("Gold & experience",EditorStyles.boldLabel);
        using(new EditorGUI.DisabledScope(settings&&!shared&&settings.rewardMode==MonsterDataMode.AllMonsters))
            {
                MonsterSpeciesInspector.Fields(so,"gold","experience","goldGrowthPerLevel");
                if(((MonsterRewardData)so.targetObject).lootProfileOnly)MonsterSpeciesInspector.Fields(so,"boss.goldMultiplier","boss.experienceMultiplier");
            }
        EditorGUILayout.LabelField("Card & item drops",EditorStyles.boldLabel);
        using(new EditorGUI.DisabledScope(settings&&!shared&&settings.dropMode==MonsterDataMode.AllMonsters)){
            MonsterSpeciesInspector.Fields(so,"cardDropPercent","bossCardDropPercent","abilityCardSharePercent","drops");
            if(MonsterSpeciesInspector.Group(so,"Pickup motion"))MonsterSpeciesInspector.Fields(so,"iconSize","launchUpSpeed","launchOutSpeed","groundWaitSeconds","attractionRadius","attractionSpeed","attractionAcceleration");
        }
    }
}

[CustomEditor(typeof(MonsterLootSettings))]
public sealed class MonsterLootSettingsEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root=new VisualElement();
        root.Add(new HelpBox("Choose each channel independently. Per Species keeps each monster's own values. All Monsters uses the shared profile, including GPU crowds and God Boss.",HelpBoxMessageType.Info));
        root.Add(new PropertyField(serializedObject.FindProperty("dropMode"),"Drop Mode"));
        root.Add(new PropertyField(serializedObject.FindProperty("rewardMode"),"Reward Mode"));
        root.Add(new PropertyField(serializedObject.FindProperty("shared"),"Shared Loot & Rewards"));
        var sharedView=new VisualElement();root.Add(sharedView);
        var select=new ObjectField("Individual species / boss profile"){objectType=typeof(MonsterRewardData),allowSceneObjects=false};root.Add(select);
        var speciesView=new VisualElement();root.Add(speciesView);
        void DrawProfile(VisualElement host,MonsterRewardData data,bool common){
            host.Clear();if(!data){host.Add(new HelpBox("Assign a loot/reward profile.",HelpBoxMessageType.Warning));return;}
            var source=new SerializedObject(data);var settings=(MonsterLootSettings)target;
            var title=new Label(common?"SHARED PROFILE":"INDIVIDUAL PROFILE");title.style.unityFontStyleAndWeight=FontStyle.Bold;title.style.marginTop=10;host.Add(title);
            void Group(string label,bool enabled,params string[] paths){
                var fold=new Foldout{text=label,value=true};host.Add(fold);fold.SetEnabled(enabled);
                foreach(var path in paths){var p=source.FindProperty(path);if(p!=null)fold.Add(new PropertyField(p));}
            }
            Group("Rewards — gold & XP",common||settings.rewardMode==MonsterDataMode.PerSpecies,"gold","experience","goldGrowthPerLevel","boss.goldMultiplier","boss.experienceMultiplier");
            Group("Drops — cards & items",common||settings.dropMode==MonsterDataMode.PerSpecies,"cardDropPercent","bossCardDropPercent","abilityCardSharePercent","drops");
            var motion=new Foldout{text="Pickup motion",value=false};host.Add(motion);
            foreach(var path in new[]{"iconSize","launchUpSpeed","launchOutSpeed","groundWaitSeconds","attractionRadius","attractionSpeed","attractionAcceleration"}){var p=source.FindProperty(path);if(p!=null)motion.Add(new PropertyField(p));}
            motion.SetEnabled(common||settings.dropMode==MonsterDataMode.PerSpecies);
            host.Add(new Button(()=>{Selection.activeObject=data;EditorGUIUtility.PingObject(data);}){text="Open profile asset"});
            host.Bind(source);
        }
        void Refresh(){if(!target)return;DrawProfile(sharedView,((MonsterLootSettings)target).shared,true);DrawProfile(speciesView,select.value as MonsterRewardData,false);}
        select.RegisterValueChangedCallback(_=>Refresh());
        root.TrackPropertyValue(serializedObject.FindProperty("dropMode"),_=>Refresh());
        root.TrackPropertyValue(serializedObject.FindProperty("rewardMode"),_=>Refresh());
        root.TrackPropertyValue(serializedObject.FindProperty("shared"),_=>Refresh());
        root.Add(new HelpBox("Card chance = chance of one card. Ability Card Share divides successful cards into blue abilities vs red stat cards. Every item entry rolls independently. Gold level growth is applied once; boss multipliers are applied once.",HelpBoxMessageType.Info));
        Refresh();return root;
    }
    [MenuItem("Survivor/Game Data/Monster Loot and Rewards")]
    private static void SelectSettings(){Selection.activeObject=MonsterLootSettings.Current;}
}
