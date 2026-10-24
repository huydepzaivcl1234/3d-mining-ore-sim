using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    public sealed class MiningPlayerStaminaHud : MonoBehaviour
    {
        [SerializeField] private MiningPlayerStamina player;
        [SerializeField] private Slider bar;
        [SerializeField] private TMP_Text label;
        private Color normalColor;
        private MiningPlayerStats stats;
        private MiningUiPanelCoordinator coordinator;
        private MiningMainMenu mainMenu;
        private Graphic[] graphics;
        private bool[] authoredEnabled;
        private bool visualsHidden;
        private void Awake()
        {
            normalColor = label != null ? label.color : Color.white;
            if (player != null) stats = player.GetComponent<MiningPlayerStats>();
            coordinator = FindFirstObjectByType<MiningUiPanelCoordinator>(FindObjectsInactive.Include);
            mainMenu = FindFirstObjectByType<MiningMainMenu>(FindObjectsInactive.Include);
            graphics = GetComponentsInChildren<Graphic>(true);
            authoredEnabled = new bool[graphics.Length];
            for (int i = 0; i < graphics.Length; i++) authoredEnabled[i] = graphics[i].enabled;
        }
        private void OnDisable()
        {
            if (label != null) label.color = normalColor;
            SetVisualsHidden(false);
        }
        private void LateUpdate()
        {
            // Independent of when this HUD was created or which Canvas owns it.
            // Do not write the coordinator's CanvasGroup alpha or stop this component's updates.
            SetVisualsHidden((coordinator != null && coordinator.BlocksGameplay) ||
                (mainMenu != null && mainMenu.IsOpen));
        }
        private void SetVisualsHidden(bool hidden)
        {
            if (graphics == null || visualsHidden == hidden) return;
            visualsHidden = hidden;
            for (int i = 0; i < graphics.Length; i++)
                if (graphics[i] != null) graphics[i].enabled = !hidden && authoredEnabled[i];
        }
        private void Update()
        {
            if (player == null) return;
            if (bar != null) bar.SetValueWithoutNotify(player.Current / player.Maximum);
            if (label != null)
            {
                label.text = $"{MiningLocalization.TextKey("PLAYER_STATS_STAMINA", "Stamina")} {Mathf.Ceil(player.Current):0} / {player.Maximum:0}";
                var data = stats != null ? stats.Data : null;
                bool low = player.Current / player.Maximum <= (data != null ? Mathf.Clamp01(data.lowStaminaFraction) : 0.2f);
                if (!low) label.color = normalColor;
                else
                {
                    Color warning = data != null ? data.lowStaminaTextColor : Color.red;
                    float period = data != null ? Mathf.Max(0.1f, data.lowStaminaBlinkSeconds) : 0.7f;
                    warning.a = normalColor.a * Mathf.Lerp(0.25f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / period));
                    label.color = warning;
                }
            }
        }
    }
}
