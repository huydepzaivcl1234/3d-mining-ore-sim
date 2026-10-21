using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using MiningSimulator.Ores;

public static class WeaponHitValidation
{
    [MenuItem("Mining Simulator/Tools/Validate Fist Hits")]
    public static void Run()
    {
        var objects = new List<GameObject>();
        try
        {
            var player = new GameObject("Temporary weapon validation");
            objects.Add(player);
            player.transform.position = new Vector3(12000, 10000, 12000);
            var combat = player.AddComponent<PlayerCombatInput>();
            typeof(PlayerCombatInput).GetField("targetLayers", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(combat, (LayerMask)(1 << 30));
            MiningCharacterHealth Target(string name, Vector3 offset)
            {
                var obj = new GameObject(name) { layer = 30 };
                objects.Add(obj);
                obj.transform.position = player.transform.position + offset;
                obj.AddComponent<SphereCollider>().radius = 0.2f;
                var hp = obj.AddComponent<MiningCharacterHealth>();
                hp.Respawn();
                return hp;
            }
            var low = Target("Low mushroom", new Vector3(0, 0.35f, 1.1f));
            var far = Target("Second forward target", new Vector3(0, 0.35f, 1.65f));
            var side = Target("Side target", new Vector3(1.1f, 0.35f, 0));
            var back = Target("Behind target", new Vector3(0, 0.35f, -1.1f));
            var high = Target("Overhead target", new Vector3(0, 3.5f, 1));
            Physics.SyncTransforms();
            var hit = typeof(PlayerCombatInput).GetMethod("ApplyHit", BindingFlags.Instance | BindingFlags.NonPublic);
            hit.Invoke(combat, null);
            bool fistPass = low.Health < low.MaxHealth && far.Health == far.MaxHealth && side.Health == side.MaxHealth && back.Health == back.MaxHealth && high.Health == high.MaxHealth;
            Debug.Log("FIST VALIDATION fists low target=" + low.Health + "/" + low.MaxHealth + ", nearest-only=" + fistPass);
            if (fistPass) Debug.Log("FIST VALIDATION PASS");
            else Debug.LogWarning("FIST VALIDATION FAILED: check nearest forward target and vertical reach.");
        }
        finally
        {
            foreach (var obj in objects) Object.DestroyImmediate(obj);
        }
    }
}
