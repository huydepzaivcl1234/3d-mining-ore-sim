using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    public static class PlayerWeaponSetup
    {
        [MenuItem("Mining Simulator/Setup/Create Fists And Sword Data")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder("Assets/GameData/Weapons"))
                AssetDatabase.CreateFolder("Assets/GameData", "Weapons");
            var fists = CreateWeapon("Fists", WeaponHitMode.StraightSingleTarget, 1.75f, 30f, 1f, Color.cyan);
            CreateWeapon("Sword", WeaponHitMode.ForwardSweep, 2.5f, 110f, 1.5f, new Color(1f, 0.85f, 0.35f));
            var stats = AssetDatabase.LoadAssetAtPath<MiningPlayerStatsData>("Assets/GameData/Player/PlayerStatsData.asset");
            if (stats != null && stats.defaultWeapon == null)
            {
                Undo.RecordObject(stats, "Set default fists");
                stats.defaultWeapon = fists;
                EditorUtility.SetDirty(stats);
                AssetDatabase.SaveAssetIfDirty(stats);
            }
            Selection.activeObject = fists;
        }
        private static WeaponAttackData CreateWeapon(string name, WeaponHitMode mode, float range, float angle, float multiplier, Color color)
        {
            string path = "Assets/GameData/Weapons/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<WeaponAttackData>(path);
            if (existing != null) return existing;
            var data = ScriptableObject.CreateInstance<WeaponAttackData>();
            data.hitMode = mode;
            data.range = range;
            data.angle = angle;
            data.damageMultiplier = multiplier;
            data.trailColor = color;
            data.swingSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameData/Audio/whoosh.mp3");
            AssetDatabase.CreateAsset(data, path);
            Undo.RegisterCreatedObjectUndo(data, "Create weapon data");
            AssetDatabase.SaveAssetIfDirty(data);
            return data;
        }
    }
}
