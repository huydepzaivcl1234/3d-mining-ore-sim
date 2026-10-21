using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MiningSimulator.Ores;

namespace ChestDefense.Fps
{
    [DefaultExecutionOrder(150), DisallowMultipleComponent]
    public sealed class FpsWeaponController : MonoBehaviour
    {
        public FpsWeaponData[] loadout;
        public FpsPlayerCamera cameraRig;
        public MiningUiPanelCoordinator panels;
        public MiningAudioManager audioManager;
        public MiningKeybindSettings keybindSettings;
        [Tooltip("Input components competing with gun controls. Setup selects melee and world loot clicks.")]
        public Behaviour[] incompatibleInputs;
        public Transform viewsRoot;
        public bool saveAmmo = true;
        public string saveKey = "ChestDefense.FpsWeapons.v1";
        public float emptySoundInterval = .3f;
        public InputAction fire = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
        public InputAction aim = new InputAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
        public InputAction reload = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
        public InputAction nextWeapon = new InputAction("NextWeapon", InputActionType.Button, "<Keyboard>/q");
        public InputAction firstWeapon = new InputAction("FirstWeapon", InputActionType.Button, "<Keyboard>/1");
        public InputAction secondWeapon = new InputAction("SecondWeapon", InputActionType.Button, "<Keyboard>/2");
        [Tooltip("Hold to click the existing screen HUD/shop buttons.")]
        public InputAction freeCursor = new InputAction("FreeCursor", InputActionType.Button, "<Keyboard>/leftAlt");
        const string BindingsKey = "ChestDefense.FpsBindings.v1";
        [Serializable] public sealed class AmmoRecord { public string id; public int loaded, reserve; }
        [Serializable] sealed class Profile { public int version = 1; public string selected; public List<AmmoRecord> weapons = new List<AmmoRecord>(); }
        [Serializable] sealed class BindingRecord { public string action, path; public int index; }
        [Serializable] sealed class BindingProfile { public List<BindingRecord> bindings = new List<BindingRecord>(); }
        Profile profile = new Profile();
        bool profileWritable = true, initialized, fireArmed, suspended;
        bool[] oldInputStates;
        readonly HashSet<int> availableSlots = new HashSet<int>();
        readonly Dictionary<string, FpsMagazine> magazines = new Dictionary<string, FpsMagazine>();
        readonly Dictionary<string, FpsGunView> views = new Dictionary<string, FpsGunView>();
        readonly RaycastHit[] hits = new RaycastHit[64];
        InputAction[] actions;
        int selected = -1;
        FpsGunView currentView;
        MiningPlayerStats stats;
        MiningCharacterHealth health;
        MiningItemSystem items;
        PlayerKnockbackRagdoll knockdown;
        double nextEmptySound;
        public event Action Changed;
        public event Action<float> HitConfirmed;
        public FpsWeaponData CurrentWeapon => loadout != null && selected >= 0 && selected < loadout.Length ? loadout[selected] : null;
        public FpsMagazine CurrentMagazine => CurrentWeapon != null && magazines.TryGetValue(CurrentWeapon.weaponId, out var m) ? m : null;
        public bool HasWeapon => currentView != null && CurrentWeapon != null;
        public bool FreeCursor => freeCursor != null && freeCursor.enabled && freeCursor.IsPressed();
        public bool IsAiming => CanUseWeapon && !CurrentMagazine.Reloading && aim.enabled && aim.IsPressed();
        public bool CanUseWeapon => initialized && HasWeapon && cameraRig != null && cameraRig.CanLook &&
            (keybindSettings == null || !keybindSettings.IsRebinding) && (health == null || health.Health > 0) &&
            (knockdown == null || !knockdown.IsIncapacitated);

