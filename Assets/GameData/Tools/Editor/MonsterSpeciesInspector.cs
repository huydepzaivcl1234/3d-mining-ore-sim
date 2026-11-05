using System;
using System.Collections.Generic;
using System.Linq;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

public static class MonsterSpeciesInspector
{
    [MenuItem("Assets/Create/Survivor/Monsters/New Monster Data",false,100)]
    public static void CreateMonsterData(){
        var data=ScriptableObject.CreateInstance<MonsterRewardData>();data.combatSettingsVersion=1;
        ProjectWindowUtil.CreateAsset(data,"New Monster Data.asset");
    }
    private static readonly Dictionary<string,bool> groups=new();
    public static void Draw(SerializedObject so)
    {
        so.Update();if(((MonsterRewardData)so.targetObject).lootProfileOnly){MonsterLootInspector.DrawSpecies(so);so.ApplyModifiedProperties();return;}Fields(so,"minimumSpawnDay");EditorGUILayout.Space(5);var data=(MonsterRewardData)so.targetObject;
        EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(data.GetType().Name.Replace("Data","")),EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{data.combat.maxHealth:0.##} HP | {data.combat.damage:0.##} {data.attackDamageType} damage | Melee {data.combat.attackRange:0.##} m");
        if(GUILayout.Button("Preview attack ranges in Scene")){
            var prefab=FindPrefab(data);if(prefab)MonsterDataPreview.Open(prefab,data,Mathf.Max(data.combat.attackRange,HasRanged(data)?data.combat.rangedRange:data is SkullclawData skull?skull.jumpRange:0));
        }
        EditorGUILayout.HelpBox("This asset owns this monster's settings. Other species' abilities and unused projectile settings are omitted.",MessageType.None);
        if(Group(so,"Core stats",true))Fields(so,"combat.maxHealth","combat.regenAmount","combat.regenInterval","combat.damage","attackDamageType","combat.attackSpeed","combat.attackCooldown","armor","magicResistance");
        if(!data.combat.rangedOnly&&Group(so,"Melee attack",true)){
            Fields(so,"combat.attackRange","combat.attackArc","combat.hitHeight","combat.hitShape");
            if(data is SkullclawData)Fields(so,"rightContact","leftContact");else Fields(so,"combat.hitMoment");
            if(so.FindProperty("combat.hitShape").enumValueIndex==(int)MushroomMonster.HitShape.Area)Fields(so,"combat.areaRadius","combat.areaForwardOffset","combat.areaSideOffset");
            if(!(data is SkullclawData)){
                Fields(so,"combat.secondAttackState");
                if(!string.IsNullOrWhiteSpace(so.FindProperty("combat.secondAttackState").stringValue))Fields(so,"combat.secondHitMoment","combat.secondAreaRadius","combat.secondAreaOffset");
            }
        }
        if((HasRanged(data)||data.GetType()==typeof(MonsterRewardData))&&Group(so,HasRanged(data)?"Ranged attack":"Ranged attack (optional)",HasRanged(data)))Fields(so,"combat.rangedRange",data is PotionMonsterData?"combat.fireState":"combat.projectile","combat.releaseMoment","combat.projectileSpeed","combat.projectileTurnDegreesPerSecond","combat.projectileRadius","combat.projectileLifetime","combat.projectileCollisionLayers");
        var own=OwnFields(data.GetType());
        if(own.Length>0&&Group(so,"Species abilities & animations",true))foreach(var name in own){if(data is SkullclawData&&(name=="rightContact"||name=="leftContact"))continue;Fields(so,name);}
        if(Group(so,"Movement & navigation"))Fields(so,"combat.moveSpeed","combat.detectionRange","combat.chaseTurnSpeed","combat.targetScanSeconds","combat.targetMemorySeconds","combat.retaliationForgetSeconds","combat.detectionHysteresis","combat.chaseRepathSeconds","combat.chaseStandDirections","combat.chaseStandRangeFraction","combat.chaseStandMaximumRangeFraction","combat.targetRepathDistance","combat.waypointTolerance","combat.avoidanceLookAhead","combat.avoidanceHoldSeconds","combat.stuckRepathSeconds","combat.pathRequestTimeout","combat.navigationObstacleLayers");
        if(Group(so,"Spawn levels & stat growth"))DrawLevels(so);
        if(Group(so,"Boss variant"))DrawBoss(so);
        if(Group(so,"Rewards & drops"))MonsterLootInspector.DrawSpecies(so);
        if(Group(so,"Advanced — animation, defenses & lifetime")){
            if(!(data is SkullclawData))Fields(so,"combat.attackState");
            if(HasRanged(data))Fields(so,"combat.fireState");
            Fields(so,"combat.animationBlendSeconds","combat.attackTurnSpeed","combat.trackingEndFraction","combat.knockbackSpeed","combat.knockbackLift","combat.warningColor","combat.warningShader","combat.deathDelay","armorPerLevel","magicResistancePerLevel","resistanceScale","combatSeconds","dissolveSeconds","dissolveColor","dissolveShader");
            if(data.burnDamagePerTick>0||data.burnDurationSeconds>0)Fields(so,"burnDamagePerTick","burnTickSeconds","burnDurationSeconds");
            if(data.lifeStealPercent>0)Fields(so,"lifeStealPercent");
            if(data.healingBonusPercent>0)Fields(so,"healingBonusPercent");
        }
        if(so.ApplyModifiedProperties())SceneView.RepaintAll();
    }
    public static bool HasRanged(MonsterRewardData data)=>data is PotionMonsterData||data is MushranonData||data.combat.projectile;
    private static string[] OwnFields(Type type)=>type.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance).Where(f=>f.DeclaringType!=typeof(MonsterRewardData)).Select(f=>f.Name).ToArray();
    public static bool Group(SerializedObject so,string title,bool initial=false){string key=so.targetObject.GetType().FullName+"/"+title;if(!groups.TryGetValue(key,out bool open))open=initial;EditorGUILayout.Space(4);open=EditorGUILayout.Foldout(open,title,true,EditorStyles.foldoutHeader);groups[key]=open;return open;}
    public static void Fields(SerializedObject so,params string[] names){foreach(var name in names){var p=so.FindProperty(name);if(p!=null)EditorGUILayout.PropertyField(p,true);}}
    public static void DrawLevels(SerializedObject so){
        Fields(so,"usePlayerRelativeLevels");
        if(so.FindProperty("usePlayerRelativeLevels").boolValue)Fields(so,"minimumLevelsBelowPlayer","maximumLevelsBelowPlayer","maximumLevelsAbovePlayer","weakerLevelWeight","equalLevelWeight","strongerLevelWeight");
        else Fields(so,"minimumLevel","addPlayerLevelAtSpawn","playerLevelContribution","higherLevelChance","higherLevelIncreasePerDayPercent","maximumHigherLevelChancePercent");
        Fields(so,"statGrowthPerLevel","randomStatMultiplier");
    }
    public static void DrawBoss(SerializedObject so)
    {
        Fields(so,"boss.enabled");if(!so.FindProperty("boss.enabled").boolValue)return;
        Fields(so,"boss.chancePercent","boss.minimumPlayerLevel","boss.sizeIncreasePercent","boss.healthMultiplier","boss.damageMultiplier","boss.goldMultiplier","boss.experienceMultiplier","boss.rewardOverride","boss.effectColor");
        var data=(MonsterRewardData)so.targetObject;
        BossSkillKind supported=data is GolemData?BossSkillKind.GolemSlow:data is BatData?BossSkillKind.BatHaste:data is MushroomData?BossSkillKind.MushroomHealing:BossSkillKind.None;
        var skill=so.FindProperty("boss.skill");var allowed=new List<BossSkillKind>{BossSkillKind.None};if(supported!=BossSkillKind.None)allowed.Add(supported);var current=(BossSkillKind)skill.intValue;if(!allowed.Contains(current))allowed.Add(current);
        if(allowed.Count>1)skill.intValue=EditorGUILayout.IntPopup("Boss ability",skill.intValue,allowed.Select(x=>ObjectNames.NicifyVariableName(x.ToString())).ToArray(),allowed.Select(x=>(int)x).ToArray());
        switch((BossSkillKind)skill.intValue){
            case BossSkillKind.MushroomHealing:Fields(so,"boss.healTriggerHealthPercent","boss.healMaxHealthPercent","boss.healSeconds");break;
            case BossSkillKind.GolemSlow:Fields(so,"boss.slowEveryStrikes","boss.slowRadius","boss.slowPercent","boss.slowSeconds");break;
            case BossSkillKind.BatHaste:Fields(so,"boss.hasteDamageThresholdPercent","boss.hasteAttackSpeedPercent","boss.hasteStacks");break;
        }
    }
    private static GameObject FindPrefab(MonsterRewardData data){
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Monsters","Assets/GameData/Monsters"})){
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));var actor=prefab.GetComponent<MushroomMonster>();if(actor&&actor.SpeciesData==data)return prefab;
        }return null;
    }
    [DrawGizmo(GizmoType.Selected|GizmoType.NonSelected)]
    private static void Ranges(MushroomMonster actor,GizmoType type){
        var data=actor.SpeciesData;if(!data||data is BreadMonsterData)return;
        if((type&GizmoType.Selected)==0&&Selection.activeObject!=data)return;
        var t=actor.transform;float scale=Mathf.Max(.01f,Mathf.Max(t.lossyScale.x,t.lossyScale.z));var p=t.position+Vector3.up*.04f;Color previous=Handles.color;
        Handles.color=new Color(1,.55f,.05f);Handles.DrawWireDisc(p,Vector3.up,actor.EffectiveAttackRange,3);Handles.Label(p+t.right*actor.EffectiveAttackRange,$"Melee {actor.EffectiveAttackRange:0.##} m");
        var first=Quaternion.AngleAxis(-data.combat.attackArc*.5f,Vector3.up)*t.forward;Handles.DrawWireArc(p,Vector3.up,first,data.combat.attackArc,actor.EffectiveAttackRange,4);
        if(data.combat.hitShape==MushroomMonster.HitShape.Area){Handles.color=Color.red;Handles.DrawWireDisc(p+(t.forward*data.combat.areaForwardOffset+t.right*data.combat.areaSideOffset)*scale,Vector3.up,data.combat.areaRadius*scale,2);}
        if(HasRanged(data)){float range=actor.GetComponent<IMonsterRangedAttack>()?.Range??data.combat.rangedRange*scale;Handles.color=Color.cyan;Handles.DrawWireDisc(p,Vector3.up,range,3);Handles.Label(p+t.forward*range,$"Ranged {range:0.##} m");}
        if(data is SkullclawData skull){Handles.color=new Color(.7f,.4f,1);Handles.DrawWireDisc(p,Vector3.up,skull.jumpRange*scale,2);Handles.Label(p+t.forward*skull.jumpRange*scale,$"Jump approach {skull.jumpRange*scale:0.##} m");}
        Handles.color=previous;
    }
}

public static class MonsterDataPreview
{
    public static void Open(GameObject prefab,UnityEngine.Object data,float radius){
        AssetDatabase.OpenAsset(prefab);double readyAt=EditorApplication.timeSinceStartup+.3,deadline=readyAt+5;EditorApplication.CallbackFunction ready=null;
        ready=()=>{if(EditorApplication.timeSinceStartup>deadline){EditorApplication.update-=ready;return;}if(EditorApplication.timeSinceStartup<readyAt)return;
            var stage=UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();if(!stage)return;EditorApplication.update-=ready;
            Selection.activeObject=data;var view=SceneView.lastActiveSceneView;if(!view)view=EditorWindow.GetWindow<SceneView>();view.drawGizmos=true;view.LookAt(stage.prefabContentsRoot.transform.position,Quaternion.Euler(70,0,0),Mathf.Max(4,radius*1.15f),false,true);view.Repaint();};
        EditorApplication.update+=ready;
    }
}
