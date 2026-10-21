using TMPro;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.UI;
using MiningSimulator.Ores;

namespace ChestDefense.Fps
{
    [DefaultExecutionOrder(200)]
    public sealed class FpsWeaponHud : MonoBehaviour
    {
        public FpsWeaponController weapons;
        public CanvasGroup group;
        public TMP_Text weaponName, ammo, hint;
        public Image reloadFill;
        public RectTransform crosshair;
        public CanvasGroup hitMarker;
        public Color normalColor = new Color32(255, 244, 218, 255), emptyColor = new Color32(246, 98, 91, 255);
        public float hitMarkerSeconds = .12f, hipCrosshairSize = 24f, aimCrosshairSize = 12f;
        [Header("Ammo card — independent of player HP HUD")]
        public bool applyAmmoLayout = true;
        public Vector2 ammoCardSize = new Vector2(420, 110);
        public Vector2 ammoCardOffset = new Vector2(0, 205);
        public float weaponFontSize = 26, ammoFontSize = 34, hintFontSize = 18;
        [Tooltip("Dedicated FPS font material; never modifies the shared menu font material.")]
        public Material textMaterial;
        float hitUntil;
        bool wasVisible;
        void Awake()
        {
            if (textMaterial == null) textMaterial = Resources.Load<Material>("FpsHudText");
            foreach (var text in new[] { weaponName, ammo, hint })
                if (text != null && text.font != null && textMaterial != null &&
                    textMaterial.GetTexture("_MainTex") == text.font.atlasTexture)
                    text.fontSharedMaterial = textMaterial;
            ApplyAmmoLayout();
        }
        public void ApplyAmmoLayout()
        {
            if (!applyAmmoLayout || weaponName == null) return;
            var card = weaponName.transform.parent as RectTransform;
            if (card == null) return;
            card.anchorMin = card.anchorMax = new Vector2(.5f, 0);
            card.pivot = new Vector2(.5f, .5f);
            card.sizeDelta = ammoCardSize; card.anchoredPosition = ammoCardOffset;
            ConfigureText(weaponName, new Vector2(-100, 28), new Vector2(180, 38), weaponFontSize, TextAlignmentOptions.MidlineLeft);
            ConfigureText(ammo, new Vector2(100, 28), new Vector2(180, 44), ammoFontSize, TextAlignmentOptions.MidlineRight);
            ConfigureText(hint, new Vector2(0, -20), new Vector2(390, 44), hintFontSize, TextAlignmentOptions.Midline);
            if (reloadFill != null) { reloadFill.rectTransform.sizeDelta = new Vector2(ammoCardSize.x - 30, 4); reloadFill.rectTransform.anchoredPosition = new Vector2(0, -48); }
        }
        static void ConfigureText(TMP_Text text, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            if (text == null) return;
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(.5f, .5f);
            text.rectTransform.anchoredPosition = position; text.rectTransform.sizeDelta = size;
            text.fontSize = fontSize; text.alignment = alignment; text.raycastTarget = false;
        }
        void OnEnable()
        {
            if (weapons != null) { weapons.Changed += Refresh; weapons.HitConfirmed += OnHit; }
            MiningLocalization.LanguageChanged += Refresh; Refresh();
        }
        void OnDisable()
        {
            if (weapons != null) { weapons.Changed -= Refresh; weapons.HitConfirmed -= OnHit; }
            MiningLocalization.LanguageChanged -= Refresh;
        }
        void OnHit(float damage) { hitUntil = Time.unscaledTime + hitMarkerSeconds; }
        void LateUpdate()
        {
            bool visible = weapons != null && weapons.HasWeapon && weapons.cameraRig != null && weapons.cameraRig.OwnsView &&
                (weapons.panels == null || !weapons.panels.BlocksGameplay);
            if (group != null) { group.alpha = visible ? 1 : 0; group.interactable = false; group.blocksRaycasts = false; }
            if (visible && !wasVisible) Refresh();
            wasVisible = visible;
            if (!visible) return;
            var m = weapons.CurrentMagazine;
            if (reloadFill != null)
            {
                reloadFill.gameObject.SetActive(m.Reloading);
                reloadFill.fillAmount = m.Reloading ? (float)((Time.timeAsDouble - m.ReloadStarted) / (m.ReloadEnds - m.ReloadStarted)) : 0;
            }
            if (crosshair != null) { crosshair.gameObject.SetActive(!weapons.FreeCursor); crosshair.sizeDelta = Vector2.one * (weapons.IsAiming ? aimCrosshairSize : hipCrosshairSize); }
            if (hitMarker != null) hitMarker.alpha = Time.unscaledTime < hitUntil && !weapons.FreeCursor ? 1 : 0;
        }
        void Refresh()
        {
            if (weapons == null || !weapons.HasWeapon) return;
            var m = weapons.CurrentMagazine;
            if (weaponName != null) weaponName.text = weapons.CurrentWeapon.LocalizedName;
            if (ammo != null) { ammo.text = m.Loaded + " / " + m.Reserve; ammo.color = m.Loaded == 0 ? emptyColor : normalColor; }
            if (hint != null)
                hint.text = m.Reloading ? MiningLocalization.TextKey("FPS_RELOADING", "RELOADING") :
                    string.Format(MiningLocalization.TextKey("FPS_HINT", "{0} Reload · {1} Switch · Hold {2} for cursor"),
                        weapons.reload.GetBindingDisplayString(), weapons.nextWeapon.GetBindingDisplayString(), weapons.freeCursor.GetBindingDisplayString());
        }
    }
}
