using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameData.Player.Script
{
    /// <summary>Observes animation only; never writes input, movement or Animator parameters.</summary>
    [DisallowMultipleComponent]
    public sealed class AnimationRunMana : MonoBehaviour
    {
        [Header("Animation detection")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private AnimationClip[] runningClips = new AnimationClip[0];
        [SerializeField, Min(0)] private int animatorLayer;
        [SerializeField, Range(0.01f, 1f)] private float minimumRunWeight = 0.35f;
        [Tooltip("Keeps the running cost during Jump/Fall when locomotion velocity remains in the run range.")]
        [SerializeField] private bool countAirborneRunning = true;
        [SerializeField, Min(0f)] private float airborneRunThreshold = 4f;
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int VelocityX = Animator.StringToHash("X_velocity");
        private static readonly int VelocityY = Animator.StringToHash("Y_velocity");
        private bool hasAirParameters;
        private PlayerController1 controller;
        private MiningSimulator.Ores.MiningCharacterHealth health;

        // Legacy fallback values; configured players use only Player Stats Core Data.
        [SerializeField, HideInInspector] private float maximumMana = 100f;
        [SerializeField, HideInInspector] private float runManaPerSecond = 10f;
        [SerializeField, HideInInspector] private float regenerationPerSecond = 8f;
        [SerializeField, HideInInspector] private float regenerationDelay = 1.5f;

        [Header("Optional existing HUD")]
        [SerializeField] private Slider manaBar;
        [SerializeField] private TMP_Text manaLabel;

        private readonly List<AnimatorClipInfo> clipInfos = new List<AnimatorClipInfo>(16);
        private float secondsSinceRun;
        private bool exhausted;
        private int lastDisplayedMana = -1;
        private int lastDisplayedMaximum = -1;
        private MiningSimulator.Ores.MiningPlayerStats playerStats;
        private PlayerCoreStatsData CoreData => playerStats != null ? playerStats.CoreData : null;

        public float CurrentMana { get; private set; }
        public float MaximumMana => Mathf.Max(1f, CoreData != null ? CoreData.maxStamina : maximumMana);
        public bool IsRunningAnimation { get; private set; }
        public bool IsDepleted => CurrentMana <= 0f;
        public bool CanRun => CurrentMana > 0f && !exhausted;
        public float SprintRecoveryThreshold => MaximumMana * Mathf.Clamp01(CoreData != null ? CoreData.sprintRecoveryThreshold : 0.25f);

        private void Reset()
        {
            playerAnimator = GetComponent<Animator>();
        }

        private void Awake()
        {
            if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
            playerStats = GetComponent<MiningSimulator.Ores.MiningPlayerStats>();
            controller = GetComponent<PlayerController1>();
            health = GetComponent<MiningSimulator.Ores.MiningCharacterHealth>();
            if (playerAnimator != null) {
                bool grounded = false, x = false, y = false;
                foreach (var p in playerAnimator.parameters) {
                    grounded |= p.nameHash == Grounded && p.type == AnimatorControllerParameterType.Bool;
                    x |= p.nameHash == VelocityX && p.type == AnimatorControllerParameterType.Float;
                    y |= p.nameHash == VelocityY && p.type == AnimatorControllerParameterType.Float;
                }
                hasAirParameters = grounded && x && y;
            }
            Refill();
            if (playerAnimator == null || runningClips.Length == 0)
                Debug.LogWarning("AnimationRunMana needs an Animator and running clips.", this);
        }

        private void LateUpdate()
        {
            IsRunningAnimation = ReadRunningAnimation();
            AdvanceMana(Time.deltaTime, IsRunningAnimation);
            RefreshHud();
        }

        private void OnDisable()
        {
            IsRunningAnimation = false;
        }

        public void Refill()
        {
            CurrentMana = MaximumMana;
            exhausted = false;
            secondsSinceRun = 0f;
            RefreshHud();
        }

        private bool ReadRunningAnimation()
        {
            if ((controller && !controller.isActiveAndEnabled) || (health && health.Health <= 0f)) return false;
            if (playerAnimator == null || !playerAnimator.isActiveAndEnabled ||
                playerAnimator.runtimeAnimatorController == null || !playerAnimator.isInitialized ||
                Mathf.Approximately(playerAnimator.speed, 0f) || animatorLayer >= playerAnimator.layerCount)
                return false;

            float runWeight = GetRunWeight(false);
            if (playerAnimator.IsInTransition(animatorLayer))
            {
                float blend = Mathf.Clamp01(playerAnimator.GetAnimatorTransitionInfo(animatorLayer).normalizedTime);
                runWeight = Mathf.Lerp(runWeight, GetRunWeight(true), blend);
            }
            if (runWeight >= minimumRunWeight) return true;
            if (!hasAirParameters || (!countAirborneRunning && !playerAnimator.GetBool(Grounded))) return false;
            float x = playerAnimator.GetFloat(VelocityX), y = playerAnimator.GetFloat(VelocityY);
            return x*x + y*y > airborneRunThreshold * airborneRunThreshold;
        }

        private float GetRunWeight(bool nextState)
        {
            clipInfos.Clear();
            if (nextState) playerAnimator.GetNextAnimatorClipInfo(animatorLayer, clipInfos);
            else playerAnimator.GetCurrentAnimatorClipInfo(animatorLayer, clipInfos);

            float weight = 0f;
            for (int i = 0; i < clipInfos.Count; i++)
            {
                for (int j = 0; j < runningClips.Length; j++)
                {
                    if (runningClips[j] != null && clipInfos[i].clip == runningClips[j])
                    {
                        weight += clipInfos[i].weight;
                        break;
                    }
                }
            }
            return Mathf.Clamp01(weight);
        }

        private void AdvanceMana(float deltaTime, bool running)
        {
            if (deltaTime <= 0f) return;
            var coreData = CoreData;
            float cost = coreData != null ? coreData.runningCostPerSecond : runManaPerSecond;
            float delay = coreData != null ? coreData.staminaRecoveryDelay : regenerationDelay;
            float recovery = coreData != null ? coreData.staminaRecoveryPerSecond : regenerationPerSecond;
            CurrentMana = Mathf.Clamp(CurrentMana, 0f, MaximumMana);
            if (CurrentMana <= 0f) exhausted = true;
            // Ignore fading run clips while exhausted so they cannot prevent recovery.
            if (running && !exhausted)
            {
                secondsSinceRun = 0f;
                CurrentMana = Mathf.Max(0f, CurrentMana - Mathf.Max(0f, cost) * deltaTime);
                if (CurrentMana <= 0f) exhausted = true;
                return;
            }

            // Only regenerate the part of this frame after the recovery delay.
            float beforeDelay = Mathf.Max(0f, secondsSinceRun - delay);
            secondsSinceRun += deltaTime;
            float recoveryTime = Mathf.Max(0f, secondsSinceRun - delay) - beforeDelay;
            CurrentMana = Mathf.Min(MaximumMana, CurrentMana + Mathf.Max(0f, recovery) * recoveryTime);
            if (exhausted && CurrentMana > 0f && CurrentMana >= SprintRecoveryThreshold) exhausted = false;
        }

        private void RefreshHud()
        {
            if (manaBar != null) manaBar.SetValueWithoutNotify(Mathf.Lerp(manaBar.minValue, manaBar.maxValue, CurrentMana / MaximumMana));
            int current = Mathf.CeilToInt(CurrentMana);
            int maximum = Mathf.CeilToInt(MaximumMana);
            if (manaLabel != null && (current != lastDisplayedMana || maximum != lastDisplayedMaximum))
            {
                manaLabel.SetText("Stamina {0} / {1}", current, maximum);
                lastDisplayedMana = current;
                lastDisplayedMaximum = maximum;
            }
        }

        private void OnValidate()
        {
            maximumMana = Mathf.Max(1f, maximumMana);
            runManaPerSecond = Mathf.Max(0f, runManaPerSecond);
            regenerationPerSecond = Mathf.Max(0f, regenerationPerSecond);
            regenerationDelay = Mathf.Max(0f, regenerationDelay);
            animatorLayer = Mathf.Max(0, animatorLayer);
            minimumRunWeight = Mathf.Clamp(minimumRunWeight, 0.01f, 1f);
            airborneRunThreshold = Mathf.Max(0f, airborneRunThreshold);
            if (runningClips == null) runningClips = new AnimationClip[0];
        }
    }
}
