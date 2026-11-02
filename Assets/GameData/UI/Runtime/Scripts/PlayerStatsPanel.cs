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
        [SerializeField] private UiPanelCoordinator coordinator;
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
            EnsureProjectileSpeedRow();
            if (panel != null) panel.gameObject.SetActive(false);
        }
        private void OnEnable()
        {
            EnsureProjectileSpeedRow();
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            MiningLocalization.LanguageChanged += Refresh;
            Refresh();
        }
        private void EnsureProjectileSpeedRow()
        {
            if(statRows==null)return;
            StatRow source=null;
            foreach(var row in statRows){if(row==null)continue;if(row.key=="PROJECTILE_SPEED")return;if(row.key=="RANGE")source=row;}
            if(source==null||!source.label||!source.value)return;
            var parent=source.label.transform.parent;
            var clone=Instantiate(parent.gameObject,parent.parent);
            clone.name="Stat Row Projectile Speed";
            string RelativePath(Transform child){string path=child.name;for(var t=child.parent;t&&t!=parent;t=t.parent)path=t.name+"/"+path;return path;}
            var labelTransform=clone.transform.Find(RelativePath(source.label.transform));
            var valueTransform=clone.transform.Find(RelativePath(source.value.transform));
            if(!labelTransform||!valueTransform){Destroy(clone);return;}
            var newRow=new StatRow{key="PROJECTILE_SPEED",label=labelTransform.GetComponent<TMP_Text>(),value=valueTransform.GetComponent<TMP_Text>()};
            System.Array.Resize(ref statRows,statRows.Length+1);statRows[statRows.Length-1]=newRow;
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
                Row(text,"DAMAGE","Base / Total DMG",attack?$"{attack.BaseDamage:0.##} / {attack.EffectiveDamage:0.##}":player.Damage.ToString("0.##"));
                Row(text,"ATTACK_SPEED","Fire rate",attack?$"{attack.ShotsPerSecond:0.##} /s":player.AttackSpeed.ToString("0.##"));
                Row(text,"MOVEMENT_SPEED","Walk / Run",$"{player.WalkSpeed:0.##} / {player.RunSpeed:0.##} m/s");
                Row(text,"RANGE","Attack range",attack?$"{attack.AttackRange:0.##} m":"—");
                Row(text,"PROJECTILE_SPEED","Projectile speed",attack?$"{attack.ProjectileSpeed:0.##} m/s":"—");
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
                var ability=player.GetComponent<GameData.Player.Script.SurvivorAbilities>();
                if(statsFooter)statsFooter.text=attack?$"Đạn: {attack.ProjectileCount}/loạt • Kích thước: {attack.ProjectileRadius*2:0.##} m • Khiên: {(ability?ability.Shield:0):0.##}":"LIVE TOTALS • INCLUDING EQUIPMENT";
                string[] left={"DAMAGE","ATTACK_SPEED","RANGE","PROJECTILE_SPEED","MOVEMENT_SPEED","STAMINA"};
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
                    if(a>=0&&leftStart)rect.anchoredPosition=leftStart.anchoredPosition+new Vector2(0,-62*a);
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