        void Awake()
        {
            stats = GetComponent<MiningPlayerStats>(); health = GetComponent<MiningCharacterHealth>();
            knockdown = GetComponent<PlayerKnockbackRagdoll>(); items = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            actions = new[] { fire, aim, reload, nextWeapon, firstWeapon, secondWeapon, freeCursor };
            LoadBindings(); LoadProfile();
            if (viewsRoot == null || cameraRig == null || cameraRig.targetCamera == null)
            { Debug.LogError("Run Chest Defense/FPS/Setup selected player and HUD first.", this); return; }
            var ids = new HashSet<string>();
            if (loadout != null) for (int i = 0; i < loadout.Length; i++)
            {
                var data = loadout[i];
                if (data == null) continue;
                if (!data.TryValidate(out var reason)) { Debug.LogError(data.name + ": " + reason, data); continue; }
                if (!ids.Add(data.weaponId)) { Debug.LogError("Duplicate FPS weapon ID: " + data.weaponId, data); continue; }
                var saved = profile.weapons.Find(r => r.id == data.weaponId);
                magazines.Add(data.weaponId, new FpsMagazine(data.magazineCapacity, data.maximumReserve,
                    saved != null ? saved.loaded : data.magazineCapacity, saved != null ? saved.reserve : data.startingReserve));
                var view = Instantiate(data.viewPrefab, viewsRoot);
                view.name = "View " + data.weaponId;
                view.transform.localPosition = data.hipPosition; view.transform.localRotation = Quaternion.Euler(data.hipEuler);
                if (!view.Initialize(data, audioManager)) { Destroy(view.gameObject); magazines.Remove(data.weaponId); continue; }
                view.gameObject.SetActive(false); views.Add(data.weaponId, view); availableSlots.Add(i);
            }
            initialized = true;
            int preferred = loadout != null ? Array.FindIndex(loadout, d => d != null && d.weaponId == profile.selected) : -1;
            if (!Equip(preferred)) Cycle(1);
        }
        void OnEnable()
        {
            if (actions != null) foreach (var action in actions) action?.Enable();
            fireArmed = false;
            if (currentView != null) { SuspendInputs(); currentView.gameObject.SetActive(cameraRig.OwnsView); }
            if (health != null) health.Died += OnDied;
        }
        void OnDisable()
        {
            if (health != null) health.Died -= OnDied;
            CancelReload(); SaveProfile();
            if (actions != null) foreach (var action in actions) action?.Disable();
            if (currentView != null) currentView.gameObject.SetActive(false);
            ResumeInputs();
        }
        void OnDestroy()
        {
            if (actions != null) foreach (var action in actions) action?.Dispose();
            foreach (var view in views.Values) if (view != null) Destroy(view.gameObject);
            views.Clear();
        }
        void OnApplicationPause(bool pause) { if (pause) { CancelReload(); SaveProfile(); GameSave.Flush(); } }
        void OnApplicationFocus(bool focused) { if (!focused) { CancelReload(); fireArmed = false; } }
        void OnApplicationQuit() { SaveProfile(); GameSave.Flush(); }
        void OnDied() { CancelReload(); fireArmed = false; }
        void Update()
        {
            if (!initialized || !HasWeapon) return;
            SuspendInputs();
            bool owns = cameraRig.OwnsView;
            if (currentView.gameObject.activeSelf != owns) currentView.gameObject.SetActive(owns);
            if (!CanUseWeapon) { CancelReload(); fireArmed = false; return; }
            if (firstWeapon.WasPressedThisFrame()) Equip(0);
            else if (secondWeapon.WasPressedThisFrame()) Equip(1);
            else if (nextWeapon.WasPressedThisFrame()) Cycle(1);
            if (reload.WasPressedThisFrame()) BeginReload();
        }
        void LateUpdate()
        {
            if (!initialized || !HasWeapon) return;
            if (CurrentMagazine.CompleteReload(Time.timeAsDouble)) { SaveProfile(); Changed?.Invoke(); }
            if (currentView.gameObject.activeSelf) currentView.Tick(IsAiming, Time.deltaTime);
            if (!CanUseWeapon) return;
            if (!fire.IsPressed()) fireArmed = true;
            if (!fireArmed) return;
            bool requested = CurrentWeapon.fireMode == FpsFireMode.Automatic ? fire.IsPressed() : fire.WasPressedThisFrame();
            if (!requested || CurrentMagazine.Reloading) return;
            if (CurrentMagazine.Loaded == 0)
            {
                if (Time.timeAsDouble >= nextEmptySound) { currentView.PlayEmpty(); nextEmptySound = Time.timeAsDouble + emptySoundInterval; }
                if (CurrentWeapon.reloadWhenEmpty) BeginReload();
                return;
            }
            float speedRatio = CurrentWeapon.usePlayerAttackSpeed && stats != null
                ? stats.AttackSpeed / (stats.Data != null ? Mathf.Max(.1f, stats.Data.attackSpeed) : 1f) : 1f;
            if (!CurrentMagazine.TryFire(Time.timeAsDouble, CurrentWeapon.ShotInterval(speedRatio))) return;
            currentView.PlayShot();
            ResolveShot();
            cameraRig.AddRecoil(CurrentWeapon.recoilPitch, UnityEngine.Random.Range(-CurrentWeapon.recoilYaw, CurrentWeapon.recoilYaw));
            SaveProfile(); Changed?.Invoke();
        }
        public bool Equip(int index)
        {
            if (!initialized || loadout == null || index < 0 || index >= loadout.Length || loadout[index] == null || !availableSlots.Contains(index) ||
                !views.TryGetValue(loadout[index].weaponId, out var view)) return false;
            if (index == selected && currentView != null) return true;
            CancelReload();
            if (currentView != null) currentView.gameObject.SetActive(false);
            selected = index; currentView = view;
            view.transform.localPosition = CurrentWeapon.hipPosition;
            view.transform.localRotation = Quaternion.Euler(CurrentWeapon.hipEuler);
            view.gameObject.SetActive(isActiveAndEnabled && cameraRig.OwnsView);
            fireArmed = false; SaveProfile(); Changed?.Invoke(); return true;
        }
        void Cycle(int step)
        {
            if (loadout == null) return;
            for (int offset = 1; offset <= loadout.Length; offset++)
                if (Equip((selected + offset * step + loadout.Length) % loadout.Length)) return;
        }
        public bool BeginReload()
        {
            if (!CanUseWeapon || !CurrentMagazine.BeginReload(Time.timeAsDouble, CurrentWeapon.reloadSeconds)) return false;
            currentView.PlayReload(); Changed?.Invoke(); return true;
        }
        void CancelReload()
        {
            if (CurrentMagazine == null || !CurrentMagazine.Reloading) return;
            CurrentMagazine.CancelReload(); currentView.ResetAnimation();
            if (currentView.audioSource != null) currentView.audioSource.Stop();
            Changed?.Invoke();
        }
        public int AddAmmo(string weaponId, int amount)
        {
            if (string.IsNullOrEmpty(weaponId) || !magazines.TryGetValue(weaponId, out var magazine)) return 0;
            int accepted = magazine.AddReserve(amount);
            if (accepted > 0) { SaveProfile(); Changed?.Invoke(); }
            return accepted;
        }
        public int AmmoSpace(string weaponId) => !string.IsNullOrEmpty(weaponId) && magazines.TryGetValue(weaponId, out var m)
            ? m.MaximumReserve - m.Reserve : 0;
        void ResolveShot()
        {
            var data = CurrentWeapon; var cam = cameraRig.targetCamera;
            float spread = Mathf.Tan((IsAiming ? data.aimSpreadDegrees : data.hipSpreadDegrees) * Mathf.Deg2Rad);
            Vector2 disk = UnityEngine.Random.insideUnitCircle * spread;
            Vector3 direction = (cam.transform.forward + cam.transform.right * disk.x + cam.transform.up * disk.y).normalized;
            bool hitAim = FpsHitscan.Closest(new Ray(cam.transform.position, direction), data.range, data.hitLayers,
                transform, cam.transform, hits, out var aimHit, out bool overflow);
            if (overflow) return;
            Vector3 point = hitAim ? aimHit.point : cam.transform.position + direction * data.range;
            Vector3 muzzle = currentView.muzzle.position;
            Vector3 travel = point - muzzle;
            if (travel.sqrMagnitude < .000001f) return;
            bool obstructed = FpsHitscan.Closest(new Ray(muzzle, travel.normalized), travel.magnitude + .01f,
                data.hitLayers, transform, cam.transform, hits, out var muzzleHit, out overflow);
            if (overflow) return;
            if (!obstructed && !hitAim) return;
            RaycastHit hit = obstructed ? muzzleHit : aimHit;
            ApplyHit(hit);
            if (data.impactPrefab != null)
            {
                var impact = Instantiate(data.impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                foreach (var ps in impact.GetComponentsInChildren<ParticleSystem>(true)) ps.Play();
                Destroy(impact, data.impactLifetime);
            }
        }
        void ApplyHit(RaycastHit hit)
        {
            var target = hit.collider.GetComponentInParent<MiningCharacterHealth>();
            // Existing monster health remains the single owner of armor, hit reaction, death and rewards.
            if (target == null || target == health || target.Health <= 0 || target.GetComponent<MushroomMonster>() == null ||
                target.GetComponentInParent<TreasureChest>() != null || target.GetComponentInParent<TowerRuntime>() != null) return;
            float damage = CurrentWeapon.usePlayerDamage && stats != null ? stats.Damage * CurrentWeapon.damageMultiplier : CurrentWeapon.flatDamage;
            float trueBonus = stats != null ? target.MaxHealth * stats.ConsumableMonsterTrueDamageFraction : 0;
            float dealt = target.DealDamageWithTrueBonus(damage, CurrentWeapon.damageType, gameObject, trueBonus);
            if (dealt <= 0) return;
            if (stats != null && stats.Data != null) MiningDamagePopup.Show(dealt, hit.point, stats.Data.damagePopup);
            var bonuses = items != null ? items.EquipmentBonuses : null;
            if (bonuses != null)
            {
                if (health != null && health.Health > 0) health.Heal(dealt * bonuses.LifeStealFraction);
                target.ApplyBurn(bonuses.burnDamagePerTick, bonuses.burnTickSeconds, bonuses.burnDurationSeconds, gameObject);
            }
            HitConfirmed?.Invoke(dealt);
        }
        void SuspendInputs()
        {
            if (suspended) return;
            oldInputStates = new bool[incompatibleInputs == null ? 0 : incompatibleInputs.Length];
            for (int i = 0; i < oldInputStates.Length; i++) if (incompatibleInputs[i] != null && incompatibleInputs[i] != this && incompatibleInputs[i] != cameraRig)
            { oldInputStates[i] = incompatibleInputs[i].enabled; incompatibleInputs[i].enabled = false; }
            suspended = true;
        }
        void ResumeInputs()
        {
            if (!suspended) return;
            for (int i = 0; i < oldInputStates.Length; i++)
                if (incompatibleInputs[i] != null && incompatibleInputs[i] != this && incompatibleInputs[i] != cameraRig)
                    incompatibleInputs[i].enabled = oldInputStates[i];
            suspended = false;
        }
        void LoadProfile()
        {
            if (!saveAmmo) return;
            string json = GameSave.GetString(saveKey);
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var loaded = JsonUtility.FromJson<Profile>(json);
                if (loaded == null || loaded.version != 1 || loaded.weapons == null) throw new FormatException("Unsupported FPS save schema.");
                var ids = new HashSet<string>();
                foreach (var r in loaded.weapons)
                    if (r == null || string.IsNullOrEmpty(r.id) || r.loaded < 0 || r.reserve < 0 || !ids.Add(r.id))
                        throw new FormatException("Invalid FPS ammo record.");
                profile = loaded;
            }
            catch (Exception e) { profileWritable = false; Debug.LogWarning("FPS save preserved without overwriting: " + e.Message, this); }
        }
        void SaveProfile()
        {
            if (!initialized || !saveAmmo || !profileWritable) return;
            foreach (var pair in magazines)
            {
                var r = profile.weapons.Find(x => x.id == pair.Key);
                if (r == null) { r = new AmmoRecord { id = pair.Key }; profile.weapons.Add(r); }
                r.loaded = pair.Value.Loaded; r.reserve = pair.Value.Reserve;
            }
            if (CurrentWeapon != null) profile.selected = CurrentWeapon.weaponId;
            GameSave.SetString(saveKey, JsonUtility.ToJson(profile)); // Existing buffered save host flushes the profile.
            GameSave.Save();
        }
        public InputAction GetAction(string actionName)
        {
            var all = actions ?? new[] { fire, aim, reload, nextWeapon, firstWeapon, secondWeapon, freeCursor };
            foreach (var a in all) if (a != null && a.name == actionName) return a;
            return null;
        }
        public void SaveBindings()
        {
            var profile = new BindingProfile();
            foreach (var a in actions) for (int i = 0; i < a.bindings.Count; i++)
                if (a.bindings[i].overridePath != null) profile.bindings.Add(new BindingRecord { action = a.name, index = i, path = a.bindings[i].overridePath });
            PlayerPrefs.SetString(BindingsKey, JsonUtility.ToJson(profile)); PlayerPrefs.Save(); Changed?.Invoke();
        }
        void LoadBindings()
        {
            string json = PlayerPrefs.GetString(BindingsKey, ""); if (string.IsNullOrEmpty(json)) return;
            try
            {
                var p = JsonUtility.FromJson<BindingProfile>(json);
                if (p == null || p.bindings == null) return;
                foreach (var b in p.bindings)
                {
                    var a = GetAction(b.action);
                    if (a != null && b.index >= 0 && b.index < a.bindings.Count) a.ApplyBindingOverride(b.index, b.path);
                }
            }
            catch (Exception e) { Debug.LogWarning("Unable to load FPS keybinds: " + e.Message, this); }
        }
    }
}
