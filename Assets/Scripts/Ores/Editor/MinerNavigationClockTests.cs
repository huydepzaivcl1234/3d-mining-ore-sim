#if UNITY_INCLUDE_TESTS
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;

public sealed class MinerNavigationClockTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void NavigationUsesTheSameClearanceAsTheMotorProbe()
    {
        var go = new GameObject("Miner clearance test");
        go.SetActive(false);
        var data = ScriptableObject.CreateInstance<NpcData>();
        try
        {
            var miner = go.AddComponent<MiningNpc>();
            typeof(MiningNpc).GetField("npcData", Private).SetValue(miner, data);
            var probe = (float)typeof(MiningNpc).GetMethod("GetObstacleProbeRadius", Private).Invoke(miner, null);
            Assert.That(miner.NavigationRadius, Is.EqualTo(probe));
            Assert.That(miner.NavigationRadius, Is.GreaterThanOrEqualTo(data.ColliderRadius));
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(data); }
    }

    [Test]
    public void ChosenOreApproachDoesNotOrbitAsMinerMoves()
    {
        var go = new GameObject("Miner stable approach test");
        go.SetActive(false);
        var oreObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        oreObject.transform.position = new Vector3(1000f, 0f, 1000f);
        var data = ScriptableObject.CreateInstance<NpcData>();
        try
        {
            var miner = go.AddComponent<MiningNpc>();
            var ore = oreObject.AddComponent<Ore>();
            var stand = new Vector3(1002f, 0f, 1000f);
            typeof(MiningNpc).GetField("npcData", Private).SetValue(miner, data);
            typeof(MiningNpc).GetField("targetOre", Private).SetValue(miner, ore);
            typeof(MiningNpc).GetField("approachOre", Private).SetValue(miner, ore);
            typeof(MiningNpc).GetField("oreApproachPoint", Private).SetValue(miner, stand);
            var method = typeof(MiningNpc).GetMethod("GetPathTargetPosition", Private);
            Assert.That((Vector3)method.Invoke(miner, new object[] { new Vector3(1000f, 0f, 1005f) }), Is.EqualTo(stand));
            Assert.That((Vector3)method.Invoke(miner, new object[] { new Vector3(995f, 0f, 1000f) }), Is.EqualTo(stand));
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(oreObject); Object.DestroyImmediate(data); }
    }

    [TestCase(17)]
    [TestCase(100)]
    public void SavedDayRestoresWithoutTouchingPlayerPrefs(int day)
    {
        var go = new GameObject("Clock restore test");
        go.SetActive(false);
        try
        {
            var clock = go.AddComponent<DayNightSystem>();
            typeof(DayNightSystem).GetMethod("RestoreClock", Private).Invoke(clock,
                new object[] { "{\"version\":1,\"day\":" + day + ",\"period\":0,\"elapsed\":0}" });
            Assert.That(clock.DayNumber, Is.EqualTo(day));
        }
        finally { Object.DestroyImmediate(go); }
    }

    [TestCase("{\"version\":1,\"day\":0}")]
    [TestCase("{\"version\":1,\"day\":9,\"period\":99}")]
    [TestCase("{\"version\":2,\"day\":9}")]
    public void InvalidOrUnsupportedClockDoesNotReplaceDefault(string json)
    {
        var go = new GameObject("Invalid clock test");
        go.SetActive(false);
        try
        {
            var clock = go.AddComponent<DayNightSystem>();
            typeof(DayNightSystem).GetMethod("RestoreClock", Private).Invoke(clock, new object[] { json });
            Assert.That(clock.DayNumber, Is.EqualTo(1));
        }
        finally { Object.DestroyImmediate(go); }
    }
}
#endif
