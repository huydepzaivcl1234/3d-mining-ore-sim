using UnityEngine;
using UnityEngine.EventSystems;

namespace MiningSimulator.Ores
{
    /// <summary>Pointer and keyboard focus reveal the same catalog information.</summary>
    public sealed class TowerCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        TowerShopPanel panel;
        TowerData tower;
        bool pointed;
        public void Initialize(TowerShopPanel owner, TowerData definition) { panel=owner; tower=definition; }
        public void OnPointerEnter(PointerEventData e) { pointed=true; panel.Preview(tower); }
        public void OnPointerExit(PointerEventData e) { pointed=false; panel.EndPreview(tower); }
        public void OnSelect(BaseEventData e) { panel.Preview(tower); }
        public void OnDeselect(BaseEventData e) { if(!pointed) panel.EndPreview(tower); }
        void OnDisable() { pointed=false; if(panel!=null) panel.EndPreview(tower); }
    }
}
