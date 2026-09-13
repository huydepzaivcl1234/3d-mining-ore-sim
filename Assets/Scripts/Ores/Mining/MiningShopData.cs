using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Designer-owned products and prices for the Gem shop.</summary>
    [CreateAssetMenu(fileName = "MiningShopData",
        menuName = "Mining Simulator/Game Data/Shop")]
    public sealed class MiningShopData : ScriptableObject
    {
        [Header("Rare Gift Box")]
        [SerializeField] private MiningItemData rareGiftBox;
        [Min(0f), SerializeField] private float rareGiftBoxGemCost = 100f;

        public MiningItemData RareGiftBox => rareGiftBox;
        public float RareGiftBoxGemCost => Mathf.Max(0f, rareGiftBoxGemCost);

        private void OnValidate()
        {
            rareGiftBoxGemCost = Mathf.Max(0f, rareGiftBoxGemCost);
        }
    }
}
