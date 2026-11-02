using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GodBossData))]
public sealed class GodBossDataEditor : Editor
{
    public override void OnInspectorGUI(){
        serializedObject.Update();var data=(GodBossData)target;
        EditorGUILayout.LabelField("GOD BOSS",EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"{data.health:0.##} HP | Melee {data.meleeRange:0.##} m | Jump {data.jumpRange:0.##} m");
        if(data.prefab&&GUILayout.Button("Preview attack ranges in Scene"))MonsterDataPreview.Open(data.prefab.gameObject,data,Mathf.Max(data.meleeRange,data.jumpRange));
        if(MonsterSpeciesInspector.Group(serializedObject,"Health, defenses & movement",true))Fields("health","armor","magicResistance","height","walkSpeed","runSpeed","startRunningDistance","stopRunningDistance","turnSpeed","crowdControlResistancePercent");
        if(MonsterSpeciesInspector.Group(serializedObject,"Melee — punch & swipe",true))Fields("meleeRange","attackHalfAngle","recovery","punch","swipe");
        if(MonsterSpeciesInspector.Group(serializedObject,"Jump & landing shockwave"))Fields("jumpRange","jumpCooldown","jumpWindup","jumpSpeed","jumpTravelSeconds","jumpHeight","jumpDamage","jumpKnockback","waveRadius","waveSpeed","waveThickness");
        if(MonsterSpeciesInspector.Group(serializedObject,"Roar, healing & summons"))Fields("roarCheckSeconds","roarCooldown","roarSpeed","roarChance","healMaxHealthFraction","summonHealthAtZeroBossHP","summonDamageAtZeroBossHP","summonBossVariants","maxLivingSummons","summonMinimumDistance","summonMaximumDistance","summonMinimumPlayerDistance","bleedMaxHealthPercentPerSecond","bleedDuration");
        if(MonsterSpeciesInspector.Group(serializedObject,"Daily spawn & later appearances"))Fields("prefab","minimumPlayerLevel","initialChancePercent","dailyChanceIncreasePercent","healthIncreasePercent","damageIncreasePercent","spawnDistance");
        if(MonsterSpeciesInspector.Group(serializedObject,"Animation clips"))Fields("idle","walk","run","punchClip","swipeClip","jump","roar","dying","crossfade","deathSpeed");
        if(serializedObject.ApplyModifiedProperties())SceneView.RepaintAll();
    }
    private void Fields(params string[] names)=>MonsterSpeciesInspector.Fields(serializedObject,names);
    [DrawGizmo(GizmoType.Selected|GizmoType.NonSelected)]
    private static void Ranges(GodBossController actor,GizmoType type){
        var data=actor.Data;if(!data||((type&GizmoType.Selected)==0&&Selection.activeObject!=data))return;
        var p=actor.transform.position+Vector3.up*.04f;var before=Handles.color;Handles.color=new Color(1,.55f,.05f);Handles.DrawWireDisc(p,Vector3.up,data.meleeRange,3);Handles.Label(p+actor.transform.right*data.meleeRange,$"Melee {data.meleeRange:0.##} m");
        Handles.color=Color.cyan;Handles.DrawWireDisc(p,Vector3.up,data.jumpRange,3);Handles.Label(p+actor.transform.forward*data.jumpRange,$"Jump {data.jumpRange:0.##} m");Handles.color=before;
    }
}
