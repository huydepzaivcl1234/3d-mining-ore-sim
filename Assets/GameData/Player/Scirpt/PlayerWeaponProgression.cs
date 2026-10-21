using MiningSimulator.Ores;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameData.Player.Script
{
    /// <summary>Two authored loadouts on the original body. God Boss reward is saved independently of view/animation data.</summary>
    [DefaultExecutionOrder(90), DisallowMultipleComponent, RequireComponent(typeof(PistolShooting))]
    public sealed class PlayerWeaponProgression : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Loadout
        {
            public Transform gun, sight, muzzle, slide;
            public PistolWeaponData data;
            public PistolHandPose pose;
            public string animationLayer;
            public Vector3 wristEuler;
            [System.NonSerialized] public int ammo = -1, reserve;
        }
        [SerializeField] private Loadout pistol = new(), rifle = new();
        [SerializeField, Min(1)] private int godBossDay = 50;
        private const string SaveKey = "Player.Weapons.RifleUnlocked.v1";
        private PistolShooting shooting;
        private PistolPresentation presentation;
        private PistolShotFeedback feedback;
        private Animator animator;
        private Loadout selected;
        private DayNightSystem dayNight;
        public bool RifleUnlocked { get; private set; }
        public bool UsingRifle => selected == rifle;
        public int GodBossDay => Mathf.Max(1, godBossDay);

        private void OnEnable()
        {
            dayNight=FindAnyObjectByType<DayNightSystem>();
            if(dayNight)dayNight.PeriodChanged+=OnPeriodChanged;
        }
        private void OnDisable() { if(dayNight)dayNight.PeriodChanged-=OnPeriodChanged; }
        private void OnPeriodChanged(MiningTimePeriod period)
        {
            if(period==MiningTimePeriod.Day)RefillAmmo();
        }
        public void RefillAmmo()
        {
            pistol.ammo=rifle.ammo=-1;
            pistol.reserve=pistol.data?Mathf.Max(0,pistol.data.reserveAmmo):0;
            rifle.reserve=rifle.data?Mathf.Max(0,rifle.data.reserveAmmo):0;
            if(!shooting)return;
            shooting.ResetState();
            if(selected!=null){selected.ammo=shooting.Ammo;selected.reserve=shooting.Reserve;}
        }

        private void Start()
        {
            shooting = GetComponent<PistolShooting>(); presentation = GetComponent<PistolPresentation>();
            feedback = GetComponent<PistolShotFeedback>(); animator = GetComponent<Animator>();
            RifleUnlocked = GameSave.GetInt(SaveKey) == 1;
            Select(false); // Each session starts holding the pistol, even after the rifle has been earned.
        }
        private void Update()
        {
            if (!presentation.CanFire || Keyboard.current == null) return;
            if (Keyboard.current.digit1Key.wasPressedThisFrame) Select(false);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) Select(true);
        }
        public bool Select(bool useRifle) => SelectLoadout(useRifle, false);
#if UNITY_EDITOR
        public bool DebugSelect(bool useRifle) => SelectLoadout(useRifle, true);
#endif
        private bool SelectLoadout(bool useRifle, bool debug)
        {
            if (!shooting || useRifle && !RifleUnlocked && !debug) return false;
            var next = useRifle ? rifle : pistol;
            if (next == selected) return true;
            if (!next.gun || !next.muzzle || !next.data || !next.pose || !next.pose.valid || animator.GetLayerIndex(next.animationLayer) < 0)
            { Debug.LogError("Weapon loadout is incomplete; previous weapon preserved.", this); return false; }
            if (selected != null) { selected.ammo = shooting.Ammo; selected.reserve = shooting.Reserve; }
            shooting.SetWeaponData(next.data, next.ammo, next.ammo < 0 ? -1 : next.reserve);
            presentation.ConfigureWeapon(next.gun, next.pose, next.animationLayer, next.sight, next.wristEuler);
            feedback.ConfigureWeapon(next.muzzle, next.slide);
            selected = next;
            return true;
        }
        public bool TryGrantRifle(GodBossRifleReward defeated)
        {
            if (!defeated || !defeated.DefeatedByPlayer || defeated.EncounterDay < GodBossDay || RifleUnlocked) return false;
            var boss = defeated.GetComponent<GodBossController>();
            if (boss ? !boss.Data.IsEncounterDay(defeated.EncounterDay) : defeated.EncounterDay != GodBossDay) return false;
            RifleUnlocked = true; GameSave.SetInt(SaveKey, 1); GameSave.Save();
            Select(true);
            return true;
        }
        public void ResetProgress()
        {
            RifleUnlocked = false; GameSave.DeleteKey(SaveKey);
            Select(false); pistol.ammo = rifle.ammo = -1;
            if (shooting) shooting.ResetState();
        }
    }
}
