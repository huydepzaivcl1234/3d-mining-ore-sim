using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(PotionMonsterData))]
public sealed class PotionMonsterDataEditor:Editor
{
    public override void OnInspectorGUI(){
        serializedObject.Update();EditorGUILayout.LabelField("POTION MONSTER — RANGED ONLY",EditorStyles.boldLabel);
        MonsterSpeciesInspector.Fields(serializedObject,"minimumSpawnDay","combat.maxHealth","armor","magicResistance","combat.moveSpeed","combat.detectionRange","combat.rangedRange","combat.attackSpeed","combat.attackCooldown","combat.fireState","combat.releaseMoment");
        EditorGUILayout.Space();EditorGUILayout.HelpBox("Each throw rolls Green / Red / Yellow independently, each with a 1 in 3 chance. Green targets a wounded ally, or the nearest ally if all are healthy.",MessageType.Info);EditorGUILayout.LabelField("Throw and potion models",EditorStyles.boldLabel);MonsterSpeciesInspector.Fields(serializedObject,"greenPotion","redPotion","yellowPotion","flightSeconds","arcHeight","potionSize");
        EditorGUILayout.Space();EditorGUILayout.LabelField("Green — heal wounded monsters",EditorStyles.boldLabel);MonsterSpeciesInspector.Fields(serializedObject,"healingRange","healingFlat","healingMaxHealthPercent");
        EditorGUILayout.Space();EditorGUILayout.LabelField("Red — true damage every second",EditorStyles.boldLabel);MonsterSpeciesInspector.Fields(serializedObject,"redFlatDamage","redPlayerMaxHealthPercent","burnDuration","burnTickSeconds");
        EditorGUILayout.Space();EditorGUILayout.LabelField("Yellow — magic damage / attack-speed slow",EditorStyles.boldLabel);MonsterSpeciesInspector.Fields(serializedObject,"abilityPower","abilityPowerPerLevel","yellowFlatDamage","yellowAbilityPowerPercent","attackSpeedSlowPercent","slowDuration");
        if(MonsterSpeciesInspector.Group(serializedObject,"Level scaling"))MonsterSpeciesInspector.DrawLevels(serializedObject);
        if(MonsterSpeciesInspector.Group(serializedObject,"Rewards and drops"))MonsterLootInspector.DrawSpecies(serializedObject);
        serializedObject.ApplyModifiedProperties();
    }
}
