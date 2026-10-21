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
        [SerializeField] private TMP_Text levelLabel, experienceLabel;
        [SerializeField] private UnityEngine.UI.Slider experienceBar;
        [SerializeField] private MiningUiPanelCoordinator coordinator;
        [System.Serializable]
        private sealed class StatRow
        {
            public string key;
            public TMP_Text label, value;
        }
        [SerializeField] private StatRow[] statRows;
        [SerializeField] private TMP_Text offenseHeading, survivalHeading, statsFooter;
        private PlayerCombatInput combat;
        private float nextRefresh;
        private void Awake()
        {
            if (player != null) combat = player.GetComponent<PlayerCombatInput>();
            if (panel != null) panel.gameObject.SetActive(false);
        }
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
            if (offenseHeading != null) offenseHeading.text = L("PLAYER_STATS_OFFENSE", "OFFENSE & MOVEMENT");
            if (survivalHeading != null) survivalHeading.text = L("PLAYER_STATS_SURVIVAL", "SURVIVAL");
            if (statsFooter != null) statsFooter.text = L("PLAYER_STATS_TOTALS", "CURRENT STATS • INCLUDING EQUIPMENT BONUSES");
            if (title != null) title.text = L("PLAYER_STATS_TITLE", "PLAYER STATS");
            if (openLabel != null) openLabel.text = L("PLAYER_STATS_BUTTON", "STATS");
            if (closeLabel != null) closeLabel.text = L("PLAYER_STATS_CLOSE", "CLOSE");
            if (player == null || player.Data == null) return;
            var d = player.Data;
            if (levelLabel != null) levelLabel.text = $"{L("PLAYER_STATS_LEVEL", "Lv.")} {player.Level}";
            if (experienceLabel != null) experienceLabel.text = $"{player.Experience:0.##} / {player.ExperienceRequired:0.##} XP";
            if (experienceBar != null) experienceBar.SetValueWithoutNotify(player.ExperienceProgress);
            var text = new StringBuilder();
            // Existing scenes can still show level/XP before running the layout update.
            if (levelLabel == null) Row(text, "LEVEL", "Lv.", player.Level.ToString());
            if (experienceLabel == null) text.Append($"{player.Experience:0.##} / {player.ExperienceRequired:0.##} XP\n\n");
            Row(text, "DAMAGE", "Damage", (combat != null ? combat.Damage : player.Damage).ToString("0.##"));
            Row(text, "ATTACK_SPEED", "Attack speed", $"x{(combat != null ? combat.AttackSpeed : player.AttackSpeed):0.##}");
            Row(text, "MOVEMENT_SPEED", "Movement speed", $"{d.MoveSpeed:0.##}m/s");
            Row(text, "RANGE", "Attack range", $"{(combat != null ? combat.AttackRange : d.attackRange):0.##}m");
            var stamina = player.GetComponent<MiningPlayerStamina>();
            Row(text, "STAMINA", "Stamina", $"{(stamina != null ? stamina.Current : d.maxStamina):0} / {d.maxStamina:0}");
            var health = player.GetComponent<MiningCharacterHealth>();
            if (health != null)
            {
                Row(text, "HEALTH", "Health", $"{health.Health:0.##} / {health.MaxHealth:0.##}");
                Row(text, "ARMOR", "Armor", $"{health.Armor:0.##} (-{CombatDamage.ReductionFraction(health.Armor, health.ResistanceScale) * 100f:0.##}%)");
                Row(text, "MAGIC_RESISTANCE", "Magic resistance", $"{health.MagicResistance:0.##} (-{CombatDamage.ReductionFraction(health.MagicResistance, health.ResistanceScale) * 100f:0.##}%)");
                Row(text, "REGEN_SPEED", "Regeneration speed", string.Format(
                    L("PLAYER_STATS_REGEN_RATE_FORMAT", "{0:0.##} HP/s"),
                    health.EffectiveRegenAmount / health.RegenInterval));
                Row(text, "REGEN_AMOUNT", "Health regenerated", string.Format(
                    L("PLAYER_STATS_REGEN_TICK_FORMAT", "{0:0.##} HP every {1:0.##}s"),
                    health.EffectiveRegenAmount, health.RegenInterval));
            }
            if (body != null) body.text = text.ToString().TrimEnd('\n');
        }
        private static string L(string key, string fallback) => MiningLocalization.TextKey(key, fallback);
        private void Row(StringBuilder text, string key, string fallback, string value)
        {
            var label = L("PLAYER_STATS_" + key, fallback);
            // The legacy body remains a fallback for scenes that have not applied the new layout.
            text.Append(label).Append(":  ").Append(value).Append('\n');
            if (statRows == null) return;
            foreach (var row in statRows)
            {
                if (row == null || row.key != key) continue;
                if (row.label != null) row.label.text = label;
                if (row.value != null) row.value.text = value;
            }
        }
    }
}
