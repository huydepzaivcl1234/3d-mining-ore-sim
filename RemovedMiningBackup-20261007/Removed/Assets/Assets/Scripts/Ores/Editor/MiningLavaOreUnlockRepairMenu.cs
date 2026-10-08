#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>Repairs the Lava unlock thresholds without replacing edited OreData assets.</summary>
    public static class MiningLavaOreUnlockRepairMenu
    {
        [MenuItem("Mining Simulator/Portal/Repair Lava Ore Unlock Requirements")]
        private static void Repair()
        {
            const string folder = "Assets/GameData/Ores/Lava/";
            OreData basalt = AssetDatabase.LoadAssetAtPath<OreData>(folder + "Basalt.asset");
            OreData ember = AssetDatabase.LoadAssetAtPath<OreData>(folder + "Ember Ore.asset");
            OreData molten = AssetDatabase.LoadAssetAtPath<OreData>(folder + "Molten Core.asset");
            if (basalt == null || ember == null || molten == null)
            {
                EditorUtility.DisplayDialog("Lava ore unlocks",
                    "One or more Lava OreData assets are missing. Set up Lava World first.", "OK");
                return;
            }

            // The original Lava setup used 1 / 8 / 35. Only repair entries left at
            // the Basalt threshold; preserve thresholds intentionally edited higher.
            int changed = 0;
            changed += RepairIfAllUnlocked(ember, basalt.MiningPowerRequired, 8);
            changed += RepairIfAllUnlocked(molten, basalt.MiningPowerRequired, 35);
            if (changed > 0) AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Lava ore unlocks",
                "Updated " + changed + " unlock thresholds. Existing ore prefabs and other OreData settings were preserved. " +
                "You can adjust Mining Power Required in each OreData Inspector.", "OK");
        }

        private static int RepairIfAllUnlocked(OreData ore, int firstOrePower, int requiredPower)
        {
            if (ore.MiningPowerRequired > firstOrePower) return 0;
            var fields = new SerializedObject(ore);
            SerializedProperty power = fields.FindProperty("miningPowerRequired");
            if (power == null) return 0;
            power.intValue = requiredPower;
            fields.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ore);
            return 1;
        }
    }
}
#endif
