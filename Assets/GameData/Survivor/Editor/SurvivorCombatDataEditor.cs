using GameData.Player.Script;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(SurvivorCombatData))]
public sealed class SurvivorCombatDataEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root=new VisualElement();
        root.Add(new HelpBox("Survivor: one source for base stats. Cards, equipment and buffs add bonuses at runtime.",HelpBoxMessageType.Info));
        void Group(string label,bool open,params string[] fields){
            var fold=new Foldout{text=label,value=open,viewDataKey=label};root.Add(fold);
            foreach(var name in fields){var p=serializedObject.FindProperty(name);if(p!=null)fold.Add(new PropertyField(p));}
        }
        Group("Attack — damage & speed",true,"damage","attackSpeed","attackSpeedRatio","baseBonusAttackSpeedPercent","maximumAttackSpeed","range","projectileSpeed","projectileRadius","abilities");
        root.Add(new HelpBox("Attacks/s = Base Attack Speed + Attack Speed Ratio × (total bonus % / 100). Bonuses add together. Time between attacks = 1 / Attacks/s.",HelpBoxMessageType.Info));
        var preview=new Label();root.Add(preview);
        void Refresh(){if(!target)return;var data=(SurvivorCombatData)target;float speed=data.AttacksPerSecond();preview.text=$"Base preview: {speed:0.###} attacks/s  |  {1f/speed:0.###} seconds/attack";}
        Refresh();root.schedule.Execute(Refresh).Every(250);
        Group("Health & defenses",false,"maxHealth","regenAmount","regenInterval","armor","armorPerLevel","magicResistance","magicResistancePerLevel","resistanceScale");
        Group("Movement & jump",false,"walkSpeed","jumpStrength");
        root.Add(new HelpBox("Sprint speed always equals base movement speed + 3 m/s.",HelpBoxMessageType.None));
        Group("Stamina",false,"maxStamina","runningCostPerSecond","staminaRecoveryPerSecond","staminaRecoveryDelay","sprintRecoveryThreshold");
        Group("Experience & levels",false,"startingLevel","startingExperience","experienceRequired","experienceRequirementGrowth");
        Group("Projectile models",false,"projectileAppearance","normalProjectilePrefab","iceProjectilePrefab","fireProjectilePrefab","toxicProjectilePrefab","chargedCubePrefab");
        Group("Projectile feedback — visuals only",false,"projectileTrailSeconds","projectileTrailWidth","impactFlashSeconds","impactFlashSize","projectileColor");
        Group("Advanced — pooling & collision",false,"maximumProjectilesPerVolley","poolCapacity","hitLayers");
        return root;
    }
}
