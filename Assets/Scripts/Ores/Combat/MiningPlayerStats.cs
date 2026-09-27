using StarterAssets;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DefaultExecutionOrder(-1000), DisallowMultipleComponent]
    public sealed class MiningPlayerStats : MonoBehaviour
    {
        [SerializeField] private MiningPlayerStatsData data;
        public MiningPlayerStatsData Data => data;
        public float MaxHealth => data != null ? Mathf.Max(1, data.maxHealth + Mathf.Max(0, data.healthPerLevel) * (Level - 1)) : 100;
        public float Damage => data != null ? Mathf.Max(0, data.damage + Mathf.Max(0, data.damagePerLevel) * (Level - 1)) : 1;
        public int Level { get; private set; } = 1;
        public float Experience { get; private set; }
        public float ExperienceRequired { get; private set; } = 100;
        public float ExperienceProgress => Mathf.Clamp01(Experience / Mathf.Max(1, ExperienceRequired));
        private ThirdPersonController movement;
        private MiningAudioManager feedbackAudio;
        private const string ProgressSaveKey = "MiningSimulator.PlayerProgress.v1";
        private bool dirty, saveBlocked;
        private float nextSave;
        [System.Serializable]
        private sealed class SavedProgress
        {
            public int version = 1;
            public int level;
            public float experience;
            public float requiredExperience;
        }
        private void Awake()
        {
            movement = GetComponent<ThirdPersonController>();
            feedbackAudio = FindFirstObjectByType<MiningAudioManager>();
            if (data != null) SetProgress(data.startingLevel, data.startingExperience, data.experienceRequired);
            LoadProgress();
            dirty = false;
            ApplyMovement();
        }
        public void AddExperience(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            double remaining = (double)Experience + amount;
            float growth = data != null ? Mathf.Max(1, data.experienceRequirementGrowth) : 1.25f;
            if (growth == 1 && remaining >= ExperienceRequired)
            {
                int levels = (int)System.Math.Min(int.MaxValue - Level, System.Math.Floor(remaining / ExperienceRequired));
                Level += levels;
                remaining -= (double)levels * ExperienceRequired;
            }
            while (remaining >= ExperienceRequired && Level < int.MaxValue)
            {
                remaining -= ExperienceRequired;
                Level++;
                ExperienceRequired = Mathf.Min(float.MaxValue, ExperienceRequired * growth);
            }
            Experience = (float)System.Math.Min(remaining, ExperienceRequired);
            dirty = true;
        }
        public void SetProgress(int level, float experience, float requiredExperience)
        {
            Level = Mathf.Max(1, level);
            ExperienceRequired = Mathf.Max(1, requiredExperience);
            Experience = Mathf.Clamp(experience, 0, ExperienceRequired);
            dirty = true;
        }
        private void LoadProgress()
        {
            if (!Application.isPlaying || !PlayerPrefs.HasKey(ProgressSaveKey)) return;
            try
            {
                var saved = JsonUtility.FromJson<SavedProgress>(PlayerPrefs.GetString(ProgressSaveKey));
                if (saved == null || saved.version != 1 || saved.level < 1 ||
                    float.IsNaN(saved.experience) || float.IsInfinity(saved.experience) || saved.experience < 0 ||
                    float.IsNaN(saved.requiredExperience) || float.IsInfinity(saved.requiredExperience) || saved.requiredExperience < 1)
                {
                    saveBlocked = true;
                    Debug.LogWarning("Player progression save is invalid or from an unsupported version. It is preserved until Reset Data.", this);
                    return;
                }
                SetProgress(saved.level, saved.experience, saved.requiredExperience);
            }
            catch (System.ArgumentException)
            {
                saveBlocked = true;
                Debug.LogWarning("Cannot read player progression. Existing save is preserved until Reset Data.", this);
            }
        }
        private void SaveProgress()
        {
            if (!Application.isPlaying || !dirty || saveBlocked || data == null) return;
            PlayerPrefs.SetString(ProgressSaveKey, JsonUtility.ToJson(new SavedProgress
            { level = Level, experience = Experience, requiredExperience = ExperienceRequired }));
            PlayerPrefs.SetInt("MiningSimulator.SaveExists.v1", 1);
            PlayerPrefs.Save();
            dirty = false;
        }
        public static void ResetSavedProgress()
        {
            PlayerPrefs.DeleteKey(ProgressSaveKey);
            foreach (var player in FindObjectsByType<MiningPlayerStats>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var defaults = player.Data;
                player.SetProgress(defaults != null ? defaults.startingLevel : 1,
                    defaults != null ? defaults.startingExperience : 0,
                    defaults != null ? defaults.experienceRequired : 100);
                player.dirty = false; // Do not recreate a deleted save on scene unload.
                player.saveBlocked = false;
            }
            PlayerPrefs.Save();
        }
        private void OnApplicationPause(bool paused) { if (paused) SaveProgress(); }
        private void OnApplicationQuit() => SaveProgress();
        private void OnEnable()
        {
            if (movement != null) movement.Jumped += PlayJumpSound;
        }
        private void OnDisable()
        {
            if (movement != null) movement.Jumped -= PlayJumpSound;
            SaveProgress();
        }
        private void PlayJumpSound()
        {
            if (data != null && feedbackAudio != null)
                feedbackAudio.PlaySfx(data.jumpSfx, data.jumpSfxVolume);
        }
        // Movement's public fields are compatibility inputs, not a second authoring source.
        private void Update()
        {
            ApplyMovement();
            if (dirty && Time.unscaledTime >= nextSave)
            {
                nextSave = Time.unscaledTime + 1;
                SaveProgress();
            }
        }
        private void ApplyMovement()
        {
            if (data == null || movement == null) return;
            movement.MoveSpeed = Mathf.Max(0, data.MoveSpeed);
            movement.SprintSpeed = Mathf.Max(0, data.SprintSpeed);
            movement.RotationSmoothTime = Mathf.Max(0.001f, data.RotationSmoothTime);
            movement.SpeedChangeRate = Mathf.Max(0, data.SpeedChangeRate);
            movement.JumpHeight = Mathf.Max(0, data.JumpHeight);
            movement.Gravity = data.Gravity;
            movement.JumpTimeout = Mathf.Max(0, data.JumpTimeout);
            movement.FallTimeout = Mathf.Max(0, data.FallTimeout);
        }
        public static MiningPlayerStatsData For(Component owner)
        {
            var stats = owner.GetComponent<MiningPlayerStats>();
            return stats != null ? stats.Data : null;
        }
    }
}
