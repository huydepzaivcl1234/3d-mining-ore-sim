using UnityEngine;
using UnityEngine.EventSystems;
namespace MiningSimulator.Ores
{
    public sealed class MiningUpgradeCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool IsHovered { get; private set; }
        public void OnPointerEnter(PointerEventData data) => IsHovered = true;
        public void OnPointerExit(PointerEventData data) => IsHovered = false;
        private void OnDisable() => IsHovered = false;
    }
}
