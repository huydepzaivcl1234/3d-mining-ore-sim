using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

// Scoped view of the existing monster Inspector: Bread never shows other species' loadouts.
public static class BreadMonsterInspector
{
    private static bool stats, melee=true, ranged=true, heal, sugar, levels, advanced, boss;
    private static bool healGizmo;
    public static void Draw(SerializedObject so)
    {
        so.Update();
        Field(so,"minimumSpawnDay","First day this species can spawn");
        var data=(BreadMonsterData)so.targetObject;
        EditorGUILayout.LabelField("BREAD MONSTER", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{data.combat.maxHealth:0.##} HP   |   {data.combat.damage:0.##} magic damage");
        EditorGUILayout.LabelField($"Melee: {data.combat.attackRange:0.##} m   |   Throw: {data.combat.attackRange:0.##}–{data.combat.rangedRange:0.##} m");
        EditorGUILayout.Space(5);
        if(GUILayout.Button("Preview attack ranges in Scene")) Preview(data);
        healGizmo=EditorGUILayout.Toggle("Also show healing radius",healGizmo);
        EditorGUILayout.HelpBox("Orange = melee range + swing arc. Cyan = maximum throw range. Ranges scale with the scene monster. Keep Scene Gizmos enabled.",MessageType.None);
        if(Group(ref stats,"Health, movement & magic damage")){
            Fields(so,"combat.maxHealth","combat.damage","combat.attackSpeed","combat.attackCooldown","combat.moveSpeed","combat.detectionRange","armor","magicResistance");
        }
        if(Group(ref melee,"Melee — slice swing")){
            Field(so,"combat.attackRange","Melee range (m)");
            Field(so,"combat.attackArc","Swing arc (degrees)");
            Field(so,"combat.hitHeight","Hit height (m)");
            Field(so,"combat.hitMoment","Contact time (0–1)");
        }
        if(Group(ref ranged,"Ranged — thrown bread slice")){
            Field(so,"combat.rangedRange","Maximum throw range (m)");
            Field(so,"sliceProjectile","Bread slice prefab");
            Field(so,"flightSeconds","Flight duration (s)");
            Field(so,"arcHeight","Arc height (m)");
            Field(so,"arcSideways","Sideways curve (m)");
            Field(so,"combat.releaseMoment","Release time (0–1)");
            Field(so,"combat.projectileRadius","Collision radius (m)");
        }
        if(Group(ref heal,"Ability — heal nearby monsters")){
            Fields(so,"healingRadius","healingInterval","healingFlat","healingPerLevel","healingMaxHealthPercent","healSelf","healingFlightSeconds");
            EditorGUILayout.HelpBox($"Every {data.healingInterval:0.##} s: {data.healingFlat:0.##} + {data.healingPerLevel:0.##} per extra monster level + {data.healingMaxHealthPercent:0.##}% of ally max HP. Does not revive.",MessageType.None);
        }
        if(Group(ref sugar,"On projectile hit — Hyperglycemia")){
            Fields(so,"sugarMaxHealthPercent","sugarDamagePerMonsterLevel","sugarDuration","sugarTickSeconds");
            EditorGUILayout.HelpBox($"Each tick: {data.sugarMaxHealthPercent:0.##}% player max HP + {data.sugarDamagePerMonsterLevel:0.##} × Bread level, as magic damage. Refreshes one effect; does not stack copies.",MessageType.None);
        }
        if(Group(ref boss,"Boss variant"))MonsterSpeciesInspector.DrawBoss(so);
        if(Group(ref levels,"Levels, defenses & rewards")){
            Fields(so,"usePlayerRelativeLevels","minimumLevelsBelowPlayer","maximumLevelsBelowPlayer","maximumLevelsAbovePlayer","weakerLevelWeight","equalLevelWeight","strongerLevelWeight");
            if(!so.FindProperty("usePlayerRelativeLevels").boolValue)Fields(so,"minimumLevel","addPlayerLevelAtSpawn","playerLevelContribution","higherLevelChance","higherLevelIncreasePerDayPercent","maximumHigherLevelChancePercent");
            Fields(so,"statGrowthPerLevel","randomStatMultiplier","armorPerLevel","magicResistancePerLevel","resistanceScale");
        }
        MonsterLootInspector.DrawSpecies(so);
        if(Group(ref advanced,"Advanced — animation, navigation & lifetime")){
            Fields(so,"combat.attackState","combat.fireState","combat.animationBlendSeconds","combat.attackTurnSpeed","combat.trackingEndFraction","combat.chaseTurnSpeed","combat.targetScanSeconds","combat.targetMemorySeconds","combat.detectionHysteresis","combat.chaseRepathSeconds","combat.targetRepathDistance","combat.waypointTolerance","combat.avoidanceLookAhead","combat.avoidanceHoldSeconds","combat.stuckRepathSeconds","combat.pathRequestTimeout","combat.navigationObstacleLayers","combat.projectileCollisionLayers","spinDegreesPerSecond","combat.deathDelay","combatSeconds","dissolveSeconds","dissolveColor","dissolveShader");
        }
        if(data.combat.rangedRange<data.combat.attackRange)EditorGUILayout.HelpBox("Throw range must exceed melee range to allow ranged attacks.",MessageType.Warning);
        if(so.ApplyModifiedProperties())SceneView.RepaintAll();
    }
    private static bool Group(ref bool open,string title){EditorGUILayout.Space(4);open=EditorGUILayout.Foldout(open,title,true,EditorStyles.foldoutHeader);return open;}
    private static void Fields(SerializedObject so,params string[] names){foreach(var name in names)Field(so,name);}
    private static void Field(SerializedObject so,string name,string label=null){
        var p=so.FindProperty(name);if(p!=null)EditorGUILayout.PropertyField(p,new GUIContent(label??ObjectNames.NicifyVariableName(name.Substring(name.LastIndexOf('.')+1))),true);
    }
    public static void Preview(BreadMonsterData data)
    {
        foreach(var actor in Object.FindObjectsByType<MushroomMonster>())
            if(actor.SpeciesData==data){Focus(actor);return;}
        string folder=System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(data)).Replace('\\','/');
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/BreadMonster.prefab");
        if(!prefab)return;
        AssetDatabase.OpenAsset(prefab);
        double readyAt=EditorApplication.timeSinceStartup+.3,deadline=readyAt+5;
        EditorApplication.CallbackFunction ready=null;
        ready=()=>{
            if(EditorApplication.timeSinceStartup>deadline){EditorApplication.update-=ready;return;}
            if(EditorApplication.timeSinceStartup<readyAt)return;
            var stage=UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if(stage&&stage.prefabContentsRoot.TryGetComponent<MushroomMonster>(out var actor)){
                EditorApplication.update-=ready;Focus(actor);
            }
        };
        EditorApplication.update+=ready;
    }
    private static void Focus(MushroomMonster actor){
        Selection.activeObject=actor.SpeciesData;
        var view=SceneView.lastActiveSceneView;if(!view)view=EditorWindow.GetWindow<SceneView>();
        view.drawGizmos=true;view.LookAt(actor.transform.position,Quaternion.Euler(75,0,0),Mathf.Max(4,actor.CombatData.rangedRange*actor.transform.lossyScale.x*1.15f),false,true);view.Repaint();
    }
    [DrawGizmo(GizmoType.Selected|GizmoType.Active|GizmoType.NonSelected)]
    private static void DrawRanges(BreadMonsterAbility bread,GizmoType type)
    {
        var data=bread.Data;if(!data)return;
        if((type&GizmoType.Selected)==0&&Selection.activeObject!=data)return;
        var actor=bread.GetComponent<MushroomMonster>();if(!actor)return;
        var transform=actor.transform;Vector3 center=transform.position+Vector3.up*.04f;
        float close=actor.EffectiveAttackRange, far=bread.Range;
        Color before=Handles.color;
        Handles.color=new Color(1,.55f,.05f);Handles.DrawWireDisc(center,Vector3.up,close,3);
        Vector3 first=Quaternion.AngleAxis(-data.combat.attackArc*.5f,Vector3.up)*transform.forward;
        Handles.DrawWireArc(center,Vector3.up,first,data.combat.attackArc,close,5);
        Handles.DrawLine(center,center+first*close,2);Handles.DrawLine(center,center+Quaternion.AngleAxis(data.combat.attackArc,Vector3.up)*first*close,2);
        Handles.Label(center+transform.right*(close+.15f),$"Melee {close:0.##} m / {data.combat.attackArc:0.#}°");
        Handles.color=Color.cyan;Handles.DrawWireDisc(center,Vector3.up,far,3);
        Handles.Label(center+transform.forward*far,$"Throw {close:0.##}–{far:0.##} m");
        if(healGizmo){Handles.color=Color.green;Handles.DrawWireDisc(center,Vector3.up,data.healingRadius,2);Handles.Label(center-transform.right*data.healingRadius,$"Heal {data.healingRadius:0.##} m");}
        Handles.color=before;
    }
}
