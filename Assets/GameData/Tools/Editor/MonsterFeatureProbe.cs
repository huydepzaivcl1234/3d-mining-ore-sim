#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MiningSimulator.Ores;

public static class MonsterFeatureProbe
{
    private static MiningCharacterHealth sample;
    private static double finish;
    [MenuItem("Mining Simulator/Tools/Temporary Monster Validation %&#2")]
    private static void Run()
    {
        if (!EditorApplication.isPlaying) return;
        var monsters = Object.FindObjectsByType<MushroomMonster>(FindObjectsSortMode.None);
        Debug.Log("MUSHROOM VALIDATION: active monsters=" + monsters.Length);
        foreach (var monster in monsters)
        {
            var animator = monster.GetComponentInChildren<Animator>();
            Debug.Log("MUSHROOM ANIMATION: " + animator.GetCurrentAnimatorStateInfo(0).shortNameHash +
                ", position=" + monster.transform.position + ", HP=" + monster.Health.Health);
            foreach (string state in new[] { "Idle", "Walk", "Headbutt", "Damage", "Down" })
                if (!animator.HasState(0, Animator.StringToHash(state))) Debug.LogError("MISSING STATE " + state);
        }
        if (monsters.Length > 0)
        {
            monsters[0].Health.ApplyDamage(10f);
            Debug.Log("MUSHROOM DAMAGE: " + monsters[0].Health.Health);
        }
        sample = new GameObject("Temporary Regen Test").AddComponent<MiningCharacterHealth>();
        sample.ApplyDamage(20f);
        finish = EditorApplication.timeSinceStartup + 5.5;
        EditorApplication.update += Check;
    }
    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < finish) return;
        EditorApplication.update -= Check;
        if (sample == null) return;
        if (Mathf.Approximately(sample.Health, 85f)) Debug.Log("REGEN PASS: 80 -> 85 HP after 5 seconds.");
        else Debug.LogError("REGEN FAIL: HP=" + sample.Health);
        sample.Heal(1000f);
        bool capped = sample.Health == sample.MaxHealth;
        sample.ApplyDamage(1000f);
        sample.Heal(1000f);
        if (capped && sample.Health == 0f) Debug.Log("HEALTH PASS: capped at max; death cannot regenerate.");
        else Debug.LogError("HEALTH BOUNDARY FAIL");
        Object.Destroy(sample.gameObject);
    }
}
#endif
