using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "MiningSimulator.Ores", "Assembly-CSharp", "MiningPlayerStatsPanel")]
    public sealed class PlayerStatsPanel : MonoBehaviour
    {
        [SerializeField] private PlayerStats player;
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
        private float nextRefresh;
        private void Awake()
        {
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
            if (player == null) player = FindFirstObjectByType<PlayerStats>();
            if (offenseHeading != null) offenseHeading.text = L("PLAYER_STATS_OFFENSE", "OFFENSE & MOVEMENT");
            if (survivalHeading != null) survivalHeading.text = L("PLAYER_STATS_SURVIVAL", "SURVIVAL");
            if (statsFooter != null) statsFooter.text = L("PLAYER_STATS_TOTALS", "CURRENT STATS • INCLUDING EQUIPMENT BONUSES");
            if (title != null) title.text = L("PLAYER_STATS_TITLE", "PLAYER STATS");
            if (openLabel != null) openLabel.text = L("PLAYER_STATS_BUTTON", "STATS");
            if (closeLabel != null) closeLabel.text = L("PLAYER_STATS_CLOSE", "CLOSE");
            if (player == null) return;
            var d = player.Data;
            if (levelLabel != null) levelLabel.text = $"{L("PLAYER_STATS_LEVEL", "Lv.")} {player.Level}";
            if (experienceLabel != null) experienceLabel.text = $"{player.Experience:0.##} / {player.ExperienceRequired:0.##} XP";
            if (experienceBar != null) experienceBar.SetValueWithoutNotify(player.ExperienceProgress);
            var text = new StringBuilder();
            // Existing scenes can still show level/XP before running the layout update.
            if (levelLabel == null) Row(text, "LEVEL", "Lv.", player.Level.ToString());
            if (experienceLabel == null) text.Append($"{player.Experience:0.##} / {player.ExperienceRequired:0.##} XP\n\n");
            if (player.CoreData != null)
            {
                var core=player.CoreData;
                var sprintStamina=player.GetComponent<GameData.Player.Script.AnimationRunMana>();
                var hp=player.GetComponent<CharacterHealth>();
                var attack=player.GetComponent<GameData.Player.Script.SurvivorAutoCombat>();
                Row(text,"DAMAGE","Attack damage",attack?attack.EffectiveDamage.ToString("0.##"):player.Damage.ToString("0.##"));
                Row(text,"ATTACK_SPEED","Attacks / sec",attack?(1f/attack.AttackInterval).ToString("0.##"):player.AttackSpeed.ToString("0.##"));
                Row(text,"MOVEMENT_SPEED","Walk / Run",$"{core.walkSpeed:0.##} / {core.runSpeed:0.##} m/s");
                Row(text,"RANGE","Attack range",attack&&attack.data?$"{attack.data.range:0.##} m":"—");
                Row(text,"STAMINA","Stamina",$"{(Application.isPlaying&&sprintStamina?sprintStamina.CurrentMana:core.maxStamina):0} / {core.maxStamina:0}");
                Row(text,"HEALTH","Health",$"{(hp?hp.Health:player.MaxHealth):0.##} / {player.MaxHealth:0.##}");
                float armor=hp?hp.Armor:player.Armor, magic=hp?hp.MagicResistance:player.MagicResistance;
                float scale=hp?hp.ResistanceScale:player.ResistanceScale;
                Row(text,"ARMOR","Armor",$"{armor:0.##} (-{CombatDamage.ReductionFraction(armor,scale)*100:0.#}%)");
                Row(text,"MAGIC_RESISTANCE","Magic resistance",$"{magic:0.##} (-{CombatDamage.ReductionFraction(magic,scale)*100:0.#}%)");
                float regen=hp?hp.EffectiveRegenAmount:player.RegenAmount;
                float interval=hp?hp.RegenInterval:player.RegenInterval;
                Row(text,"REGEN_SPEED","HP regeneration",$"{regen/interval:0.##} HP/s");
                Row(text,"REGEN_AMOUNT","Healing per tick",$"{regen:0.##} HP / {interval:0.##} s");
                if(statsFooter)statsFooter.text=attack?$"LIVE TOTALS • {attack.ProjectileCount} PROJECTILE(S) • {attack.ProjectileSpeed:0.##} m/s":"LIVE TOTALS • INCLUDING EQUIPMENT";
                string[] left={"DAMAGE","ATTACK_SPEED","MOVEMENT_SPEED","RANGE","STAMINA"};
                string[] right={"HEALTH","ARMOR","MAGIC_RESISTANCE","REGEN_SPEED","REGEN_AMOUNT"};
                RectTransform leftStart=null,rightStart=null;
                if(statRows!=null)foreach(var row in statRows){
                    if(row==null||!row.label)continue;
                    if(row.key=="DAMAGE")leftStart=row.label.transform.parent as RectTransform;
                    if(row.key=="HEALTH")rightStart=row.label.transform.parent as RectTransform;
                }
                if(statRows!=null)foreach(var row in statRows){
                    if(row==null||!row.label)continue;
                    var rect=row.label.transform.parent as RectTransform;if(!rect)continue;
                    int a=System.Array.IndexOf(left,row.key),b=System.Array.IndexOf(right,row.key);
                    rect.gameObject.SetActive(a>=0||b>=0);
                    if(a>=0&&leftStart)rect.anchoredPosition=leftStart.anchoredPosition+new Vector2(0,-70*a);
                    if(b>=0&&rightStart)rect.anchoredPosition=rightStart.anchoredPosition+new Vector2(0,-70*b);
                }
                if(body)body.text=text.ToString().TrimEnd('\n');
                return;
            }
            if (d == null) return;
            Row(text, "DAMAGE", "Damage", player.Damage.ToString("0.##"));
            Row(text, "ATTACK_SPEED", "Attack speed", $"x{player.AttackSpeed:0.##}");
            Row(text, "MOVEMENT_SPEED", "Movement speed", $"{d.MoveSpeed:0.##}m/s");
            Row(text, "RANGE", "Attack range", $"{d.attackRange:0.##}m");
            var stamina = player.GetComponent<MiningPlayerStamina>();
            Row(text, "STAMINA", "Stamina", $"{(stamina != null ? stamina.Current : d.maxStamina):0} / {d.maxStamina:0}");
            var health = player.GetComponent<CharacterHealth>();
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
            var label = player != null && player.CoreData != null ? fallback : L("PLAYER_STATS_" + key, fallback);
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

