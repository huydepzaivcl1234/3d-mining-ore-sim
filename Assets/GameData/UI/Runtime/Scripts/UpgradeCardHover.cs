using UnityEngine;
using UnityEngine.EventSystems;
namespace MiningSimulator.Ores
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true,"MiningSimulator.Ores","Assembly-CSharp","MiningUpgradeCardHover")]
    public sealed class UpgradeCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool IsHovered { get; private set; }
        public void OnPointerEnter(PointerEventData data) => IsHovered = true;
        public void OnPointerExit(PointerEventData data) => IsHovered = false;
        private void OnDisable() => IsHovered = false;
    }
}
