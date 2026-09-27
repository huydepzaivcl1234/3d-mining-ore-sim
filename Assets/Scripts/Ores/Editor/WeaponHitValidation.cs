using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using MiningSimulator.Ores;

public static class WeaponHitValidation
{
    [MenuItem("Mining Simulator/Tools/Validate Weapon Hits")]
    public static void Run()
    {
        var objects = new List<GameObject>();
        var weapon = ScriptableObject.CreateInstance<WeaponAttackData>();
        try
        {
            var player = new GameObject("Temporary weapon validation");
            objects.Add(player);
            player.transform.position = new Vector3(12000, 10000, 12000);
            var combat = player.AddComponent<PlayerCombatInput>();
            typeof(PlayerCombatInput).GetField("targetLayers", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(combat, (LayerMask)(1 << 30));
            weapon.hitOriginOffset = Vector3.up;
            weapon.range = 1.75f;
            weapon.angle = 30f;
            combat.TryEquipWeapon(weapon);
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
            Debug.Log("WEAPON VALIDATION fists low target=" + low.Health + "/" + low.MaxHealth + ", nearest-only=" + fistPass);
            low.Respawn();
            weapon.hitMode = WeaponHitMode.ForwardSweep;
            weapon.angle = 110;
            var diagonal = Target("Sword diagonal", new Vector3(0.8f, 0.35f, 1.1f));
            diagonal.gameObject.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            Physics.SyncTransforms();
            hit.Invoke(combat, null);
            bool swordPass = low.Health < low.MaxHealth && far.Health < far.MaxHealth && Mathf.Approximately(diagonal.MaxHealth - diagonal.Health, combat.Damage) && side.Health == side.MaxHealth && back.Health == back.MaxHealth && high.Health == high.MaxHealth;
            Debug.Log("WEAPON VALIDATION sword low/diagonal/multiple/dedup=" + swordPass);
            if (fistPass && swordPass) Debug.Log("WEAPON VALIDATION PASS");
            else Debug.LogWarning("WEAPON VALIDATION FAILED: low enemies must be hittable without accepting side, behind or overhead targets.");
        }
        finally
        {
            foreach (var obj in objects) Object.DestroyImmediate(obj);
            Object.DestroyImmediate(weapon);
        }
    }
}
