using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent]
    public sealed class TreasureChestDefenseFeedback : MonoBehaviour
    {
        private TreasureChest chest;
        private MiningPlayerStats player;
        private MiningCharacterHealth playerHealth;
        private MiningUiPanelCoordinator panels;
        private Camera viewer;
        private float heldSeconds, nextLookup;
        private bool interrupted;
        private RectTransform repair, warning;
        private GameObject overlay;
        private TextMeshProUGUI repairText, warningText, arrow;
        private UnityEngine.UI.Image repairFill;
        public float RepairProgress => chest != null && chest.Data != null
            ? Mathf.Clamp01(heldSeconds / Mathf.Max(.1f, chest.Data.emergencyRepairSeconds)) : 0f;

        public static bool ClaimsRepairKey(Transform actor)
        {
            var owner = TreasureChest.Active;
            return owner != null && owner.IsBroken && owner.Data != null && actor != null &&
                Vector3.Distance(actor.position, owner.transform.position) <= owner.Data.emergencyRepairRange;
        }

        private void Awake() => chest = GetComponent<TreasureChest>();

        private void Start()
        {
            if (chest == null || chest.Data == null) { enabled = false; return; }
            repair = CreateCard("Hold E repair", transform, new Vector2(400, 60), out repairText);
            var world = repair.gameObject.AddComponent<Canvas>();
            world.renderMode = RenderMode.WorldSpace;
            repairFill = new GameObject("Repair progress", typeof(RectTransform), typeof(UnityEngine.UI.Image))
                .GetComponent<UnityEngine.UI.Image>();
            repairFill.transform.SetParent(repair, false);
            repairFill.color = new Color(.08f, .75f, .55f); repairFill.raycastTarget = false;
            repairFill.rectTransform.anchorMin = new Vector2(0, 0);
            repairFill.rectTransform.anchorMax = new Vector2(0, 0);
            repairFill.rectTransform.pivot = Vector2.zero;
            repairFill.rectTransform.anchoredPosition = new Vector2(5, 5);
            repairFill.rectTransform.sizeDelta = new Vector2(0, 5);
            overlay = new GameObject("Chest distant threat", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            overlay.transform.SetParent(transform, false);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30;
            warning = CreateCard("Chest warning", overlay.transform, new Vector2(210, 58), out warningText);
            arrow = CreateText("Direction", warning, new Vector2(30, 30));
            arrow.text = ">"; arrow.rectTransform.anchoredPosition = new Vector2(-85, 0);
            warningText.rectTransform.sizeDelta = new Vector2(155, 50);
            warningText.rectTransform.anchoredPosition = new Vector2(16, 0);
            repair.gameObject.SetActive(false); warning.gameObject.SetActive(false);
        }

        private void ResolvePlayer()
        {
            if (player != null || Time.unscaledTime < nextLookup) return;
            nextLookup = Time.unscaledTime + 1;
            player = FindFirstObjectByType<MiningPlayerStats>();
            panels = FindFirstObjectByType<MiningUiPanelCoordinator>();
            if (player == null) return;
            playerHealth = player.GetComponent<MiningCharacterHealth>();
            if (playerHealth != null) playerHealth.Damaged += Interrupt;
        }

        private void Interrupt() { heldSeconds = 0; interrupted = true; }

        private void Update()
        {
            if (chest == null || chest.Data == null || repair == null) return;
            ResolvePlayer();
            bool down = Keyboard.current != null && Keyboard.current.eKey.isPressed;
            if (!down) interrupted = false;
            bool eligible = player != null && playerHealth != null && playerHealth.Health > 0 &&
                ClaimsRepairKey(player.transform) && (panels == null || !panels.BlocksGameplay) &&
                !RuneStation.PlayerUsesRuneTime && Time.timeScale > 0;
            if (!eligible || !down || interrupted) heldSeconds = 0;
            else
            {
                heldSeconds += Time.deltaTime;
                if (heldSeconds >= Mathf.Max(.1f, chest.Data.emergencyRepairSeconds))
                { chest.EmergencyRepair(); heldSeconds = 0; }
            }
            repair.gameObject.SetActive(eligible && chest.IsBroken);
            if (repair.gameObject.activeSelf)
            {
                repairText.text = MiningLocalization.Text("Hold E to repair", "Giữ E để sửa") +
                    $"  {RepairProgress:P0}";
                repairFill.rectTransform.sizeDelta = new Vector2(390 * RepairProgress, 5);
            }
        }

        private void LateUpdate()
        {
            if (repair == null || warning == null || chest == null || chest.Data == null) return;
            if (viewer == null || !viewer.isActiveAndEnabled) viewer = Camera.main;
            if (viewer == null) { warning.gameObject.SetActive(false); repair.gameObject.SetActive(false); return; }
            if (repair.gameObject.activeSelf)
            {
                repair.position = transform.position + chest.Data.panelOffset + Vector3.down * 1.7f;
                repair.rotation = viewer.transform.rotation;
                Vector3 scale = transform.lossyScale;
                float unit = chest.Data.panelWorldScale;
                repair.localScale = new Vector3(unit / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                    unit / Mathf.Max(.001f, Mathf.Abs(scale.y)), unit / Mathf.Max(.001f, Mathf.Abs(scale.z)));
                repair.GetComponent<Canvas>().worldCamera = viewer;
            }
            bool distant = player != null && Vector3.Distance(player.transform.position, transform.position) >
                Mathf.Max(chest.Data.panelNearDistance, chest.Data.panelHideDistance);
            bool show = chest.isActiveAndEnabled && distant && playerHealth != null && playerHealth.Health > 0 &&
                Time.time - chest.LastAttackTime < Mathf.Max(.1f, chest.Data.threatWarningSeconds) &&
                (panels == null || !panels.BlocksGameplay);
            warning.gameObject.SetActive(show);
            if (!show) return;
            Vector3 projected = viewer.WorldToScreenPoint(transform.position + chest.Data.panelOffset);
            Vector2 direction = new Vector2(projected.x - Screen.width * .5f, projected.y - Screen.height * .5f);
            if (projected.z < 0) direction = -direction;
            warning.position = EdgePosition(direction, Screen.safeArea, new Vector2(115, 45));
            arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            warningText.text = MiningLocalization.Text("CHEST UNDER ATTACK", "RƯƠNG BỊ TẤN CÔNG") +
                $"\n{chest.Health.Health:0} / {chest.Health.MaxHealth:0} HP";
        }

        public static Vector2 EdgePosition(Vector2 direction, Rect safeArea, Vector2 inset)
        {
            if (direction.sqrMagnitude < .001f) direction = Vector2.down;
            Vector2 half = new Vector2(Mathf.Max(1, safeArea.width * .5f - inset.x),
                Mathf.Max(1, safeArea.height * .5f - inset.y));
            float reach = Mathf.Min(half.x / Mathf.Max(.001f, Mathf.Abs(direction.x)),
                half.y / Mathf.Max(.001f, Mathf.Abs(direction.y)));
            return safeArea.center + direction * reach;
        }

        private RectTransform CreateCard(string title, Transform parent, Vector2 size, out TextMeshProUGUI text)
        {
            var card = new GameObject(title, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            card.SetParent(parent, false); card.sizeDelta = size;
            var background = card.GetComponent<UnityEngine.UI.Image>();
            background.sprite = chest.Data.levelBadge; background.type = UnityEngine.UI.Image.Type.Sliced;
            background.color = new Color32(255, 221, 137, 245); background.raycastTarget = false;
            text = CreateText("Label", card, size - new Vector2(12, 10));
            return card;
        }

        private TextMeshProUGUI CreateText(string title, Transform parent, Vector2 size)
        {
            var label = new GameObject(title, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(parent, false); label.rectTransform.sizeDelta = size;
            label.font = chest.Data.statusBodyFont != null ? chest.Data.statusBodyFont : chest.Data.font;
            label.fontSize = 16; label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(65, 35, 28, 255); label.raycastTarget = false;
            return label;
        }

        private void OnDisable()
        {
            heldSeconds = 0; interrupted = true;
            if (repair != null) repair.gameObject.SetActive(false);
            if (warning != null) warning.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.Damaged -= Interrupt;
            if (overlay != null) Destroy(overlay);
        }
    }
}
