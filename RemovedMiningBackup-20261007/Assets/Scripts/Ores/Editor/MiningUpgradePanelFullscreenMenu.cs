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
        private const string PriceCoinIconName = "Coin_Icon";

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
            Undo.RegisterFullObjectHierarchyUndo(
                panel.gameObject,
                "Make Upgrade Panel Fullscreen");

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

            PreserveUpgradeCardVisualSizing(panel);

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

        private static void MatchCardContentToFirstCard(RectTransform panel)
        {
            RectTransform template = panel.Find(CardNames[0]) as RectTransform;
            if (template == null)
            {
                Debug.LogWarning(
                    $"'{CardNames[0]}' is missing, so card content sizes could not be synchronized.",
                    panel);
                return;
            }

            for (int index = 1; index < CardNames.Length; index++)
            {
                RectTransform target = panel.Find(CardNames[index]) as RectTransform;
                if (target == null)
                {
                    continue;
                }

                CopyMatchingChildLayout(template, target);
                AlignExtraIconDecoration(target);
            }
        }

        private static void CopyMatchingChildLayout(Transform templateParent, Transform targetParent)
        {
            foreach (Transform templateChild in templateParent)
            {
                Transform targetChild = targetParent.Find(templateChild.name);
                if (targetChild == null)
                {
                    continue;
                }

                if (templateChild is RectTransform templateRect &&
                    targetChild is RectTransform targetRect)
                {
                    CopyRectTransformLayout(templateRect, targetRect);
                }

                TMPro.TextMeshProUGUI templateText =
                    templateChild.GetComponent<TMPro.TextMeshProUGUI>();
                TMPro.TextMeshProUGUI targetText =
                    targetChild.GetComponent<TMPro.TextMeshProUGUI>();
                if (templateText != null && targetText != null)
                {
                    CopyTextSizing(templateText, targetText);
                }

                CopyMatchingChildLayout(templateChild, targetChild);
            }
        }

        private static void CopyRectTransformLayout(
            RectTransform template,
            RectTransform target)
        {
            target.anchorMin = template.anchorMin;
            target.anchorMax = template.anchorMax;
            target.pivot = template.pivot;
            target.anchoredPosition = template.anchoredPosition;
            target.sizeDelta = template.sizeDelta;
            target.localRotation = template.localRotation;
            target.localScale = template.localScale;
            EditorUtility.SetDirty(target);
        }

        private static void CopyTextSizing(
            TMPro.TextMeshProUGUI template,
            TMPro.TextMeshProUGUI target)
        {
            target.enableAutoSizing = template.enableAutoSizing;
            target.fontSize = template.fontSize;
            target.fontSizeMin = template.fontSizeMin;
            target.fontSizeMax = template.fontSizeMax;
            target.fontStyle = template.fontStyle;
            target.alignment = template.alignment;
            target.margin = template.margin;
            target.characterSpacing = template.characterSpacing;
            target.wordSpacing = template.wordSpacing;
            target.lineSpacing = template.lineSpacing;
            EditorUtility.SetDirty(target);
        }

        private static void AlignExtraIconDecoration(RectTransform card)
        {
            RectTransform icon = card.Find("Icon") as RectTransform;
            RectTransform medal = card.Find("Brass_Medal") as RectTransform;
            if (icon == null || medal == null)
            {
                return;
            }

            medal.anchorMin = icon.anchorMin;
            medal.anchorMax = icon.anchorMax;
            medal.pivot = icon.pivot;
            medal.anchoredPosition = icon.anchoredPosition;
            medal.localScale = icon.localScale;
            EditorUtility.SetDirty(medal);
        }

        internal static void PreserveUpgradeCardVisualSizing(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            MatchCardContentToFirstCard(panel);
            EnsureUpgradePriceIcons(panel);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            EditorUtility.SetDirty(panel);
        }

        private static void EnsureUpgradePriceIcons(RectTransform panel)
        {
            Sprite coinSprite = FindExistingCoinSprite(panel);
            if (coinSprite == null)
            {
                Debug.LogWarning(
                    "No existing coin sprite was found under Upgrade Panel/Header. " +
                    "Price text was cleaned, but Coin_Icon images were not created.",
                    panel);
                return;
            }

            foreach (string cardName in CardNames)
            {
                RectTransform card = panel.Find(cardName) as RectTransform;
                RectTransform plaque = card != null
                    ? card.Find("Price_Plaque") as RectTransform
                    : null;
                if (plaque == null)
                {
                    continue;
                }

                UnityEngine.UI.Image coinImage = EnsureCoinImage(plaque, coinSprite);
                LayoutPriceContents(plaque, coinImage);
                WirePriceCoinIcon(card, coinImage);
            }
        }

        private static Sprite FindExistingCoinSprite(RectTransform panel)
        {
            Transform headerCoin = panel.Find("Header/Wallet_Well/Coin");
            UnityEngine.UI.Image headerCoinImage = headerCoin != null
                ? headerCoin.GetComponent<UnityEngine.UI.Image>()
                : null;
            if (headerCoinImage != null && headerCoinImage.sprite != null)
            {
                return headerCoinImage.sprite;
            }

            UnityEngine.UI.Image[] images =
                panel.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (UnityEngine.UI.Image image in images)
            {
                if (image != null && image.name == "Coin" && image.sprite != null)
                {
                    return image.sprite;
                }
            }

            return null;
        }

        private static UnityEngine.UI.Image EnsureCoinImage(
            RectTransform plaque,
            Sprite coinSprite)
        {
            Transform existing = plaque.Find(PriceCoinIconName);
            UnityEngine.UI.Image coinImage = existing != null
                ? existing.GetComponent<UnityEngine.UI.Image>()
                : null;

            if (coinImage == null)
            {
                var coinObject = new GameObject(
                    PriceCoinIconName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(UnityEngine.UI.Image));
                Undo.RegisterCreatedObjectUndo(coinObject, "Create Upgrade Price Coin Icon");
                coinObject.transform.SetParent(plaque, false);
                coinImage = coinObject.GetComponent<UnityEngine.UI.Image>();
            }

            coinImage.sprite = coinSprite;
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;
            coinImage.color = Color.white;
            EditorUtility.SetDirty(coinImage);
            return coinImage;
        }

        private static void LayoutPriceContents(
            RectTransform plaque,
            UnityEngine.UI.Image coinImage)
        {
            RectTransform coinRect = coinImage.rectTransform;
            coinRect.anchorMin = new Vector2(0.5f, 0.5f);
            coinRect.anchorMax = new Vector2(0.5f, 0.5f);
            coinRect.pivot = new Vector2(0.5f, 0.5f);
            coinRect.anchoredPosition = new Vector2(-42f, -10f);
            coinRect.sizeDelta = new Vector2(44f, 44f);
            coinRect.localScale = Vector3.one;
            EditorUtility.SetDirty(coinRect);

            RectTransform cost = plaque.Find("Cost") as RectTransform;
            if (cost != null)
            {
                cost.anchorMin = new Vector2(0.5f, 0.5f);
                cost.anchorMax = new Vector2(0.5f, 0.5f);
                cost.pivot = new Vector2(0.5f, 0.5f);
                cost.anchoredPosition = new Vector2(24f, -10f);
                cost.sizeDelta = new Vector2(126f, 34f);
                cost.localScale = Vector3.one;
                TMPro.TextMeshProUGUI costText = cost.GetComponent<TMPro.TextMeshProUGUI>();
                if (costText != null)
                {
                    costText.text = costText.text
                        .Replace("●", string.Empty)
                        .Replace("•", string.Empty)
                        .Replace("🪙", string.Empty)
                        .Trim();
                    costText.enableAutoSizing = false;
                    costText.fontSize = 20f;
                    costText.alignment = TMPro.TextAlignmentOptions.Center;
                    EditorUtility.SetDirty(costText);
                }
                EditorUtility.SetDirty(cost);
            }

            RectTransform caption = plaque.Find("Buy_Caption") as RectTransform;
            if (caption != null)
            {
                caption.anchoredPosition = new Vector2(0f, 24f);
                caption.sizeDelta = new Vector2(150f, 22f);
                caption.localScale = Vector3.one;
                TMPro.TextMeshProUGUI captionText =
                    caption.GetComponent<TMPro.TextMeshProUGUI>();
                if (captionText != null)
                {
                    captionText.enableAutoSizing = false;
                    captionText.fontSize = 12f;
                    captionText.alignment = TMPro.TextAlignmentOptions.Center;
                    EditorUtility.SetDirty(captionText);
                }
                EditorUtility.SetDirty(caption);
            }
        }

        private static void WirePriceCoinIcon(
            RectTransform card,
            UnityEngine.UI.Image coinImage)
        {
            JuicyUpgradeItem item = card.GetComponent<JuicyUpgradeItem>();
            if (item == null)
            {
                return;
            }

            var serializedItem = new SerializedObject(item);
            SerializedProperty property = serializedItem.FindProperty("priceCoinIcon");
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = coinImage;
            serializedItem.ApplyModifiedProperties();
            EditorUtility.SetDirty(item);
        }
    }
}
