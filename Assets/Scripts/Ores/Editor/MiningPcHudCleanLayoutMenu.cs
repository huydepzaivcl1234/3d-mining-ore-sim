#if UNITY_EDITOR
using System.Collections.Generic;
using Microlight.MicroBar;
using MiningSimulator.Ores;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiningSimulator.Editor
{
    /// <summary>One-shot editable PC HUD layout. Never modifies the scene until invoked.</summary>
    public static class MiningPcHudCleanLayoutMenu
    {
        private const string MenuPath = "Mining Simulator/UI/Arrange Compact PC Gameplay HUD";
        private static readonly Vector2 TL = new(0, 1), TC = new(.5f, 1), TR = new(1, 1);
        private static readonly Vector2 BL = new(0, 0), BR = new(1, 0), C = new(.5f, .5f);
        private static readonly Color Shell = new(.035f, .10f, .105f, .96f);
        private static readonly Color Pale = new(.93f, .97f, .98f, 1);
        private static readonly Color Gold = new(1f, .75f, .19f, 1);

        [MenuItem(MenuPath)]
        private static void Arrange()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("PC HUD", "Stop Play Mode before editing the scene.", "OK");
                return;
            }
            Scene scene = SceneManager.GetActiveScene();
            Canvas canvas = null;
            foreach (Canvas candidate in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.name == "Mining HUD Canvas" && candidate.gameObject.scene == scene)
                { canvas = candidate; break; }
            if (canvas == null || canvas.transform.Find("NPC Progress HUD/Card_Visual") == null)
            {
                EditorUtility.DisplayDialog("PC HUD", "Open the gameplay scene with Mining HUD Canvas and the existing Miner Progress card.", "OK");
                return;
            }
            Undo.SetCurrentGroupName("Arrange Compact PC Gameplay HUD");
            int group = Undo.GetCurrentGroup();
            Transform root = canvas.transform;
            CompactProgress(root);
            CompactRebirth(root);
            CompactShop(root);
            ArrangeOthers(root);
            HideUnusedHints(root);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root.Find("NPC Progress HUD").gameObject;
            Debug.Log("Compact PC HUD arranged. Save the scene to keep the editable layout. XP and Rebirth remain separate systems.", canvas);
        }

        private static void CompactProgress(Transform root)
        {
            RectTransform panel = Rect(root, "NPC Progress HUD");
            if (panel == null) return;
            Layout(panel, TC, new Vector2(0, -16), new Vector2(540, 76), new Vector2(.5f, 1));
            Paint(panel, Color.clear);
            RoundedBackdrop(panel, "PC Progress Background", 19, Shell);
            VisibleGroup(panel);
            Hide(panel, "Header", "Level", "Power", "Experience");
            RectTransform card = Rect(panel, "Card_Visual");
            if (card == null) return;
            Layout(card, C, Vector2.zero, new Vector2(540, 76), C);
            // One background only: the old card image created the extra black block.
            Paint(card, new Color(0, 0, 0, 0));
            Hide(card, "Stitched_Seam", "Gold_Trench", "XP_Percent");
            RectTransform header = Rect(card, "Header_Pill");
            if (header != null)
            {
                Layout(header, C, new Vector2(-177, 13), new Vector2(95, 34), C);
                Paint(header, new Color(0, 0, 0, 0));
                Hide(header, "Title_Text", "Rank_Text", "Star_Medal", "Avatar_Group");
                // Move the live level label out of the old offset avatar group.
                RectTransform badge = Rect(card, "PC Level Pill/Level_Badge") ??
                    Rect(header, "Avatar_Group/Level_Badge");
                if (badge != null)
                {
                    RectTransform pill = Rect(card, "PC Level Pill");
                    if (pill == null)
                    {
                        pill = Create("PC Level Pill", card);
                        var bg = Undo.AddComponent<Image>(pill.gameObject);
                        bg.raycastTarget = false;
                    }
                    Layout(pill, C, new Vector2(-205, 13), new Vector2(73, 30), C);
                    Paint(pill, Color.clear);
                    RoundedBackdrop(pill, "PC Level Background", 8, new Color(.05f, .32f, .25f, 1));
                    if (badge.parent != pill) Undo.SetTransformParent(badge, pill, "Keep live level inside its pill");
                    Layout(badge, C, Vector2.zero, new Vector2(74, 28), C);
                    Text(badge, 17, Pale, TextAlignmentOptions.Center);
                }
            }
            RectTransform well = Rect(card, "Stats_Well");
            if (well != null)
            {
                Layout(well, C, new Vector2(65, 13), new Vector2(400, 34), C);
                Paint(well, new Color(.035f, .10f, .105f, 0));
                Hide(well, "Power_Label", "Reward_Label", "Divider");
                RectTransform power = Rect(well, "Power_Value");
                if (power != null)
                {
                    Layout(power, C, new Vector2(-140, 0), new Vector2(100, 28), C);
                    Text(power, 14, Pale, TextAlignmentOptions.Center);
                }
                RectTransform ore = Rect(well, "Reward_Text");
                if (ore != null)
                {
                    Layout(ore, C, new Vector2(75, 0), new Vector2(250, 28), C);
                    Text(ore, 14, Gold, TextAlignmentOptions.Right);
                }
            }
            RectTransform trench = Rect(card, "XP_Trench");
            if (trench != null)
            {
                Layout(trench, C, new Vector2(0, -19), new Vector2(505, 20), C);
                MicroBar xpBar = panel.GetComponentInChildren<MicroBar>(true);
                if (xpBar != null && xpBar.transform is RectTransform bar)
                {
                    if (bar.parent != trench) Undo.SetTransformParent(bar, trench, "Place XP fill in compact track");
                    Layout(bar, C, Vector2.zero, new Vector2(495, 16), C);
                    bar.SetAsFirstSibling();
                }
            }
            RectTransform xp = Rect(card, "XP_Label");
            if (xp != null)
            {
                Layout(xp, C, new Vector2(0, -19), new Vector2(495, 20), C);
                Text(xp, 12, Pale, TextAlignmentOptions.Center);
                xp.SetAsLastSibling();
            }
            JuicyMinerProgress presenter = panel.GetComponent<JuicyMinerProgress>();
            if (presenter != null) SetReference(presenter, "compactPcXpLabel", null);
            MiningPcHudProgressTextRepair.RepairPanel(panel);
        }

        private static void CompactRebirth(Transform root)
        {
            RectTransform panel = Rect(root, "Rebirth HUD");
            if (panel == null) return;
            RectTransform details = Rect(panel, "Rebirth Details");
            if (details == null)
            {
                details = Create("Rebirth Details", panel);
                Layout(details, panel.anchorMin, Vector2.zero, panel.sizeDelta, panel.pivot);
                // Keep every reward and original confirm button, including their serialized references.
                var children = new List<Transform>();
                foreach (Transform child in panel) if (child != details) children.Add(child);
                foreach (Transform child in children)
                {
                    RectTransform old = child as RectTransform;
                    Vector2 original = old != null ? old.anchoredPosition : Vector2.zero;
                    Undo.SetTransformParent(child, details, "Group rebirth reward details");
                    if (old != null)
                    {
                        Undo.RecordObject(old, "Preserve rebirth detail placement");
                        old.anchoredPosition = original;
                    }
                }
                Layout(details, TR, new Vector2(0, -49), new Vector2(560, 370), new Vector2(1, 1));
                Paint(details, Shell);
                Undo.RecordObject(details.gameObject, "Collapse rebirth details");
                details.gameObject.SetActive(false);
            }
            Layout(panel, TR, new Vector2(-73, -16), new Vector2(95, 40), new Vector2(1, 1));
            Paint(panel, new Color(0, 0, 0, 0));
            VisibleGroup(panel);
            RectTransform badge = Rect(panel, "Compact Rebirth Badge");
            if (badge == null)
            {
                badge = Create("Compact Rebirth Badge", panel);
                Image bg = Undo.AddComponent<Image>(badge.gameObject);
                bg.color = Shell;
                Button button = Undo.AddComponent<Button>(badge.gameObject);
                button.targetGraphic = bg;
                var labelRect = Create("Multiplier", badge);
                TextMeshProUGUI label = Undo.AddComponent<TextMeshProUGUI>(labelRect.gameObject);
                label.text = "R   x1.00";
                label.raycastTarget = false;
                label.fontSize = 17;
                label.color = Gold;
                label.alignment = TextAlignmentOptions.Center;
                Layout(labelRect, C, Vector2.zero, new Vector2(92, 35), C);
                MiningPcRebirthBadge presenter = Undo.AddComponent<MiningPcRebirthBadge>(badge.gameObject);
                MiningRebirthSystem system = Object.FindFirstObjectByType<MiningRebirthSystem>(FindObjectsInactive.Include);
                MiningRebirthPanel rebirthPanel = Object.FindFirstObjectByType<MiningRebirthPanel>(FindObjectsInactive.Include);
                SetReference(presenter, "rebirthSystem", system);
                SetReference(presenter, "rebirthPanel", rebirthPanel);
                SetReference(presenter, "details", details.gameObject);
                SetReference(presenter, "label", label);
            }
            Layout(badge, C, Vector2.zero, new Vector2(95, 40), C);
            Paint(badge, new Color(0, 0, 0, .01f));
            RoundedBackdrop(badge, "PC Rebirth Background", 20, Shell);
            VisibleGroup(badge);
            Image badgeHit = badge.GetComponent<Image>();
            if (badgeHit != null)
            {
                Undo.RecordObject(badgeHit, "Keep rebirth badge clickable");
                badgeHit.raycastTarget = true;
            }
        }

        private static void CompactShop(Transform root)
        {
            RectTransform shop = Rect(root, "NPC Shop");
            if (shop == null) return;
            RectTransform quick = Rect(root, "PC Quick Actions");
            if (quick == null) quick = Create("PC Quick Actions", root);
            Layout(quick, BL, new Vector2(16, 16), new Vector2(294, 148), BL);
            VisibleGroup(quick);
            MoveButton(shop, quick, "BuyMinerButton", 80);
            MoveButton(shop, quick, "Open Upgrades", 0);
            Layout(shop, TL, new Vector2(16, -16), new Vector2(254, 72), TL);
            Paint(shop, new Color(0, 0, 0, 0));
            VisibleGroup(shop);
            RectTransform moneyPill = Rect(shop, "PC Money Pill");
            if (moneyPill == null)
            {
                moneyPill = Create("PC Money Pill", shop);
                Undo.AddComponent<Image>(moneyPill.gameObject);
            }
            Layout(moneyPill, TL, new Vector2(0, -36), new Vector2(138, 36), TL);
            Paint(moneyPill, Color.clear);
            RoundedBackdrop(moneyPill, "PC Money Background", 18, Shell);
            moneyPill.SetAsFirstSibling();
            RectTransform header = Rect(shop, "Header");
            if (header != null)
            {
                Layout(header, TL, Vector2.zero, new Vector2(254, 30), TL);
                Paint(header, Color.clear);
                RoundedBackdrop(header, "PC Header Background", 15, Shell);
                RectTransform title = Rect(header, "Title");
                if (title != null)
                {
                    Layout(title, TL, new Vector2(3, -2), new Vector2(150, 26), TL);
                    Text(title, 13, Gold, TextAlignmentOptions.Left);
                }
            }
            RectTransform count = Rect(shop, "NPC Count");
            if (count != null)
            {
                Layout(count, TL, new Vector2(155, -1), new Vector2(97, 28), TL);
                Text(count, 11, Pale, TextAlignmentOptions.Right);
            }
            RectTransform money = Rect(shop, "Money");
            if (money != null)
            {
                Layout(money, TL, new Vector2(24, -37), new Vector2(105, 30), TL);
                Text(money, 14, Gold, TextAlignmentOptions.Left);
            }
            RectTransform coin = Rect(shop, "Money Icon");
            if (coin != null) Layout(coin, TL, new Vector2(0, -36), new Vector2(23, 23), TL);
            Hide(shop, "NPC Icon");
            MiningUiPanelCoordinator coordinator = Object.FindFirstObjectByType<MiningUiPanelCoordinator>(FindObjectsInactive.Include);
            if (coordinator != null) SetReference(coordinator, "pcQuickActions", quick);
        }

        private static void ArrangeOthers(Transform root)
        {
            RectTransform gems = Rect(root, "Gem HUD");
            if (gems != null)
            {
                Layout(gems, TL, new Vector2(168, -52), new Vector2(130, 36), TL);
                VisibleGroup(gems);
                Hide(gems, "Gem Sparkles");
                PlaceChild(gems, "Gem Icon", new Vector2(18, 18), new Vector2(26, 26));
                PlaceChild(gems, "Gem Amount", new Vector2(67, 18), new Vector2(64, 26));
                PlaceChild(gems, "Add Gems Button", new Vector2(113, 18), new Vector2(25, 25));
                RectTransform gemText = Rect(gems, "Gem Amount");
                if (gemText != null) Text(gemText, 14, Pale, TextAlignmentOptions.Center);
            }
            RectTransform effects = Rect(root, "Active Item Effects");
            if (effects != null)
            {
                Layout(effects, TL, new Vector2(16, -97), new Vector2(180, 45), TL);
                VisibleGroup(effects);
            }
            RectTransform settings = Rect(root, "Audio Menu Button");
            if (settings != null)
            {
                Layout(settings, TR, new Vector2(-16, -16), new Vector2(44, 40), new Vector2(1, 1));
                Paint(settings, new Color(0, 0, 0, .01f));
                RoundedBackdrop(settings, "PC Settings Background", 17, Shell);
                VisibleGroup(settings);
                // Image is the Button hit target. Turning off Raycast Target made SET inert.
                Image hit = settings.GetComponent<Image>();
                if (hit != null)
                {
                    Undo.RecordObject(hit, "Restore settings click target");
                    hit.raycastTarget = true;
                }
                Button open = settings.GetComponent<Button>();
                MiningAudioSettingsPanel audio = Object.FindFirstObjectByType<MiningAudioSettingsPanel>(FindObjectsInactive.Include);
                if (open != null && audio != null) SetReference(audio, "openButton", open);
                Hide(settings, "Label");
                RectTransform compactLabel = Rect(settings, "PC Settings Label");
                if (compactLabel == null)
                {
                    compactLabel = Create("PC Settings Label", settings);
                    TextMeshProUGUI label = Undo.AddComponent<TextMeshProUGUI>(compactLabel.gameObject);
                    label.text = "SET";
                    label.raycastTarget = false;
                }
                Layout(compactLabel, C, Vector2.zero, new Vector2(42, 35), C);
                Text(compactLabel, 12, Pale, TextAlignmentOptions.Center);
            }
            float right = 16;
            foreach (string name in new[] { "Shop Menu Button", "Inventory Menu Button", "Quest Menu Button" })
            {
                RectTransform button = Rect(root, name);
                if (button == null) continue;
                Layout(button, BR, new Vector2(-right, 16), new Vector2(96, 54), BR);
                VisibleGroup(button);
                foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    RectTransform child = label.rectTransform;
                    if (child == button) continue;
                    Layout(child, C, Vector2.zero, new Vector2(92, 48), C);
                    Text(child, 11, Pale, TextAlignmentOptions.Center);
                }
                right += 104;
            }
        }

        private static void MoveButton(RectTransform source, RectTransform quick, string name, float y)
        {
            RectTransform button = Rect(source, name) ?? Rect(quick, name);
            if (button == null) return;
            if (button.parent != quick) Undo.SetTransformParent(button, quick, "Move shop action to PC dock");
            Layout(button, BL, new Vector2(0, y), new Vector2(294, 68), BL);
        }

        private static RectTransform Create(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            Undo.SetTransformParent(obj.transform, parent, "Parent " + name);
            return (RectTransform)obj.transform;
        }

        private static RectTransform Rect(Transform parent, string path) => parent != null ? parent.Find(path) as RectTransform : null;

        private static void PlaceChild(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform child = Rect(parent, name);
            if (child != null) Layout(child, BL, position, size, C);
        }

        private static void Layout(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            Undo.RecordObject(rect, "Layout " + rect.name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
            MiningSafeAreaInset inset = rect.GetComponent<MiningSafeAreaInset>();
            if (inset != null)
            {
                Undo.RecordObject(inset, "Update safe area");
                SerializedProperty pad = new SerializedObject(inset).FindProperty("extraPadding");
                inset.Configure(rect, pad != null ? pad.floatValue : 0);
            }
        }

        private static void Paint(RectTransform rect, Color color)
        {
            Image image = rect.GetComponent<Image>();
            if (image == null) return;
            Undo.RecordObject(image, "Recolor " + rect.name);
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            foreach (BaseMeshEffect effect in rect.GetComponents<BaseMeshEffect>())
                if (effect.enabled)
                { Undo.RecordObject(effect, "Simplify compact HUD material"); effect.enabled = false; }
        }

        private static void RoundedBackdrop(RectTransform parent, string name, float radius, Color color)
        {
            RectTransform rect = Rect(parent, name);
            if (rect == null) rect = Create(name, parent);
            Undo.RecordObject(rect, "Fit rounded PC background");
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = C;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            MiningPcRoundedGraphic graphic = rect.GetComponent<MiningPcRoundedGraphic>();
            if (graphic == null) graphic = Undo.AddComponent<MiningPcRoundedGraphic>(rect.gameObject);
            Undo.RecordObject(graphic, "Style rounded PC background");
            graphic.Configure(radius, color);
            graphic.raycastTarget = false;
            rect.SetAsFirstSibling();
        }

        private static void VisibleGroup(RectTransform rect)
        {
            CanvasGroup group = rect.GetComponent<CanvasGroup>();
            if (group == null) group = Undo.AddComponent<CanvasGroup>(rect.gameObject);
            Undo.RecordObject(group, "Enable PC HUD CanvasGroup");
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            // Respect the parent's menu visibility; only repair this element's own group.
            group.ignoreParentGroups = false;
        }

        private static void Text(RectTransform rect, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            TMP_Text label = rect.GetComponent<TMP_Text>();
            if (label == null) return;
            Undo.RecordObject(label, "Style " + rect.name);
            label.fontSize = fontSize;
            label.enableAutoSizing = false;
            label.color = color;
            label.alignment = alignment;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
        }

        private static void Hide(Transform parent, params string[] paths)
        {
            foreach (string path in paths)
            {
                Transform child = parent.Find(path);
                if (child == null || !child.gameObject.activeSelf) continue;
                Undo.RecordObject(child.gameObject, "Hide large HUD decoration");
                child.gameObject.SetActive(false);
            }
        }

        private static void ActivateAncestors(Transform target, Transform stop)
        {
            for (Transform node = target; node != null && node != stop; node = node.parent)
            {
                Undo.RecordObject(node.gameObject, "Show level badge");
                node.gameObject.SetActive(true);
            }
        }

        private static void SetReference(Object target, string field, Object value)
        {
            if (target == null) return;
            Undo.RecordObject(target, "Configure compact PC HUD");
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) return;
            if (property.propertyType == SerializedPropertyType.Boolean) property.boolValue = true;
            else property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static void HideUnusedHints(Transform root)
        {
            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
                if (label.text != null && label.text.IndexOf("WASD", System.StringComparison.OrdinalIgnoreCase) >= 0)
                { Undo.RecordObject(label.gameObject, "Hide keyboard guide"); label.gameObject.SetActive(false); }
        }
    }
}
#endif
