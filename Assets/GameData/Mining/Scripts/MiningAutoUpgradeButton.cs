using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed class MiningAutoUpgradeButton : MonoBehaviour
    {
        [Tooltip("Pinned above the scroll viewport, so the station/player cannot cover it.")]
        [SerializeField] private Vector2 position = new(0f, 490f);
        [SerializeField] private Vector2 size = new(1100f, 90f);
        [SerializeField, Min(1f)] private float fontSize = 44f;
        private MiningUpgradeSystem system;
        private UnityEngine.UI.Button button;
        private TMP_Text label;
        private Material labelMaterial;

        public void Configure(MiningUpgradeSystem owner)
        {
            Unsubscribe();
            system = owner;
            if (button == null)
            {
                var go = new GameObject("Auto Upgrade", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.anchoredPosition = position; rect.sizeDelta = size;
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.color = new Color(.04f, .25f, .3f, .96f);
                button = go.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
                var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(rect, false);
                var textRect = (RectTransform)textGo.transform;
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(15, 4); textRect.offsetMax = new Vector2(-15, -4);
                label = textGo.GetComponent<TMP_Text>();
                var fontSource = GetComponentInChildren<TMP_Text>(true);
                if (fontSource != null && fontSource != label) label.font = fontSource.font;
                label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                label.color = Color.white;
                // The project's Square font uses a black face material. A white vertex
                // color cannot brighten that; give this label its own white face.
                if (label.fontSharedMaterial != null)
                {
                    labelMaterial = new Material(label.fontSharedMaterial);
                    labelMaterial.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
                    label.fontSharedMaterial = labelMaterial;
                }
                go.AddComponent<MiningButtonSfx>();
            }
            button.gameObject.SetActive(true);
            button.transform.SetAsLastSibling();
            Subscribe(); Refresh();
        }
        private void OnEnable() { Subscribe(); Refresh(); }
        private void OnDisable() => Unsubscribe();
        private void OnDestroy()
        {
            Unsubscribe();
            if (labelMaterial != null) Destroy(labelMaterial);
        }
        private void Subscribe()
        {
            if (system != null) { system.AutoUpgradeChanged -= Refresh; system.AutoUpgradeChanged += Refresh; }
            MiningLocalization.LanguageChanged -= Refresh; MiningLocalization.LanguageChanged += Refresh;
            if (button != null) { button.onClick.RemoveListener(Click); button.onClick.AddListener(Click); }
        }
        private void Unsubscribe()
        {
            if (system != null) system.AutoUpgradeChanged -= Refresh;
            MiningLocalization.LanguageChanged -= Refresh;
            if (button != null) button.onClick.RemoveListener(Click);
        }
        private void Click()
        {
            if (system == null) return;
            if (!system.AutoUpgradeUnlocked) system.TryUnlockAutoUpgrade();
            else system.SetAutoUpgradeEnabled(!system.AutoUpgradeEnabled);
            Refresh();
        }
        private void Refresh()
        {
            if (label == null || system == null || system.UpgradeData == null) return;
            label.text = !system.AutoUpgradeUnlocked
                ? string.Format(MiningLocalization.TextKey("AUTO_UPGRADE_UNLOCK", "Unlock auto upgrade • {0} gems"), system.UpgradeData.AutoUpgradeGemCost)
                : MiningLocalization.TextKey(system.AutoUpgradeEnabled ? "AUTO_UPGRADE_ON" : "AUTO_UPGRADE_OFF",
                    system.AutoUpgradeEnabled ? "Auto upgrade: ON (Money)" : "Auto upgrade: OFF");
        }
    }
}
