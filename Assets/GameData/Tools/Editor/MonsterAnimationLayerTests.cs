#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class MonsterAnimationLayerTests
{
    [TestCase("Bat", "Assets/Prefabs/Monsters/BatMonster.prefab", true)]
    [TestCase("ForestGolem", "Assets/Prefabs/Monsters/ForestGolem.prefab", true)]
    [TestCase("Golem", "Assets/Prefabs/Monsters/GolemMonster.prefab", true)]
    [TestCase("Mushnight", "Assets/Prefabs/Monsters/MushnightMonster.prefab", true)]
    [TestCase("Mushranon", "Assets/Prefabs/Monsters/MushranonMonster.prefab", true)]
    [TestCase("Mushroom", "Assets/Prefabs/Monsters/MushroomMonster.prefab", true)]
    [TestCase("Skullclaw", "Assets/GameData/Monsters/Skullclaw/Skullclaw.prefab", true)]
    [TestCase("GodBoss", "Assets/GameData/Monsters/GodBoss/GodBoss.prefab", false)]
    public void SpeciesOwnsClipsAndUpperBodyLayer(string species, string prefabPath, bool hasDamage)
    {
        string folder = "Assets/Animations/Monsters/" + species;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.That(prefab, Is.Not.Null);
        var animator = prefab.GetComponentInChildren<Animator>(true);
        var controller = animator.runtimeAnimatorController as AnimatorController;
        Assert.That(AssetDatabase.GetAssetPath(controller), Is.EqualTo(folder + "/" + species + ".controller"));
        Assert.That(controller.animationClips.All(c => c != null && AssetDatabase.GetAssetPath(c).StartsWith(folder + "/Clips/")), Is.True);
        var layer = controller.layers.Single(l => l.name == "Hit Reaction");
        Assert.That(layer.defaultWeight, Is.EqualTo(1f));
        Assert.That(layer.blendingMode, Is.EqualTo(AnimatorLayerBlendingMode.Override));
        Assert.That(layer.stateMachine.defaultState.name, Is.EqualTo("Empty"));
        var mask = layer.avatarMask;
        Assert.That(mask, Is.Not.Null);
        Assert.That(mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root), Is.False);
        Assert.That(mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg), Is.False);
        Assert.That(mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg), Is.False);
        int upperCount = 0;
        for (int i = 0; i < mask.transformCount; i++)
        {
            string path = mask.GetTransformPath(i).ToLowerInvariant();
            bool active = mask.GetTransformActive(i);
            if (active) upperCount++;
            string leaf = path.Split('/').Last();
            if (path == "" || leaf == "hip" || leaf == "mixamorig:hips" || path.Split('/').Any(n => n.Contains("thigh") || n.Contains("leg") || n.Contains("foot") || n.Contains("knee")))
                Assert.That(active, Is.False, path);
        }
        Assert.That(upperCount, Is.GreaterThan(0));
        var reaction = layer.stateMachine.states.FirstOrDefault(s => s.state.name == "Damage").state;
        Assert.That(reaction != null, Is.EqualTo(hasDamage));
        if (!hasDamage) return; // GodBoss has no authored hit clip; never substitute an attack.
        Assert.That(reaction.motion, Is.Not.Null);
        Assert.That(reaction.transitions.Any(t => t.hasExitTime && t.destinationState == layer.stateMachine.defaultState), Is.True);
    }
}
#endif
