using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Routes one authored inventory slot button to its panel without lambda listeners.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningInventorySlotButton : MonoBehaviour
    {
        [SerializeField] private MiningInventoryPanel panel;
        [SerializeField, Min(0)] private int slotIndex;
        private Button button;

        public void Configure(MiningInventoryPanel targetPanel, int targetSlotIndex)
        {
            panel = targetPanel;
            slotIndex = Mathf.Max(0, targetSlotIndex);
            button ??= GetComponent<Button>();
        }

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button ??= GetComponent<Button>();
            button?.onClick.RemoveListener(UseItem);
            button?.onClick.AddListener(UseItem);
        }

        private void OnDisable()
        {
            button?.onClick.RemoveListener(UseItem);
        }

        private void UseItem()
        {
            panel?.UseSlot(slotIndex);
        }
    }
}
