using UnityEngine;
namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent]
    public sealed class UpgradeCardView : MonoBehaviour
    {
        [Tooltip("Connect the parts of this card prefab. The panel reads these references, not child names.")]
        public UpgradePanelView.OfferSlot bindings = new();
    }
}
