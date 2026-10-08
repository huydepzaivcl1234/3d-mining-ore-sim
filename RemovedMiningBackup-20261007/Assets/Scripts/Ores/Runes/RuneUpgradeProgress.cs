using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Permanent rune ranks. Uses its own additive save, never rewrites card/XP data.</summary>
    public sealed class RuneUpgradeProgress
    {
        public const string SaveKey = "MiningSimulator.RuneRanks.v1";
        [Serializable] private sealed class Save { public int version = 1; public int[] ranks; }
        private RuneUpgradeData data;
        private readonly int[] ranks = new int[5];
        private bool saveBlocked;
        public event Action Changed;
        public void Configure(RuneUpgradeData settings)
        {
            if (settings == null || data == settings) return;
            data = settings;
            Changed?.Invoke();
        }
        public RuneUpgradeProgress(RuneUpgradeData settings, string savedJson = null)
        {
            data = settings;
            if (string.IsNullOrEmpty(savedJson)) return;
            try
            {
                var saved = JsonUtility.FromJson<Save>(savedJson);
                if (saved == null || saved.version != 1 || saved.ranks == null || saved.ranks.Length != ranks.Length)
                { saveBlocked = true; return; }
                for (int i = 0; i < ranks.Length; i++)
                {
                    if (saved.ranks[i] < 0) { saveBlocked = true; Array.Clear(ranks, 0, ranks.Length); return; }
                    ranks[i] = saved.ranks[i]; // Retain purchased ranks if designers later change the cap.
                }
            }
            catch (ArgumentException) { saveBlocked = true; }
        }
        public RuneUpgradeTrack Track(int index) => data != null && data.tracks != null && index >= 0 && index < data.tracks.Length ? data.tracks[index] : null;
        public int Rank(int index) => index >= 0 && index < ranks.Length ? ranks[index] : 0;
        public float Bonus(RuneStat stat)
        {
            float total = 0;
            for (int i = 0; i < ranks.Length; i++)
                if (Track(i) is RuneUpgradeTrack track && track.stat == stat)
                    total += Rank(i) * CombatDamage.NonNegative(track.bonusPerRank);
            return total;
        }
        public float Cost(int index) => Track(index)?.Cost(Rank(index)) ?? float.PositiveInfinity;
        public bool CanBuy(int index, float gems) => !saveBlocked && Track(index) != null && index < ranks.Length &&
            Rank(index) < Track(index).maximumRank && !float.IsInfinity(Cost(index)) && !float.IsNaN(gems) && gems >= Cost(index);
        public bool TryBuy(int index, PlayerWallet wallet)
        {
            if (wallet == null || !CanBuy(index, wallet.CurrentGems) || !wallet.TrySpendGems(Cost(index))) return false;
            ranks[index]++;
            if (Application.isPlaying) { PlayerPrefs.SetString(SaveKey, ToJson()); PlayerPrefs.Save(); }
            Changed?.Invoke();
            return true;
        }
        public string ToJson() => JsonUtility.ToJson(new Save { ranks = ranks });
        public void Reset()
        {
            Array.Clear(ranks, 0, ranks.Length);
            saveBlocked = false;
            Changed?.Invoke();
        }
    }
}
