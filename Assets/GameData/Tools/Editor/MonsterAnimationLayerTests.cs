#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class MonsterAnimationLayerTests
{
    [TestCase("Assets/Prefabs/Monsters/GolemMonster.prefab")]
    [TestCase("Assets/Prefabs/Monsters/ForestGolem.prefab")]
    [TestCase("Assets/Prefabs/Monsters/MushnightMonster.prefab")]
    public void AttackReleasesPreviousHitPose(string prefabPath)
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), scene);
        MiningSimulator.Ores.MonsterRewardData testData = null;
        try
        {
            root.hideFlags = HideFlags.HideAndDontSave;
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            var monster = root.GetComponent<MiningSimulator.Ores.MushroomMonster>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            // This EditMode fixture tests animation, not the runtime warning quad.
            // Clone the data so the authored asset is never modified.
            testData = Object.Instantiate(monster.SpeciesData);
            testData.hideFlags = HideFlags.HideAndDontSave;
            testData.combat.warningShader = null;
            typeof(MiningSimulator.Ores.MushroomMonster).GetField("rewards", flags).SetValue(monster, testData);
            typeof(MiningSimulator.Ores.MushroomMonster).GetMethod("Awake", flags).Invoke(monster, null);
            var animator = root.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.Update(0);
            int layer = animator.GetLayerIndex("Hit Reaction");
            animator.SetLayerWeight(layer, 1f);
            animator.Play("Hit Reaction.Damage", layer, .3f); animator.Update(.1f);
            typeof(MiningSimulator.Ores.MushroomMonster).GetMethod("BeginAttack", flags).Invoke(monster, null);
            animator.Update(.2f); animator.Update(.2f);
            Assert.That(animator.GetLayerWeight(layer), Is.Zero, "Old reaction pose must not override the attack");
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(monster.CombatData.attackState), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (testData != null) Object.DestroyImmediate(testData);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }

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
        var monster = prefab.GetComponent<MiningSimulator.Ores.MushroomMonster>();
        if (monster != null && monster.CombatData != null)
        {
            var stateNames = controller.layers[0].stateMachine.states.Select(s => s.state.name).ToArray();
            Assert.That(stateNames, Does.Contain(monster.CombatData.attackState), "Missing primary attack state");
            if (!string.IsNullOrEmpty(monster.CombatData.secondAttackState))
                Assert.That(stateNames, Does.Contain(monster.CombatData.secondAttackState), "Missing secondary attack state");
        }
        var layer = controller.layers.Single(l => l.name == "Hit Reaction");
        Assert.That(layer.defaultWeight, Is.EqualTo(hasDamage ? 1f : 0f));
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
