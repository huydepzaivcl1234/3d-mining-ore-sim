#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Styles the existing HUD button in place. Its position, callbacks and SFX stay intact.</summary>
    public static class NpcBuySvgStyleMenu
    {
        [MenuItem("Mining Simulator/UI/Apply NPC Buy SVG")]
        public static void Apply()
        {
            if (Application.isPlaying) return;
            var hud = Object.FindAnyObjectByType<MiningHud>(FindObjectsInactive.Include);
            if (hud == null) return;
            var fields = new SerializedObject(hud);
            var button = fields.FindProperty("buyButton").objectReferenceValue as Button;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameData/NPC/UI/Group 3 (1).svg");
            if (button == null || sprite == null) return;
            var root = button.transform as RectTransform;
            var label = root.Find("Label").GetComponent<TextMeshProUGUI>();
            var icon = root.Find("Icon").GetComponent<Image>();
            if (label == null || icon == null) return;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("NPC Buy SVG and Price");
            var background = button.GetComponent<Image>();
            Undo.RecordObject(background, "NPC Buy SVG");
            background.sprite = sprite;
            background.overrideSprite = null;
            background.type = Image.Type.Simple;
            background.color = Color.white;
            background.material = null;
            foreach (var shadow in root.GetComponents<Shadow>())
            {
                Undo.RecordObject(shadow, "Use SVG Border");
                shadow.enabled = false;
            }
            Layout(icon.rectTransform, new Vector2(.075f, .29f), new Vector2(.173f, .71f));
            Undo.RecordObject(icon, "Fit Miner Icon");
            icon.preserveAspect = true;
            Layout(label.rectTransform, new Vector2(.32f, .53f), new Vector2(.91f, .74f));
            StyleText(label, 14f, Color.white);

            var cost = Child(root, "NPC Price", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            Layout(cost.rectTransform, new Vector2(.50f, .27f), new Vector2(.86f, .51f));
            Undo.RecordObject(cost, "NPC Price Font");
            cost.font = label.font;
            StyleText(cost, 18f, new Color(1f, .85f, .30f));
            var coin = Child(root, "NPC Price Coin", typeof(Image)).GetComponent<Image>();
            Layout(coin.rectTransform, new Vector2(.40f, .27f), new Vector2(.46f, .51f));
            var ui = AssetDatabase.LoadAssetAtPath<MiningUiData>("Assets/GameData/UI/MiningUiData.asset");
            Undo.RecordObject(coin, "NPC Price Coin");
            coin.sprite = ui != null ? ui.MoneyIconSprite : null;
            coin.color = Color.white;
            coin.preserveAspect = true;
            coin.raycastTarget = false;
            // The existing HUD already owns wallet/count/language subscriptions and purchase callbacks.
            fields.FindProperty("buyButtonLabel").objectReferenceValue = label;
            fields.FindProperty("buyButtonCostText").objectReferenceValue = cost;
            fields.ApplyModifiedProperties();
            var shop = fields.FindProperty("npcShop").objectReferenceValue as NpcShop;
            label.text = MiningLocalization.Text("BUY NPC", "Mua npc");
            cost.text = MiningMoneyFormatter.Format(shop != null ? shop.NpcCost : 0);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("NPC Buy SVG applied. Save the scene to keep this layout.", root);
        }

        private static RectTransform Child(RectTransform parent, string name, System.Type component)
        {
            var child = parent.Find(name) as RectTransform;
            if (child != null) return child;
            var go = new GameObject(name, typeof(RectTransform), component);
            Undo.RegisterCreatedObjectUndo(go, "NPC Price Visual");
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Layout(RectTransform rect, Vector2 min, Vector2 max)
        {
            Undo.RecordObject(rect, "NPC SVG Content Layout");
            rect.anchorMin = min; rect.anchorMax = max;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void StyleText(TextMeshProUGUI text, float size, Color color)
        {
            Undo.RecordObject(text, "NPC Buy Text");
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f; text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Truncate;
        }
    }
}
#endif
