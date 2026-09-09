using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Shows all active consumable effects and their live remaining durations.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningEffectToast : MonoBehaviour
    {
        [SerializeField] private MiningItemSystem itemSystem;
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private TextMeshProUGUI effectLabel;

        private readonly List<MiningItemSystem.ActiveEffectView> activeEffects = new(3);
        private readonly StringBuilder textBuilder = new(160);
        private float nextRefreshTime;

        private void Awake()
        {
            Refresh();
        }

        private void OnEnable()
        {
            if (itemSystem != null)
            {
                itemSystem.EffectsChanged -= Refresh;
                itemSystem.EffectsChanged += Refresh;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (itemSystem != null)
            {
                itemSystem.EffectsChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime || toastRoot == null ||
                !toastRoot.activeSelf)
            {
                return;
            }
            nextRefreshTime = Time.unscaledTime + 0.1f;
            Refresh();
        }

        private void Refresh()
        {
            activeEffects.Clear();
            itemSystem?.GetActiveEffects(activeEffects);
            bool visible = activeEffects.Count > 0;
            if (effectLabel != null)
            {
                textBuilder.Clear();
                for (int index = 0; index < activeEffects.Count; index++)
                {
                    MiningItemSystem.ActiveEffectView effect = activeEffects[index];
                    if (index > 0)
                    {
                        textBuilder.AppendLine();
                    }
                    textBuilder.Append(effect.Item.DisplayName)
                        .Append(": ")
                        .Append(effect.Item.EffectName)
                        .Append(" +")
                        .Append(effect.Item.EffectPercent.ToString("0.##"))
                        .Append("%  •  còn ")
                        .Append(Mathf.CeilToInt(effect.RemainingSeconds))
                        .Append('s');
                }
                effectLabel.text = textBuilder.ToString();
            }
            toastRoot?.SetActive(visible);
        }
    }
}
