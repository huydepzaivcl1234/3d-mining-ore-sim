using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed class RuneUpgradeRow : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button buyButton;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text price;
        [SerializeField] private TMP_Text bonus;
        [SerializeField] private UnityEngine.UI.Image[] progress;
        private RuneStation station;
        private int index;
        public void Bind(RuneStation owner, int rowIndex)
        {
            station = owner; index = rowIndex;
            EnsureBonusLabel();
            title.fontSize = 26f;
            price.fontSize = 22f;
            bonus.fontSize = 22f;
            bonus.rectTransform.sizeDelta = new Vector2(520,24);
            bonus.rectTransform.anchoredPosition = new Vector2(0,5);
            buyButton.onClick.RemoveListener(Buy);
            buyButton.onClick.AddListener(Buy);
        }
        private void Buy() { if (station != null) station.Buy(index); }
        public void Refresh(RuneUpgradeProgress upgrades, float gems)
        {
            var track = upgrades.Track(index);
            if (track == null) { buyButton.interactable = false; return; }
            int rank = upgrades.Rank(index);
            EnsureBonusLabel();
            title.text = MiningLocalization.TextKey(track.labelKey, track.stat.ToString());
            string unit = track.stat == RuneStat.Armor || track.stat == RuneStat.MagicResistance ? "" : "%";
            bonus.text = string.Format(MiningLocalization.TextKey("RUNE_BONUS", "Each rank +{0}{1} · Total +{2}{1}"),
                track.bonusPerRank.ToString("0.##"), unit, (rank * track.bonusPerRank).ToString("0.##"));
            bool max = rank >= track.maximumRank;
            price.text = max ? MiningLocalization.TextKey("RUNE_MAX", "MAX") : $"{upgrades.Cost(index):0} Gem  ·  {rank}/{track.maximumRank}";
            buyButton.interactable = upgrades.CanBuy(index, gems);
            for (int i = 0; i < progress.Length; i++)
                progress[i].color = i < rank ? track.progressColor : Color.clear;
        }
        private void EnsureBonusLabel()
        {
            if (bonus != null) return;
            var label = new GameObject("Bonus", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(transform, false);
            bonus = label.GetComponent<TextMeshProUGUI>();
            bonus.font = title.font;
            bonus.fontSize = 18f;
            bonus.color = title.color;
            bonus.alignment = TextAlignmentOptions.MidlineLeft;
            bonus.raycastTarget = false;
            bonus.rectTransform.sizeDelta = new Vector2(340, 24);
            bonus.rectTransform.anchoredPosition = new Vector2(-100, 5);
            title.rectTransform.anchoredPosition = new Vector2(title.rectTransform.anchoredPosition.x, 31);
        }
        private void OnDestroy() { if (buyButton != null) buyButton.onClick.RemoveListener(Buy); }
    }
}
