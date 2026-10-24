using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum RuneStat { AttackSpeed, Damage, Health, MagicResistance, Armor }
    [Serializable]
    public sealed class RuneUpgradeTrack
    {
        public RuneStat stat;
        public string labelKey;
        public Sprite artwork;
        public Color progressColor = Color.yellow;
        [Min(1)] public int maximumRank = 11;
        [Min(0f)] public float bonusPerRank = 5f;
        [Min(1f)] public float firstGemCost = 10f;
        [Min(1.001f)] public float costGrowth = 1.5f;
        public float Cost(int rank)
        {
            double cost = Math.Ceiling(Math.Max(1, firstGemCost) * Math.Pow(Math.Max(1.001, costGrowth), Math.Max(0, rank)) + Math.Max(0, rank));
            return double.IsNaN(cost) || cost > float.MaxValue ? float.PositiveInfinity : (float)cost;
        }
    }
    [CreateAssetMenu(menuName = "Mining Simulator/Game Data/Rune Upgrades")]
    public sealed class RuneUpgradeData : ScriptableObject
    {
        [Tooltip("AS, damage and HP use percentage points; MR and armor use flat points.")]
        public RuneUpgradeTrack[] tracks = new RuneUpgradeTrack[5];
        public AudioClip ambience;
        public AudioClip purchaseSfx;
        [Min(.1f)] public float radius = 4f;
        [Min(0f)] public float revealDistance = 8f;
        [Min(.01f)] public float fadeSeconds = .3f;
        public Vector3 zoneOffset;
        public Vector3 panelOffset = new Vector3(0, 5.5f, 0);
        public Vector2 barSize = new Vector2(565, 104);
        [Min(0f)] public float panelSpacing = 24f;
        [Range(1,3)] public int visibleRows = 3;
        [Min(.01f)] public float scrollSeconds = .18f;
        [Range(0f,1f)] public float lowerRowOpacity = .75f;
        [Min(.001f)] public float scrollCanvasScale = .005f;
        [Min(0f)] public float panelForwardOffset = 2.5f;
        [Tooltip("Step height used only near the rune; the player's normal setting is restored on departure.")]
        [Min(0f)] public float nearbyStepHeight = .45f;
        [Min(.001f)] public float canvasScale = .006f;
        public Material boundaryMaterial;
    }
}
