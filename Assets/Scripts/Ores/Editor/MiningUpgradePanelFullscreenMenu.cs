using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>
    /// Expands the existing scene-authored Upgrade Panel and arranges its existing content
    /// as a responsive two-column, five-row fullscreen layout.
    /// </summary>
    public static class MiningUpgradePanelFullscreenMenu
    {
        private const string MenuPath = "Mining Simulator/UI/Make Upgrade Panel Fullscreen";
        private const string PanelName = "Upgrade Panel";

        private static readonly string[] CardNames =
        {
            "Money Reward Upgrade",
            "Rare Ore Upgrade",
            "Ore Damage Upgrade",
            "Ore Spawn Speed Upgrade",
            "NPC Move Speed Upgrade",
            "NPC Capacity Upgrade",
            "Lucky Block Reward Upgrade",
            "Lucky Block Drop Chance Upgrade",
            "NPC Experience Upgrade",
            "Item Drop Chance Upgrade"
        };

        [MenuItem(MenuPath, priority = 220)]
        private static void MakeFullscreen()
        {
            RectTransform panel = FindUpgradePanel();
            if (panel == null)
            {
                EditorUtility.DisplayDialog(
                    "Upgrade Panel not found",
                    "Open the gameplay scene and make sure a RectTransform named 'Upgrade Panel' exists.",
                    "OK");
                return;
            }

            List<RectTransform> targets = CollectLayoutTargets(panel);
            Undo.RecordObjects(targets.ToArray(), "Make Upgrade Panel Fullscreen");

            Stretch(panel, Vector2.zero, Vector2.one);
            LayoutHeader(panel.Find("Header") as RectTransform);
            LayoutCornerButton(panel.Find("Close") as RectTransform);
            LayoutBackButton(panel.Find("Back") as RectTransform);

            int laidOutCards = 0;
            for (int index = 0; index < CardNames.Length; index++)
            {
                RectTransform card = panel.Find(CardNames[index]) as RectTransform;
                if (card == null)
                {
                    Debug.LogWarning($"Upgrade Panel child '{CardNames[index]}' was not found.", panel);
                    continue;
                }

                LayoutCard(card, index);
                laidOutCards++;
            }

            foreach (RectTransform target in targets)
            {
                EditorUtility.SetDirty(target);
            }

            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Selection.activeTransform = panel;
            SceneView.FrameLastActiveSceneView();

            Debug.Log(
                $"Upgrade Panel now fills the Canvas with {laidOutCards} cards in two columns. " +
                "Styles, gameplay references, and animation components were not changed.",
                panel);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateMakeFullscreen()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static RectTransform FindUpgradePanel()
        {
            if (Selection.activeTransform is RectTransform selected &&
                selected.name == PanelName &&
                selected.gameObject.scene.IsValid())
            {
                return selected;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            RectTransform[] transforms = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (RectTransform candidate in transforms)
            {
                if (candidate == null ||
                    candidate.name != PanelName ||
                    candidate.gameObject.scene != activeScene ||
                    EditorUtility.IsPersistent(candidate))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static List<RectTransform> CollectLayoutTargets(RectTransform panel)
        {
            var targets = new List<RectTransform> { panel };
            AddIfPresent(targets, panel.Find("Header") as RectTransform);
            AddIfPresent(targets, panel.Find("Close") as RectTransform);
            AddIfPresent(targets, panel.Find("Back") as RectTransform);

            foreach (string cardName in CardNames)
            {
                AddIfPresent(targets, panel.Find(cardName) as RectTransform);
            }

            return targets;
        }

        private static void AddIfPresent(List<RectTransform> targets, RectTransform target)
        {
            if (target != null)
            {
                targets.Add(target);
            }
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void LayoutHeader(RectTransform header)
        {
            if (header == null)
            {
                return;
            }

            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 82f);
            header.localScale = Vector3.one;
        }

        private static void LayoutCornerButton(RectTransform close)
        {
            if (close == null)
            {
                return;
            }

            close.anchorMin = Vector2.one;
            close.anchorMax = Vector2.one;
            close.pivot = Vector2.one;
            close.anchoredPosition = new Vector2(-12f, -12f);
            close.sizeDelta = new Vector2(58f, 58f);
            close.localScale = Vector3.one;
        }

        private static void LayoutBackButton(RectTransform back)
        {
            if (back == null)
            {
                return;
            }

            back.anchorMin = new Vector2(0.5f, 0f);
            back.anchorMax = new Vector2(0.5f, 0f);
            back.pivot = new Vector2(0.5f, 0f);
            back.anchoredPosition = new Vector2(0f, 20f);
            back.sizeDelta = new Vector2(220f, 54f);
            back.localScale = Vector3.one;
        }

        private static void LayoutCard(RectTransform card, int index)
        {
            const float leftMargin = 0.035f;
            const float rightMargin = 0.965f;
            const float centerGap = 0.03f;
            const float contentTop = 0.90f;
            const float contentBottom = 0.10f;
            const float rowGap = 0.012f;
            const int rowCount = 5;

            int column = index % 2;
            int row = index / 2;
            float cardHeight =
                (contentTop - contentBottom - rowGap * (rowCount - 1)) / rowCount;
            float top = contentTop - row * (cardHeight + rowGap);
            float bottom = top - cardHeight;
            float center = 0.5f;

            float minX = column == 0 ? leftMargin : center + centerGap * 0.5f;
            float maxX = column == 0 ? center - centerGap * 0.5f : rightMargin;

            Stretch(card, new Vector2(minX, bottom), new Vector2(maxX, top));
        }
    }
}
