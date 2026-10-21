#if UNITY_INCLUDE_TESTS
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;

public class DirectionalCombatTests
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    [Test]
    public void SpamDuringThirdStrikeCannotQueueAnotherCombo()
    {
        var player = new GameObject("Terminal combo fixture");
        try
        {
            var animator = player.AddComponent<Animator>();
            animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/GameData/Player/Animations/Player controller.controller");
            var combat = player.AddComponent<PlayerCombatInput>();
            typeof(PlayerCombatInput).GetField("animator", Flags).SetValue(combat, animator);
            typeof(PlayerCombatInput).GetField("combatMode", Flags).SetValue(combat, true);
            int layer = animator.GetLayerIndex("combat layer");
            animator.Play("Combat", animator.GetLayerIndex("Arms Layer"), 0);
            var process = typeof(PlayerCombatInput).GetMethod("ProcessAttackInput", Flags);
            foreach (float phase in new[] { .1f, .3f, .5f, .7f, .85f })
            {
                animator.Play("Special Attack", layer, phase); animator.Update(0);
                typeof(PlayerCombatInput).GetField("hitApplied", Flags).SetValue(combat, phase >= .7f);
                process.Invoke(combat, new object[] { layer, true });
                var next = animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer) : animator.GetCurrentAnimatorStateInfo(layer);
                Assert.That(next.shortNameHash, Is.EqualTo(Animator.StringToHash("Special Attack")),
                    "Clicks during the terminal strike cannot become a delayed fourth attack.");
            }
        }
        finally { Object.DestroyImmediate(player); }
    }

    [Test]
    public void IntentBeatsAnUnrelatedNearestTarget()
    {
        Assert.That(CombatTargetSelection.TryScore(Vector3.right,Vector3.right*4,80,4,6,out float intended),Is.True);
        Assert.That(CombatTargetSelection.TryScore(Vector3.right,Vector3.forward,80,1,6,out _),Is.False);
        Assert.That(CombatTargetSelection.TryScore(Vector3.forward,Vector3.forward,100,1,6,out float near),Is.True);
        Assert.That(CombatTargetSelection.TryScore(Vector3.forward,Vector3.forward*4,100,4,6,out float far),Is.True);
        Assert.That(near,Is.LessThan(far));
        Assert.That(intended,Is.EqualTo(24));
    }

    [Test]
    public void WallsBlockTargetAcquisitionAndDamagePath()
    {
        var player=new GameObject("Isolated combat player");
        var target=new GameObject("Isolated combat target");
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            player.transform.position=new Vector3(12000,10000,12000);
            target.transform.position=player.transform.position+Vector3.forward*3;
            var combat=player.AddComponent<PlayerCombatInput>();
            var collider=target.AddComponent<BoxCollider>();collider.center=Vector3.up;collider.size=new Vector3(.5f,2,.5f);
            wall.transform.position=player.transform.position+new Vector3(0,1,1.5f);wall.transform.localScale=new Vector3(3,3,.2f);
            Physics.SyncTransforms();
            var path=typeof(PlayerCombatInput).GetMethod("HasClearStrikePath",Flags);
            Assert.That(path.Invoke(combat,new object[]{collider,target.transform}),Is.False);
            wall.SetActive(false);Physics.SyncTransforms();
            Assert.That(path.Invoke(combat,new object[]{collider,target.transform}),Is.True);
        }
        finally {Object.DestroyImmediate(wall);Object.DestroyImmediate(target);Object.DestroyImmediate(player);}
    }

    [Test]
    public void MoveInputUsesCameraYawWithoutChangingCameraOrForward()
    {
        var player=new GameObject("Directional fixture");var camera=new GameObject("Directional camera");
        try
        {
            var input=player.AddComponent<StarterAssets.StarterAssetsInputs>();input.move=Vector2.up;
            var combat=player.AddComponent<PlayerCombatInput>();camera.transform.rotation=Quaternion.Euler(20,90,0);
            typeof(PlayerCombatInput).GetField("locomotionInput",Flags).SetValue(combat,input);
            typeof(PlayerCombatInput).GetField("attackCamera",Flags).SetValue(combat,camera.transform);
            var args=new object[]{false};
            var direction=(Vector3)typeof(PlayerCombatInput).GetMethod("ResolveAttackDirection",Flags).Invoke(combat,args);
            Assert.That(args[0],Is.True);Assert.That(Vector3.Distance(direction,Vector3.right),Is.LessThan(.001f));
            Assert.That(player.transform.forward,Is.EqualTo(Vector3.forward));
            input.move=Vector2.zero;args[0]=true;
            Assert.That(typeof(PlayerCombatInput).GetMethod("ResolveAttackDirection",Flags).Invoke(combat,args),Is.EqualTo(Vector3.forward));
            Assert.That(args[0],Is.False);
        }
        finally {Object.DestroyImmediate(camera);Object.DestroyImmediate(player);}
    }

    [TestCase("Sword Attack 1", "Sword Attack 2")]
    [TestCase("Sword Attack 2", "Special Attack")]
    public void EarlySpamIsDiscardedAndOnlyFreshLinkClickAdvances(string currentAttack, string nextAttack)
    {
        var player=new GameObject("Buffered sword fixture");
        try
        {
            var animator=player.AddComponent<Animator>();
            animator.runtimeAnimatorController=UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/GameData/Player/Animations/Player controller.controller");
            var combat=player.AddComponent<PlayerCombatInput>();
            typeof(PlayerCombatInput).GetField("animator",Flags).SetValue(combat,animator);
            typeof(PlayerCombatInput).GetField("combatMode",Flags).SetValue(combat,true);
            int layer=animator.GetLayerIndex("combat layer");
            animator.Play("Combat",animator.GetLayerIndex("Arms Layer"),0);
            var process=typeof(PlayerCombatInput).GetMethod("ProcessAttackInput",Flags);
            foreach (float phase in new[] { .05f, .15f, .3f, .45f, .6f })
            {
                animator.Play(currentAttack,layer,phase);animator.Update(0);
                process.Invoke(combat,new object[]{layer,true});
            }
            // Contact event has occurred even when its damage query found no enemy.
            typeof(PlayerCombatInput).GetField("hitApplied",Flags).SetValue(combat,true);
            animator.Play(currentAttack,layer,.8f);animator.Update(0);
            process.Invoke(combat,new object[]{layer,false});animator.Update(.01f);
            var next=animator.IsInTransition(layer)?animator.GetNextAnimatorStateInfo(layer):animator.GetCurrentAnimatorStateInfo(layer);
            Assert.That(next.shortNameHash,Is.EqualTo(Animator.StringToHash(currentAttack)),
                "Releasing attack after early spam must not trigger a deferred strike.");
            process.Invoke(combat,new object[]{layer,true});animator.Update(.01f);
            next=animator.IsInTransition(layer)?animator.GetNextAnimatorStateInfo(layer):animator.GetCurrentAnimatorStateInfo(layer);
            Assert.That(next.shortNameHash,Is.EqualTo(Animator.StringToHash(nextAttack)),
                "A fresh click in the link window advances exactly one strike.");
        }
        finally {Object.DestroyImmediate(player);}
    }
}
#endif
