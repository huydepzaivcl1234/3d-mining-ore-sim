using Microlight.MicroBar;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Displays shared NPC power and smoothly animated experience progress.</summary>
    [DisallowMultipleComponent]
    public sealed class NpcProgressionHud : MonoBehaviour
    {
        [SerializeField] private NpcProgressionSystem progressionSystem;
        [SerializeField] private NpcShop npcShop;
        [SerializeField] private NpcData npcData;
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TextMeshProUGUI powerLabel;
        [SerializeField] private TextMeshProUGUI experienceLabel;
        [SerializeField] private MicroBar experienceBar;
        [SerializeField, HideInInspector] private Image experienceFill;

        [Header("Editable Text")]
        [SerializeField] private string levelFormat = "CẤP THỢ MỎ: {0}";
        [SerializeField] private string powerFormat = "NPC: {0}  •  POWER: {1}  •  TỔNG DMG: {2:0.##}";
        [SerializeField] private string experienceFormat = "{0:0.##} / {1} XP";

        private float displayedProgress;
        private float targetProgress;
        private bool barInitialized;
        private int initializedRequirement = 1;

        private void OnEnable()
        {
            if (progressionSystem != null)
            {
                progressionSystem.ProgressionChanged -= Refresh;
                progressionSystem.ProgressionChanged += Refresh;
            }

            if (npcShop != null)
            {
                npcShop.NpcCountChanged -= HandleNpcCountChanged;
                npcShop.NpcCountChanged += HandleNpcCountChanged;
            }

            RefreshImmediate();
        }

        private void OnDisable()
        {
            if (progressionSystem != null)
            {
                progressionSystem.ProgressionChanged -= Refresh;
            }

            if (npcShop != null)
            {
                npcShop.NpcCountChanged -= HandleNpcCountChanged;
            }
        }

        private void Update()
        {
            float speed = uiData != null ? uiData.NpcExperienceBarAnimationSpeed : 2.5f;
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress,
                speed * Time.unscaledDeltaTime);
            RefreshExperienceBar();
        }

        public void Refresh()
        {
            int level = progressionSystem != null ? progressionSystem.CurrentLevel : 1;
            int power = progressionSystem != null
                ? progressionSystem.CurrentMiningPower
                : npcData != null ? npcData.MiningPower : 1;
            int npcCount = npcShop != null ? npcShop.PurchasedCount : 0;
            float damagePerHit = progressionSystem != null
                ? progressionSystem.CurrentDamagePerHit
                : npcData != null ? npcData.DamagePerHit : 0f;
            float totalDamage = npcCount * damagePerHit;
            float experience = progressionSystem != null ? progressionSystem.CurrentExperience : 0f;
            int required = progressionSystem != null ? progressionSystem.ExperienceRequired : 1;

            if (levelLabel != null)
            {
                levelLabel.text = string.Format(levelFormat, level);
            }
            if (powerLabel != null)
            {
                powerLabel.text = string.Format(powerFormat, npcCount, power, totalDamage);
            }
            if (experienceLabel != null)
            {
                experienceLabel.text = string.Format(experienceFormat, experience, required);
            }

            ConfigureExperienceBar(required);
            targetProgress = progressionSystem != null ? progressionSystem.Progress01 : 0f;
        }

        private void RefreshImmediate()
        {
            Refresh();
            displayedProgress = targetProgress;
            RefreshExperienceBar();
        }

        private void ConfigureExperienceBar(int required)
        {
            if (experienceBar == null)
            {
                return;
            }

            int safeRequirement = Mathf.Max(1, required);
            if (!barInitialized)
            {
                experienceBar.Initialize(safeRequirement);
                barInitialized = true;
            }
            else if (initializedRequirement != safeRequirement)
            {
                experienceBar.SetNewMaxHP(safeRequirement, true);
            }

            initializedRequirement = safeRequirement;
        }

        private void RefreshExperienceBar()
        {
            if (experienceBar != null)
            {
                if (!barInitialized)
                {
                    ConfigureExperienceBar(initializedRequirement);
                }

                experienceBar.UpdateBar(displayedProgress * initializedRequirement);
                return;
            }

            // Preserve old Scene HUDs until the editor refresh command migrates them to MicroBar.
            if (experienceFill != null)
            {
                experienceFill.fillAmount = displayedProgress;
            }
        }

        private void HandleNpcCountChanged(int count)
        {
            Refresh();
        }
    }
}
