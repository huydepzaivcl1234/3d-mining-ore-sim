#if UNITY_EDITOR
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;
public sealed class MonsterCrowdTests
{
    private static Vector3 Push(Vector3 a,Vector3 b,int aId=1,int bId=2)
    {
        var type=typeof(MushroomMonster).Assembly.GetType("MiningSimulator.Ores.MonsterCrowd");
        return (Vector3)type.GetMethod("Repulsion",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{a,.4f,b,.4f,aId,bId,.03f});
    }
    [Test] public void SeparatedNeighboursDoNotPush()=>Assert.That(Push(Vector3.zero,Vector3.right*2),Is.EqualTo(Vector3.zero));
    [Test] public void OverlappingNeighboursPushApart(){Assert.That(Push(Vector3.zero,Vector3.right*.5f).x,Is.LessThan(0));}
    [Test] public void ExactOverlapHasFiniteOppositeDirections(){var a=Push(Vector3.zero,Vector3.zero);var b=Push(Vector3.zero,Vector3.zero,2,1);Assert.That(a.magnitude,Is.GreaterThan(0));Assert.That((a+b).sqrMagnitude,Is.LessThan(.000001f));}
    [Test] public void AirborneActorsDoNotPushActorsBelowThem()=>Assert.That(Push(Vector3.zero,Vector3.up*4),Is.EqualTo(Vector3.zero));
    [Test] public void DistantSteeringNeverReducesConfiguredNearRate()
    {
        var data=ScriptableObject.CreateInstance<GameplayPerformanceData>();try{
            Assert.That(data.SteeringInterval(2),Is.EqualTo(.05f));Assert.That(data.SteeringInterval(20),Is.EqualTo(.15f));
            data.monsterSteeringInterval=.3f;Assert.That(data.SteeringInterval(20),Is.EqualTo(.3f));
            data.monsterSteeringInterval=0;Assert.That(data.SteeringInterval(20),Is.Zero);
        }finally{Object.DestroyImmediate(data);}
    }
    [Test] public void CrowdModeRestoresAuthoredCollisionMask()
    {
        var go=new GameObject("Inactive crowd fixture");go.SetActive(false);
        try{var m=go.AddComponent<MushroomMonster>();var motor=go.GetComponent<CharacterController>();motor.excludeLayers=1<<5;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(MushroomMonster).GetField("motor",flags).SetValue(m,motor);
            var configure=typeof(MushroomMonster).GetMethod("ConfigureCrowd",flags);configure.Invoke(m,new object[]{true});
            Assert.That(motor.excludeLayers.value & LayerMask.GetMask("Enemy"),Is.Not.Zero);configure.Invoke(m,new object[]{false});
            Assert.That(motor.excludeLayers.value,Is.EqualTo(1<<5));
        }finally{Object.DestroyImmediate(go);}
    }
}
#endif
