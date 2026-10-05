using System.Collections.Generic;
using MiningSimulator.Ores;
using StarterAssets;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// Keep legacy serialized values for unmigrated actors and monsters. Once migrated,
// only GameData is exposed as the authoring source for these numbers.
public abstract class MiningPlayerStatsBoundEditor : Editor
{
    protected abstract string[] StatFields { get; }
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        bool bound = true;
        foreach (Object obj in targets)
            bound &= obj is Component component && MiningPlayerStats.For(component) != null;
        if (bound) root.Add(new HelpBox("Player stats are edited in GameData/Player/PlayerStatsData. Select the Player Stats Data asset on Mining Player Stats.", HelpBoxMessageType.Info));
        var hidden = new HashSet<string>(bound ? StatFields : System.Array.Empty<string>());
        var property = serializedObject.GetIterator();
        bool enter = true;
        while (property.NextVisible(enter))
        {
            enter = false;
            if (hidden.Contains(property.name)) continue;
            var field = new PropertyField(property.Copy());
            if (property.name == "m_Script") field.SetEnabled(false);
            root.Add(field);
        }
        return root;
    }
}
[CustomEditor(typeof(MiningCharacterHealth)), CanEditMultipleObjects]
public sealed class MiningPlayerHealthStatsEditor : MiningPlayerStatsBoundEditor
{ protected override string[] StatFields => new[] { "maxHealth", "regenAmount", "regenInterval", "armor", "magicResistance", "resistanceScale" }; }
[CustomEditor(typeof(PlayerCombatInput)), CanEditMultipleObjects]
public sealed class MiningPlayerCombatStatsEditor : MiningPlayerStatsBoundEditor
{ protected override string[] StatFields => new[] { "damage", "attackRange", "attackAngle", "attackSpeed", "combatBlendSeconds", "hitOriginOffset" }; }
[CustomEditor(typeof(ThirdPersonController)), CanEditMultipleObjects]
public sealed class MiningPlayerMovementStatsEditor : MiningPlayerStatsBoundEditor
{ protected override string[] StatFields => new[] { "MoveSpeed", "SprintSpeed", "RotationSmoothTime", "SpeedChangeRate", "JumpHeight", "Gravity", "JumpTimeout", "FallTimeout" }; }
[CustomEditor(typeof(PlayerDeathRespawn)), CanEditMultipleObjects]
public sealed class MiningPlayerRespawnStatsEditor : MiningPlayerStatsBoundEditor
{ protected override string[] StatFields => new[] { "respawnSeconds" }; }
