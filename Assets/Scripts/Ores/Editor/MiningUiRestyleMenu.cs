#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Restores only authored uGUI surfaces affected by Candy removal.</summary>
    public static class MiningUiRestyleMenu
    {
        private readonly struct GradientColors
        {
            public readonly Color Top;
            public readonly Color Bottom;
            public GradientColors(Color top, Color bottom) { Top = top; Bottom = bottom; }
        }

        private readonly struct StyleTarget
        {
            public readonly string Path;
            public readonly int Palette;
            public StyleTarget(string path, int palette) { Path = path; Palette = palette; }
        }

        private static readonly GradientColors[] Palettes =
        {
            new(new Color(0.22f, 0.62f, 0.44f, 1f), new Color(0.1f, 0.42f, 0.27f, 1f)),
            new(new Color(0.12f, 0.1f, 0.08f, 1f), new Color(0.05f, 0.04f, 0.03f, 1f)),
            new(new Color(1f, 0.27f, 0.13f, 1f), new Color(0.8f, 0.1f, 0f, 1f)),
            new(new Color(1f, 0.72f, 0.19f, 1f), new Color(0.8f, 0.52f, 0f, 1f)),
            new(new Color(0.6482338f, 0.8553459f, 0.73270303f, 0.7411765f), new Color(1f, 1f, 1f, 1f)),
            new(new Color(0f, 1f, 0.40745032f, 1f), new Color(0.8301887f, 0.6053395f, 0.17491381f, 1f)),
            new(new Color(0.12f, 0.17f, 0.27f, 1f), new Color(0.04f, 0.07f, 0.13f, 1f)),
            new(new Color(0.2f, 0.84f, 0.69f, 1f), new Color(0.08f, 0.49f, 0.36f, 1f)),
        };

        // Exact Canvas-relative paths recorded before the effect was removed.
        // No sprites, text, scroll-view hit areas or unrelated Images are restyled.
        private static readonly StyleTarget[] AffectedSurfaces =
        {
            new("Audio Menu Button", 0),
            new("Audio Settings Panel", 1),
            new("Audio Settings Panel/Close", 2),
            new("Audio Settings Panel/Header", 0),
            new("Audio Settings Panel/Language Toggle", 0),
            new("Audio Settings Panel/Master Slider", 1),
            new("Audio Settings Panel/Master Slider/Fill Area/Fill", 3),
            new("Audio Settings Panel/Master Slider/Handle Slide Area/Handle", 1),
            new("Audio Settings Panel/Music Slider", 1),
            new("Audio Settings Panel/Music Slider/Fill Area/Fill", 3),
            new("Audio Settings Panel/Music Slider/Handle Slide Area/Handle", 1),
            new("Audio Settings Panel/Reset Data", 2),
            new("Audio Settings Panel/Return To Main Menu", 3),
            new("Audio Settings Panel/SFX Slider", 1),
            new("Audio Settings Panel/SFX Slider/Fill Area/Fill", 3),
            new("Audio Settings Panel/SFX Slider/Handle Slide Area/Handle", 1),
            new("Computer Info Panel", 1),
            new("Computer Info Panel/Header", 1),
            new("Computer Info Panel/Header/Close", 2),
            new("Computer Info Panel/Upgrade", 3),
            new("Gem HUD", 0),
            new("Interaction Hover Prompt", 1),
            new("Inventory Panel", 1),
            new("Inventory Panel/Close", 2),
            new("Inventory Panel/Grid/Slot 01", 1),
            new("Inventory Panel/Grid/Slot 02", 1),
            new("Inventory Panel/Grid/Slot 03", 1),
            new("Inventory Panel/Grid/Slot 04", 1),
            new("Inventory Panel/Grid/Slot 05", 1),
            new("Inventory Panel/Grid/Slot 06", 1),
            new("Inventory Panel/Grid/Slot 07", 1),
            new("Inventory Panel/Grid/Slot 08", 1),
            new("Inventory Panel/Grid/Slot 09", 1),
            new("Inventory Panel/Grid/Slot 10", 1),
            new("Inventory Panel/Grid/Slot 11", 1),
            new("Inventory Panel/Grid/Slot 12", 1),
            new("Inventory Panel/Grid/Slot 13", 1),
            new("Inventory Panel/Grid/Slot 14", 1),
            new("Inventory Panel/Grid/Slot 15", 1),
            new("Inventory Panel/Grid/Slot 16", 1),
            new("Inventory Panel/Grid/Slot 17", 1),
            new("Inventory Panel/Grid/Slot 18", 1),
            new("Inventory Panel/Grid/Slot 19", 1),
            new("Inventory Panel/Grid/Slot 20", 1),
            new("Inventory Panel/Grid/Slot 21", 1),
            new("Inventory Panel/Grid/Slot 22", 1),
            new("Inventory Panel/Grid/Slot 23", 1),
            new("Inventory Panel/Grid/Slot 24", 1),
            new("Inventory Panel/Grid/Slot 25", 1),
            new("Inventory Panel/Grid/Slot 26", 1),
            new("Inventory Panel/Grid/Slot 27", 1),
            new("Inventory Panel/Grid/Slot 28", 1),
            new("Inventory Panel/Grid/Slot 29", 1),
            new("Inventory Panel/Grid/Slot 30", 1),
            new("Inventory Panel/Grid/Slot 31", 1),
            new("Inventory Panel/Grid/Slot 32", 1),
            new("Inventory Panel/Header", 0),
            new("Main Menu/Exit Confirmation/Exit Confirmation Dialog", 1),
            new("Main Menu/Exit Confirmation/Exit Confirmation Dialog/Cancel Exit Button", 2),
            new("Main Menu/Exit Confirmation/Exit Confirmation Dialog/Confirm Exit Button", 2),
            new("Main Menu/Main Menu Card/Main View/Exit Button", 2),
            new("Main Menu/Main Menu Card/Main View/Play Button", 3),
            new("Main Menu/Main Menu Card/Main View/Quest Button", 1),
            new("Main Menu/Main Menu Card/Main View/Settings Button", 0),
            new("Main Menu/Main Menu Card/Settings View/Back Button", 3),
            new("Main Menu/Main Menu Card/Settings View/Language Button", 0),
            new("Main Menu/Main Menu Card/Settings View/Master Slider", 1),
            new("Main Menu/Main Menu Card/Settings View/Master Slider/Fill", 3),
            new("Main Menu/Main Menu Card/Settings View/Master Slider/Handle", 1),
            new("Main Menu/Main Menu Card/Settings View/Music Slider", 1),
            new("Main Menu/Main Menu Card/Settings View/Music Slider/Fill", 3),
            new("Main Menu/Main Menu Card/Settings View/Music Slider/Handle", 1),
            new("Main Menu/Main Menu Card/Settings View/SFX Slider", 1),
            new("Main Menu/Main Menu Card/Settings View/SFX Slider/Fill", 3),
            new("Main Menu/Main Menu Card/Settings View/SFX Slider/Handle", 1),
            new("NPC Progress HUD", 3),
            new("NPC Progress HUD/Header", 0),
            new("NPC Shop", 1),
            new("NPC Shop/Buy Mining NPC", 3),
            new("NPC Shop/Header", 0),
            new("NPC Shop/Open Upgrades", 3),
            new("Quest Menu Button", 1),
            new("Quest Panel/Quest Card", 1),
            new("Quest Panel/Quest Card/Header", 1),
            new("Quest Panel/Quest Card/Header/Close Button", 2),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_buy_3_npc", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_buy_3_npc/Claim Button", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_buy_3_npc/Period Badge", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_buy_3_npc/Progress Background", 4),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_buy_3_npc/Progress Background/Progress Fill", 5),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_mine_500", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_mine_500/Claim Button", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_mine_500/Period Badge", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_mine_500/Progress Background", 4),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_mine_500/Progress Background/Progress Fill", 5),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_mine_5000", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_mine_5000/Claim Button", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_mine_5000/Period Badge", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_mine_5000/Progress Background", 4),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row daily_mine_5000/Progress Background/Progress Fill", 5),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row weekly_rebirth_1", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row weekly_rebirth_1/Claim Button", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row weekly_rebirth_1/Period Badge", 1),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row weekly_rebirth_1/Progress Background", 4),
            new("Quest Panel/Quest Card/Quest Scroll View/Viewport/Content/Quest Row weekly_rebirth_1/Progress Background/Progress Fill", 5),
            new("Rebirth Confirmation", 1),
            new("Rebirth Confirmation/Cancel Rebirth", 2),
            new("Rebirth Confirmation/Confirm Rebirth", 2),
            new("Rebirth Confirmation/Header", 2),
            new("Rebirth HUD", 1),
            new("Rebirth HUD/Header", 2),
            new("Rebirth HUD/Open Rebirth", 2),
            new("Shop Menu Button", 0),
            new("Shop Panel/Shop Card", 1),
            new("Shop Panel/Shop Card/Header", 0),
            new("Shop Panel/Shop Card/Header/Close Button", 2),
            new("Shop Panel/Shop Card/Product Scroll View/Viewport/Product Content/Rare Gift Product", 6),
            new("Shop Panel/Shop Card/Product Scroll View/Viewport/Product Content/Rare Gift Product/Buy Rare Gift Button", 7),
            new("Shop Panel/Shop Card/Product Scroll View/Viewport/Product Content/Shop Product 2", 6),
            new("Shop Panel/Shop Card/Product Scroll View/Viewport/Product Content/Shop Product 2/Buy Product Button", 7),
            new("Shop Panel/Shop Card/Product Scroll View/Viewport/Product Content/Shop Product 3", 6),
            new("Shop Panel/Shop Card/Product Scroll View/Viewport/Product Content/Shop Product 3/Buy Product Button", 7),
            new("Shop Panel/Shop Card/Product Scroll View/Viewport/Product Content/Shop Product 4", 6),
            new("Shop Panel/Shop Card/Product Scroll View/Viewport/Product Content/Shop Product 4/Buy Product Button", 7),
            new("Shop Panel/Shop Card/Spin Once Button", 3),
            new("Shop Panel/Shop Card/Spin Ten Button", 3),
            new("Shop Panel/Shop Card/Wheel Pointer", 1),
            new("Shop Panel/Shop Card/Wheel Result Panel", 1),
            new("Shop Panel/Shop Card/Wheel Root", 1),
            new("Shop Panel/Shop Card/Wheel Root/Wheel Hub", 1),
            new("Upgrade Panel/Back", 3),
            new("Upgrade Panel/Close", 2),
            new("Upgrade Panel/Header", 3),
            new("Upgrade Panel/Item Drop Chance Upgrade", 3),
            new("Upgrade Panel/Lucky Block Drop Chance Upgrade", 3),
            new("Upgrade Panel/Lucky Block Reward Upgrade", 3),
            new("Upgrade Panel/Money Reward Upgrade", 3),
            new("Upgrade Panel/NPC Capacity Upgrade", 3),
            new("Upgrade Panel/NPC Experience Upgrade", 3),
            new("Upgrade Panel/NPC Move Speed Upgrade", 3),
            new("Upgrade Panel/Ore Damage Upgrade", 3),
            new("Upgrade Panel/Ore Spawn Speed Upgrade", 3),
            new("Upgrade Panel/Rare Ore Upgrade", 3),
        };

        [MenuItem("Mining Simulator/Fixes/Restyle UI After Candy Removal")]
        private static void RestyleAffectedSurfaces()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("UI Restyle", "Exit Play Mode first.", "OK");
                return;
            }

            Canvas canvas = null;
            foreach (Canvas candidate in Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == "Mining HUD Canvas" &&
                    candidate.gameObject.scene ==
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene())
                {
                    canvas = candidate;
                    break;
                }
            }
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("UI Restyle",
                    "Open your gameplay scene containing Mining HUD Canvas first.", "OK");
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Restyle Previously Affected Mining UI");
            int styled = 0;
            int missing = 0;
            foreach (StyleTarget target in AffectedSurfaces)
            {
                Transform child = canvas.transform.Find(target.Path);
                UnityEngine.UI.Image image = child != null
                    ? child.GetComponent<UnityEngine.UI.Image>() : null;
                if (image == null)
                {
                    missing++;
                    continue;
                }
                if (image.GetComponent<MiningCandyGradient>() != null) continue;

                GradientColors colors = Palettes[target.Palette];
                MiningUiGradient effect = image.GetComponent<MiningUiGradient>();
                if (effect == null)
                {
                    effect = Undo.AddComponent<MiningUiGradient>(image.gameObject);
                }
                else if (effect.HasColors(colors.Top, colors.Bottom))
                {
                    continue;
                }
                else
                {
                    Undo.RecordObject(effect, "Restyle Mining UI Surface");
                }
                effect.SetColors(colors.Top, colors.Bottom);
                EditorUtility.SetDirty(effect);
                styled++;
            }

            RectTransform upgradePanel =
                canvas.transform.Find("Upgrade Panel") as RectTransform;
            if (upgradePanel != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(
                    upgradePanel.gameObject,
                    "Preserve Upgrade Card Visual Sizes");
                MiningUpgradePanelFullscreenMenu.PreserveUpgradeCardVisualSizing(upgradePanel);
            }

            if (styled > 0 || upgradePanel != null)
            {
                EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
                Undo.CollapseUndoOperations(undoGroup);
            }
            EditorUtility.DisplayDialog("UI Restyle",
                $"Restyled {styled} surfaces ({missing} no longer found). " +
                "Review the UI and save your own scene.", "OK");
        }
    }
}
#endif
