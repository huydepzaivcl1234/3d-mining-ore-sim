using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    public sealed class MiningPlayerStatsPanel : MonoBehaviour
    {
        [SerializeField] private MiningPlayerStats player;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button openButton, closeButton;
        [SerializeField] private TMP_Text title, body, openLabel, closeLabel;
        [SerializeField] private MiningUiPanelCoordinator coordinator;
        private float nextRefresh;
        private void Awake() { if (panel != null) panel.gameObject.SetActive(false); }
        private void OnEnable()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            MiningLocalization.LanguageChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            MiningLocalization.LanguageChanged -= Refresh;
        }
        private void Update()
        {
            if (panel == null || !panel.gameObject.activeInHierarchy || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh();
        }
        public void Open()
        {
            if (panel == null) return;
            Refresh();
            if (coordinator != null) coordinator.OpenPanel(panel);
            else panel.gameObject.SetActive(true);
        }
        public void Close()
        {
            if (panel == null) return;
            if (coordinator != null) coordinator.ClosePanel(panel);
            else panel.gameObject.SetActive(false);
        }
        private void Refresh()
        {
            if (title != null) title.text = L("PLAYER_STATS_TITLE", "PLAYER STATS");
            if (openLabel != null) openLabel.text = L("PLAYER_STATS_BUTTON", "STATS");
            if (closeLabel != null) closeLabel.text = L("PLAYER_STATS_CLOSE", "CLOSE");
            if (player == null || player.Data == null || body == null) return;
            var d = player.Data;
            var health = player.GetComponent<MiningCharacterHealth>();
            var combat = player.GetComponent<PlayerCombatInput>();
            var text = new StringBuilder();
            Row(text, "HEALTH", "Health", health != null ? $"{health.Health:0.##} / {health.MaxHealth:0.##}" : d.maxHealth.ToString("0.##"));
            Row(text, "REGEN", "HP regeneration", $"{d.regenAmount:0.##} / {d.regenInterval:0.##}s");
            Row(text, "DAMAGE", "Damage", (combat != null ? combat.Damage : d.damage).ToString("0.##"));
            Row(text, "RANGE", "Attack range", $"{(combat != null ? combat.AttackRange : d.attackRange):0.##}m");
            Row(text, "ANGLE", "Attack angle", $"{d.attackAngle:0.##}");
            Row(text, "ATTACK_SPEED", "Attack animation speed", $"x{d.attackSpeed:0.##}");
            Row(text, "HIT_TIME", "Attack contact", $"{d.hitTime * 100:0.##}%");
            Row(text, "BLEND", "Combat blend", $"{d.combatBlendSeconds:0.##}s");
            Row(text, "WALK", "Walk speed", $"{d.MoveSpeed:0.##}m/s");
            Row(text, "SPRINT", "Sprint speed", $"{d.SprintSpeed:0.##}m/s");
            Row(text, "ACCELERATION", "Speed change rate", d.SpeedChangeRate.ToString("0.##"));
            Row(text, "TURN", "Turn smoothing", $"{d.RotationSmoothTime:0.###}s");
            Row(text, "JUMP", "Jump height", $"{d.JumpHeight:0.##}m");
            Row(text, "GRAVITY", "Gravity", d.Gravity.ToString("0.##"));
            Row(text, "JUMP_DELAY", "Jump cooldown", $"{d.JumpTimeout:0.##}s");
            Row(text, "FALL_DELAY", "Fall delay", $"{d.FallTimeout:0.##}s");
            Row(text, "RESPAWN", "Respawn delay", $"{d.respawnSeconds:0.##}s");
            body.text = text.ToString();
        }
        private static string L(string key, string fallback) => MiningLocalization.TextKey(key, fallback);
        private static void Row(StringBuilder text, string key, string fallback, string value)
            => text.Append(L("PLAYER_STATS_" + key, fallback)).Append(":  ").Append(value).Append('\n');
    }
}
