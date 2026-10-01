#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Opt-in, repeatable shop styling. Never rebuilds cards or changes authored layout.</summary>
    public static class ShopSvgStyleMenu
    {
        private const string ArtPath = "Assets/GameData/Shop/UI/";

        [MenuItem("Mining Simulator/UI/Apply Shop SVG and Smooth Scroll")]
        public static void Apply()
        {
            if (Application.isPlaying) { Debug.LogWarning("Apply shop styling outside Play Mode."); return; }
            var shop = Object.FindAnyObjectByType<MiningShopPanel>(FindObjectsInactive.Include);
            if (shop == null) { Debug.LogError("MiningShopPanel is missing."); return; }
            var shopData = new SerializedObject(shop);
            var panel = shopData.FindProperty("panelRoot").objectReferenceValue as RectTransform;
            if (panel == null) return;
            var old = panel.GetComponentInChildren<ScrollRect>(true);
            if (old == null || old.content == null) { Debug.LogError("Shop product ScrollRect is missing."); return; }
            var card = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "ShopProductCard.svg");
            var buy = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "ShopBuyButton.svg");
            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Rectangle 13.svg");
            if (card == null || buy == null || frame == null)
            { Debug.LogError("Import the three Shop UI SVGs as Textured Sprite first."); return; }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Shop SVG and Smooth Scroll");
            var content = old.content;
            // Serialized product references, not arbitrary Button names or other shop controls.
            var views = shopData.FindProperty("productViews");
            for (int i = 0; i < views.arraySize; i++)
            {
                var view = views.GetArrayElementAtIndex(i);
                var root = view.FindPropertyRelative("root").objectReferenceValue as GameObject;
                var button = view.FindPropertyRelative("buyButton").objectReferenceValue as Button;
                var icon = view.FindPropertyRelative("icon").objectReferenceValue as Image;
                if (root != null) SetSprite(root.GetComponent<Image>(), card);
                if (button != null) SetSprite(button.targetGraphic as Image, buy);
                if (root != null && icon != null)
                {
                    var existing = root.transform.Find("SVG Item Icon Frame");
                    Image graphic;
                    if (existing == null)
                    {
                        var go = new GameObject("SVG Item Icon Frame", typeof(RectTransform), typeof(Image));
                        Undo.RegisterCreatedObjectUndo(go, "Add Shop Icon Frame");
                        go.transform.SetParent(icon.transform.parent, false);
                        graphic = go.GetComponent<Image>();
                    }
                    else graphic = existing.GetComponent<Image>();
                    if (graphic != null)
                    {
                        Undo.RecordObject(graphic.rectTransform, "Shop Icon Frame Layout");
                        var rect = graphic.rectTransform;
                        var source = icon.rectTransform;
                        rect.anchorMin = source.anchorMin; rect.anchorMax = source.anchorMax;
                        rect.pivot = source.pivot;
                        // Keep the authored frame size on repeat runs; never shrink it with the icon.
                        if (existing == null) rect.sizeDelta = source.sizeDelta;
                        rect.anchoredPosition3D = source.anchoredPosition3D;
                        rect.localRotation = source.localRotation; rect.localScale = source.localScale;
                        // First sibling stays behind the icon even when this tool is run again.
                        rect.SetAsFirstSibling();
                        SetSprite(graphic, frame);
                        graphic.raycastTarget = false;
                        Undo.RecordObject(source, "Fit Shop Icon Inside Frame");
                        source.sizeDelta = new Vector2(Mathf.Max(1f, rect.sizeDelta.x - 16f),
                            Mathf.Max(1f, rect.sizeDelta.y - 16f));
                        Undo.RecordObject(icon, "Keep Shop Icon Aspect");
                        icon.preserveAspect = true;
                    }
                }
            }

            if (!(old is ShopSmoothScrollRect))
            {
                var go = old.gameObject;
                var viewport = old.viewport;
                var horizontal = old.horizontal; var vertical = old.vertical;
                var movement = old.movementType; var elasticity = old.elasticity;
                var inertia = old.inertia; var deceleration = old.decelerationRate;
                var sensitivity = old.scrollSensitivity;
                var hbar = old.horizontalScrollbar; var vbar = old.verticalScrollbar;
                var hvisibility = old.horizontalScrollbarVisibility; var vvisibility = old.verticalScrollbarVisibility;
                var hspacing = old.horizontalScrollbarSpacing; var vspacing = old.verticalScrollbarSpacing;
                var changed = old.onValueChanged;
                var enabled = old.enabled;
                Undo.DestroyObjectImmediate(old);
                var smooth = Undo.AddComponent<ShopSmoothScrollRect>(go);
                smooth.content = content; smooth.viewport = viewport;
                smooth.horizontal = horizontal; smooth.vertical = vertical;
                smooth.movementType = movement; smooth.elasticity = elasticity;
                smooth.inertia = inertia; smooth.decelerationRate = deceleration;
                smooth.scrollSensitivity = sensitivity;
                smooth.horizontalScrollbar = hbar; smooth.verticalScrollbar = vbar;
                smooth.horizontalScrollbarVisibility = hvisibility; smooth.verticalScrollbarVisibility = vvisibility;
                smooth.horizontalScrollbarSpacing = hspacing; smooth.verticalScrollbarSpacing = vspacing;
                smooth.onValueChanged = changed; smooth.enabled = enabled;
                EditorUtility.SetDirty(smooth);
            }
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("Shop SVG styling applied; original cards, purchase callbacks and layout retained. Save the scene to keep it.", panel);
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            if (image == null) return;
            Undo.RecordObject(image, "Shop SVG Graphic");
            image.sprite = sprite;
            image.overrideSprite = null;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.material = null;
            EditorUtility.SetDirty(image);
        }
    }
}
#endif
