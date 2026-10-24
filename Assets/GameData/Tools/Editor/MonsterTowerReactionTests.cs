#if UNITY_EDITOR
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MonsterTowerReactionTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void TowerDamageDoesNotReplaceMovementOrCommittedAttack(bool attack, bool contactApplied)
    {
        var root = new GameObject("Isolated reaction test");
        root.SetActive(false);
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Monsters/Skullclaw/Skullclaw.prefab");
            var actor = Object.Instantiate(prefab, root.transform);
            var monster = actor.GetComponent<MushroomMonster>();
            var health = actor.GetComponent<MiningCharacterHealth>();
            var type = typeof(MushroomMonster);
            type.GetField("health", Flags).SetValue(monster, health);
            type.GetField("animator", Flags).SetValue(monster, actor.GetComponentInChildren<Animator>());
            health.ConfigureSpawnHealth(100);
            int attackHash = Animator.StringToHash("Committed test attack");
            int expected = attack ? attackHash : Animator.StringToHash("Walk");
            type.GetField("attackStateHash", Flags).SetValue(monster, attackHash);
            type.GetField("animationState", Flags).SetValue(monster, expected);
            type.GetField("hitApplied", Flags).SetValue(monster, contactApplied);
            var source = new GameObject("Tower source");
            source.transform.SetParent(root.transform);
            source.AddComponent<TowerRuntime>();
            for (int i = 0; i < 10; i++)
            {
                health.DealDamage(1, CombatDamageType.True, source);
                type.GetMethod("OnDamage", Flags).Invoke(monster, null);
                Assert.That(type.GetField("animationState", Flags).GetValue(monster), Is.EqualTo(expected));
            }
            Assert.That(health.Health, Is.EqualTo(90));
        }
        finally { Object.DestroyImmediate(root); }
    }
}
#endif
