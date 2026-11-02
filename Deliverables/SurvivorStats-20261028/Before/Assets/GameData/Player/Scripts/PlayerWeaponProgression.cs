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
            public Transform gun, muzzle, slide;
            public Transform worldGun;
            public PistolWeaponData data;
            public PistolShooting.ViewFrame frame = new();
            public string animationLayer;
            [System.NonSerialized] public int ammo = -1, reserve;
            [System.NonSerialized] public DroppedWeapon dropped;
        }
        [SerializeField] private Loadout pistol = new(), rifle = new();
        [SerializeField, Min(1)] private int godBossDay = 50;
        private const string SaveKey = "Player.Weapons.RifleUnlocked.v1";
        private PistolShooting shooting;
        private Loadout selected;
        private DayNightSystem dayNight;
        public bool RifleUnlocked { get; private set; }
        public bool UsingRifle => selected == rifle;
        public int GodBossDay => Mathf.Max(1, godBossDay);
        public bool CanCollectDroppedWeapon => isActiveAndEnabled && shooting && shooting.CanChangeWeapon;

        public bool DropEquippedWeapon()
        {
            if (selected == null || !shooting || !shooting.CanFire || shooting.Reloading) return false;
            Transform view = shooting.ViewCamera.transform;
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .001f) forward = transform.forward;
            float distance = 2.2f;
            foreach (var hit in Physics.SphereCastAll(view.position, .14f, forward, distance, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform)) distance = Mathf.Min(distance, Mathf.Max(.35f, hit.distance - .2f));
            Vector3 ground = transform.position + forward * distance;
            bool found = false; float nearest = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(ground + Vector3.up * 3f, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<MiningCharacterHealth>() || hit.normal.y < .5f) continue;
                if (hit.distance < nearest) { nearest = hit.distance; ground = hit.point; found = true; }
            }
            if (!found) return false;
            var pickup = DroppedWeapon.Create(this, selected.worldGun ? selected.worldGun : selected.gun,
                view.position + forward * .4f - Vector3.up * .18f, ground);
            if (!pickup) return false;
            selected.ammo = shooting.Ammo; selected.reserve = shooting.Reserve;
            selected.dropped = pickup; selected = null;
            shooting.SetWeaponAvailable(false);
            return true;
        }

        public bool PickupDroppedWeapon(DroppedWeapon pickup)
        {
            if (!pickup || !CanCollectDroppedWeapon) return false;
            Loadout loadout = pistol.dropped == pickup ? pistol : rifle.dropped == pickup ? rifle : null;
            if (loadout == null) return false;
            loadout.dropped = null;
            if (SelectLoadout(loadout == rifle, false)) return true;
            loadout.dropped = pickup;
            return false;
        }
        private void OnDestroy()
        {
            if (pistol.dropped) Destroy(pistol.dropped.gameObject);
            if (rifle.dropped) Destroy(rifle.dropped.gameObject);
        }

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
            if(!pistol.dropped){pistol.ammo=-1;pistol.reserve=pistol.data?Mathf.Max(0,pistol.data.reserveAmmo):0;}
            if(!rifle.dropped){rifle.ammo=-1;rifle.reserve=rifle.data?Mathf.Max(0,rifle.data.reserveAmmo):0;}
            if(!shooting || selected==null)return;
            shooting.ResetState();
            if(selected!=null){selected.ammo=shooting.Ammo;selected.reserve=shooting.Reserve;}
        }

        private void Start()
        {
            shooting = GetComponent<PistolShooting>();
            RifleUnlocked = GameSave.GetInt(SaveKey) == 1;
            Select(false); // Each session starts holding the pistol, even after the rifle has been earned.
        }
        private void Update()
        {
            if (!shooting || !shooting.CanChangeWeapon || Keyboard.current == null) return;
            if (Keyboard.current.eKey.wasPressedThisFrame) { DropEquippedWeapon(); return; }
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
            if (next.dropped) return false;
            if (next == selected) { shooting.SetEquipped(true); return true; }
            if (!next.gun || !next.muzzle || !next.data || !shooting.PoseAnimator || (!shooting.UsesFullBody && shooting.PoseAnimator.GetLayerIndex(next.animationLayer) < 0))
            { Debug.LogError("Weapon loadout is incomplete; previous weapon preserved.", this); return false; }
            if (selected != null) { selected.ammo = shooting.Ammo; selected.reserve = shooting.Reserve; }
            shooting.SetWeaponData(next.data, next.ammo, next.ammo < 0 ? -1 : next.reserve);
            shooting.ConfigureWeapon(next.gun, next.frame, next.animationLayer, next.worldGun);
            shooting.ConfigureWeapon(next.muzzle, next.slide);
            selected = next;
            shooting.SetWeaponAvailable(true);
            shooting.SetEquipped(true);
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
