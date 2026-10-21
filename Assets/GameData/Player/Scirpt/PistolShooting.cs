using UnityEngine;
using UnityEngine.InputSystem;
using MiningSimulator.Ores;
namespace GameData.Player.Script
{
    [DefaultExecutionOrder(110), DisallowMultipleComponent, RequireComponent(typeof(PistolPresentation))]
    public sealed class PistolShooting : MonoBehaviour
    {
        [SerializeField] private PistolWeaponData data;
        [SerializeField] private TMPro.TMP_Text ammoLabel;
        private PistolPresentation presentation;
        private AudioSource audioSource;
        private MiningPlayerStats stats;
        private GunSpecialUpgrades specials;
        private PistolShotFeedback feedback;
        private PistolShotVfx shotVfx;
        public int TracersPlayed => shotVfx != null ? shotVfx.TracersPlayed : 0;
        public int ImpactsPlayed => shotVfx != null ? shotVfx.ImpactsPlayed : 0;
        public int ActiveTracers => shotVfx != null ? shotVfx.ActiveTracers : 0;
        public int MagazineCapacity => data ? (int)System.Math.Min(int.MaxValue, (long)Mathf.Max(1, data.magazineSize) + (stats ? stats.GunCardMagazineBonus : 0)) : 0;
        public float BulletSpeed => data ? data.tracerSpeed * (stats ? stats.GunBulletSpeedMultiplier : 1f) : 0f;
        public float ShotInterval => data ? Mathf.Max(.02f,data.shotCooldown/(specials?specials.FireRateMultiplier:1f)) : .02f;
        public void SetWeaponData(PistolWeaponData weapon, int ammo, int reserve)
        {
            CancelReload(); shotVfx?.Dispose(); data = weapon;
            shotVfx = new PistolShotVfx(data, stats);
            Ammo = ammo < 0 ? MagazineCapacity : Mathf.Clamp(ammo, 0, MagazineCapacity);
            Reserve = reserve < 0 ? Mathf.Max(0, data.reserveAmmo) : Mathf.Max(0, reserve);
            nextShot = Time.time + ShotInterval;
        }
        private readonly RaycastHit[] hits = new RaycastHit[64];
        private float nextShot, reloadElapsed, reloadDuration;
        public event System.Action Fired;
        public PistolWeaponData Data => data;
        public int Ammo { get; private set; }
        public int Reserve { get; private set; }
        public bool Reloading { get; private set; }
        public bool EmptyReload { get; private set; }
        public float EffectiveDamage => data ? (stats ? stats.CalculateGunDamage(data.damage) : data.damage) : 0f;
        public void ResetState()
        {
            CancelReload(); nextShot = 0f; EmptyReload = false;
            Ammo = MagazineCapacity; Reserve = data ? Mathf.Max(0, data.reserveAmmo) : 0;
            shotVfx?.Clear();
        }
        public float ReloadProgress => Reloading ? Mathf.Clamp01(reloadElapsed / Mathf.Max(.1f, reloadDuration)) : 0f;
        public int ShotsFired { get; private set; }
        private void Awake()
        {
            presentation = GetComponent<PistolPresentation>();
            stats = GetComponent<MiningPlayerStats>();
            specials = GetComponent<GunSpecialUpgrades>();
            audioSource = GetComponent<AudioSource>();
            if (!data) { enabled = false; return; }
            Ammo = MagazineCapacity; Reserve = Mathf.Max(0, data.reserveAmmo);
            feedback = GetComponent<PistolShotFeedback>();
            shotVfx = new PistolShotVfx(data, stats);
        }
        private void Update()
        {
            if (!data || !presentation) return;
            shotVfx?.Step(Time.deltaTime);
            if (ammoLabel) {
                ammoLabel.gameObject.SetActive(presentation.Equipped);
                string message=$"{data.displayName}   {Ammo}/{MagazineCapacity} · {Reserve}"+(Reloading ? "  Reloading" : "");
                if(ammoLabel.text!=message) ammoLabel.text=message;
            }
            if (Reloading) {
                if (!presentation.CanMaintainWeaponAction) CancelReload();
                else {
                    reloadElapsed += Time.deltaTime;
                    if (reloadElapsed >= reloadDuration) {
                        int count = Mathf.Min(Mathf.Max(0,MagazineCapacity - Ammo), Reserve);
                        Ammo += count; Reserve -= count; Reloading = false;
                    }
                }
            }
            if (!presentation.CanFire) return;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) BeginReload();
        }
        private void LateUpdate()
        {
            // Presentation (order 100) finishes camera/IK first; use this frame's rendered muzzle and aim.
            if (!data || !presentation || !presentation.CanFire) return;
            if (Mouse.current != null && (data.automatic ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame)) TryFire();
        }
        public bool BeginReload()
        {
            if (!data || !presentation.CanFire || Reloading || Reserve <= 0 || Ammo >= MagazineCapacity) return false;
            EmptyReload = Ammo == 0;
            reloadElapsed = 0f; reloadDuration = Mathf.Max(.1f,data.reloadSeconds); Reloading = true;
            if(audioSource && data.reloadProfile && data.reloadProfile.reloadSound)
                audioSource.PlayOneShot(data.reloadProfile.reloadSound,data.reloadProfile.soundVolume);
            return true;
        }
        public void CancelReload() { Reloading = false; reloadElapsed = 0f; }
        public bool TryFire()
        {
            if (!data || !presentation.CanFire || Reloading || Ammo <= 0 || Time.time < nextShot) return false;
            nextShot = Time.time + ShotInterval; Ammo--; ShotsFired++;
            Fired?.Invoke();
            var camera = presentation.ViewCamera;
            var ray = camera.ViewportPointToRay(new Vector3(.5f,.5f,0));
            int count = Physics.RaycastNonAlloc(ray, hits, Mathf.Max(1,data.range), data.hitLayers, QueryTriggerInteraction.Collide);
            int nearest=-1; float distance=float.PositiveInfinity;
            for(int i=0;i<count;i++) {
                if(hits[i].collider.transform.IsChildOf(transform) || MonsterHitbox.IgnoreShot(hits[i].collider)) continue;
                if(hits[i].distance < distance) { nearest=i; distance=hits[i].distance; }
            }
            bool hasHit = nearest >= 0;
            RaycastHit impact = hasHit ? hits[nearest] : default;
            Vector3 end = hasHit ? impact.point : ray.GetPoint(Mathf.Max(1,data.range));
            Vector3 start = feedback && feedback.Muzzle ? feedback.Muzzle.position : ray.origin;
            if (Vector3.Distance(start, end) > data.range) { end = start + (end-start).normalized * data.range; hasHit=false; }
            // A centered camera ray chooses aim; the muzzle ray prevents shooting through nearby walls.
            Vector3 path = end - start;
            int obstacles = Physics.RaycastNonAlloc(start, path.normalized, hits, path.magnitude, data.hitLayers, QueryTriggerInteraction.Collide);
            float closest = path.magnitude;
            for (int i=0;i<obstacles;i++) {
                if(hits[i].collider.transform.IsChildOf(transform) || MonsterHitbox.IgnoreShot(hits[i].collider) || hits[i].distance >= closest) continue;
                closest=hits[i].distance; impact=hits[i]; hasHit=true; end=impact.point;
            }
            shotVfx?.Play(start,end,hasHit ? impact.normal : -ray.direction,hasHit);
            if(hasHit) {
                var target=impact.collider.GetComponentInParent<MiningCharacterHealth>();
                float shotDistance = Vector3.Distance(start, end);
                float falloff = data.DamageFalloffAtDistance(shotDistance);
                float damage = EffectiveDamage * falloff;
                if(damage > 0f && target && (target.GetComponentInParent<MushroomMonster>() || target.GetComponentInParent<GodBossRifleReward>())) {
                    float dealt=target.DealDamageWithTrueBonus(damage,CombatDamageType.Physical,gameObject,
                        stats != null ? target.MaxHealth * stats.ConsumableMonsterTrueDamageFraction * falloff : 0f,true);
                    if(specials)specials.OnBulletHit(target,damage,dealt,impact.point);
                }
            }
            if(audioSource && data.shotSound) audioSource.PlayOneShot(data.shotSound,data.soundVolume);
            return true;
        }
        private void OnDisable() { if(presentation) presentation.ShotKick=Vector3.zero; CancelReload(); shotVfx?.Clear(); }
        private void OnDestroy() { shotVfx?.Dispose(); }
    }
}
