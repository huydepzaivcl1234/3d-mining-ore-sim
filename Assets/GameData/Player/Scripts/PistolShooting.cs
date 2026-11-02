using UnityEngine;
using UnityEngine.InputSystem;
using MiningSimulator.Ores;
namespace GameData.Player.Script
{
    [DefaultExecutionOrder(10030), DisallowMultipleComponent]
    public sealed partial class PistolShooting : MonoBehaviour
    {
        [SerializeField] private PistolWeaponData data;
        [SerializeField] private TMPro.TMP_Text ammoLabel;
        private bool authoredReloadActive;
        private FullBodyFirstPerson firstPerson;
        public bool UsesFullBody => firstPerson; // Disabling look must never reactivate the retained arms model.
        private Animation mechanicalAnimation;
        private Animation worldMechanicalAnimation;
        // Restore only gun parts. Sampling an Animator root resets its scene position too.
        private readonly System.Collections.Generic.List<PartRest> reloadParts = new();
        private struct PartRest
        {
            public Transform part;
            public Vector3 position, scale;
            public Quaternion rotation;
        }
        private void CaptureMechanism(Transform root)
        {
            if (!root) return;
            foreach (var part in root.GetComponentsInChildren<Transform>(true))
                reloadParts.Add(new PartRest {part=part,position=part.localPosition,rotation=part.localRotation,scale=part.localScale});
        }
        private AudioSource audioSource;
        private PlayerStats stats;
        private GunSpecialUpgrades specials;
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
            ResetBobbing();
            Ammo = MagazineCapacity; Reserve = data ? Mathf.Max(0, data.reserveAmmo) : 0;
            shotVfx?.Clear();
        }
        public float ReloadProgress => Reloading ? Mathf.Clamp01(reloadElapsed / Mathf.Max(.1f, reloadDuration)) : 0f;
        public int ShotsFired { get; private set; }
        private void Awake()
        {
            InitializeView();
            stats = GetComponent<PlayerStats>();
            specials = GetComponent<GunSpecialUpgrades>();
            audioSource = GetComponent<AudioSource>();
            if (!data) { enabled = false; return; }
            Ammo = MagazineCapacity; Reserve = Mathf.Max(0, data.reserveAmmo);
            BuildFlash();
            shotVfx = new PistolShotVfx(data, stats);
        }
        private void Update()
        {
            sharedBody?.BeginFrame();
            UpdateView();
            StepFeedback();
            if (!data) return;
            shotVfx?.Step(Time.deltaTime);
            if (ammoLabel) {
                ammoLabel.gameObject.SetActive(Equipped);
                string message=$"{data.displayName}   {Ammo}/{MagazineCapacity} · {Reserve}"+(Reloading ? "  Reloading" : "");
                if(ammoLabel.text!=message) ammoLabel.text=message;
            }
            if (Reloading) {
                if (!CanMaintainWeaponAction) CancelReload();
                else {
                    reloadElapsed += Time.deltaTime;
                    if (reloadElapsed >= reloadDuration) {
                        int count = Mathf.Min(Mathf.Max(0,MagazineCapacity - Ammo), Reserve);
                        Ammo += count; Reserve -= count; EndAuthoredReload(); Reloading = false;
                    }
                }
            }
            if (!CanFire) return;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) BeginReload();
        }
        private void LateUpdate()
        {
            ApplyView();
            ApplyMechanism();
            sharedBody?.Present();
            shotVfx?.Present();
            // Use this frame's camera and rendered muzzle after applying the FPS root.
            if (!data || !CanFire) return;
            if (Mouse.current != null && (data.automatic ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame)) TryFire();
        }
        public bool BeginReload()
        {
            if (!data || !CanFire || Reloading || Reserve <= 0 || Ammo >= MagazineCapacity) return false;
            EmptyReload = Ammo == 0;
            reloadElapsed = 0f; reloadDuration = Mathf.Max(.1f,data.reloadSeconds); Reloading = true;
            Aiming = false; aimBlend = 0f;
            requireAimRelease = Mouse.current != null && Mouse.current.rightButton.isPressed;
            if (playerAnimator) { playerAnimator.SetFloat(AimWeightId, 0f); playerAnimator.ResetTrigger("PistolAttack"); playerAnimator.SetBool("PistolAttacking", false); }
            if (bodyAnimator && bodyAnimator != playerAnimator) { bodyAnimator.SetFloat(AimWeightId, 0f); bodyAnimator.ResetTrigger("PistolAttack"); bodyAnimator.SetBool("PistolAttacking", false); }
            var profile = data.reloadProfile;
            if (profile && profile.authoredClip && playerAnimator)
            {
                authoredReloadActive = true;
                // Native Animator transitions own the pose; no direct bone writes or shared layer indexes.
                playerAnimator.SetBool(ReloadingId, true);
                if (bodyAnimator && bodyActionLayer >= 0) bodyAnimator.SetBool(ReloadingId, true);
                reloadParts.Clear();
                // Capture the closed mechanism, never a half-open slide from an interrupted shot.
                var shotAnchor = UsesFullBody && !RifleEquipped ? firstPerson.weapon : null;
                var restClip = shotAnchor && shotAnchor.referenceMechanismAttack ? shotAnchor.referenceMechanismAttack : profile.mechanismRestClip;
                var worldRestClip = shotAnchor && shotAnchor.referenceWorldMechanismAttack ? shotAnchor.referenceWorldMechanismAttack : restClip;
                if (gun && restClip) { var fire = gun.GetComponent<Animation>(); if (fire) fire.Stop(); restClip.SampleAnimation(gun.gameObject, 0f); }
                if (worldGun && worldGun != gun && worldRestClip) { var fire = worldGun.GetComponent<Animation>(); if (fire) fire.Stop(); worldRestClip.SampleAnimation(worldGun.gameObject, 0f); }
                CaptureMechanism(gun);
                if(worldGun!=gun)CaptureMechanism(worldGun);
                mechanicalAnimation = gun ? gun.GetComponent<Animation>() : null;
                if (mechanicalAnimation && profile.mechanismClip) {
                    mechanicalAnimation.Play(profile.mechanismClip.name);
                    mechanicalAnimation[profile.mechanismClip.name].speed = profile.mechanismClip.length / reloadDuration;
                }
                worldMechanicalAnimation = worldGun && worldGun!=gun ? worldGun.GetComponent<Animation>() : null;
                var worldClip = profile.worldMechanismClip ? profile.worldMechanismClip : profile.mechanismClip;
                if (worldMechanicalAnimation && worldClip) {
                    worldMechanicalAnimation.Play(worldClip.name);
                    worldMechanicalAnimation[worldClip.name].speed = worldClip.length / reloadDuration;
                }
            }
            if(audioSource && data.reloadProfile && data.reloadProfile.reloadSound)
                audioSource.PlayOneShot(data.reloadProfile.reloadSound,data.reloadProfile.soundVolume);
            return true;
        }
        private void EndAuthoredReload()
        {
            if (!authoredReloadActive) return;
            if (playerAnimator) playerAnimator.SetBool(ReloadingId, false);
            if (bodyAnimator && bodyActionLayer >= 0) bodyAnimator.SetBool(ReloadingId, false);
            if (mechanicalAnimation) mechanicalAnimation.Stop();
            mechanicalAnimation = null;
            if (worldMechanicalAnimation) worldMechanicalAnimation.Stop();
            worldMechanicalAnimation = null;
            foreach (var rest in reloadParts)
                if (rest.part) { rest.part.SetLocalPositionAndRotation(rest.position, rest.rotation); rest.part.localScale=rest.scale; }
            reloadParts.Clear();
            authoredReloadActive = false;
        }
        public void CancelReload() { EndAuthoredReload(); Reloading = false; reloadElapsed = 0f; }
        public bool TryFire()
        {
            if (!data || !CanFire || Reloading || Ammo <= 0 || Time.time < nextShot) return false;
            nextShot = Time.time + ShotInterval; Ammo--; ShotsFired++;
            Fired?.Invoke();
            var camera = ViewCamera;
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
            Vector3 start = Muzzle ? Muzzle.position : ray.origin;
            if (Vector3.Distance(start, end) > data.range) { end = start + (end-start).normalized * data.range; hasHit=false; }
            // A centered camera ray chooses aim; the muzzle ray prevents shooting through nearby walls.
            Vector3 path = end - start;
            int obstacles = Physics.RaycastNonAlloc(start, path.normalized, hits, path.magnitude, data.hitLayers, QueryTriggerInteraction.Collide);
            float closest = path.magnitude;
            for (int i=0;i<obstacles;i++) {
                if(hits[i].collider.transform.IsChildOf(transform) || MonsterHitbox.IgnoreShot(hits[i].collider) || hits[i].distance >= closest) continue;
                closest=hits[i].distance; impact=hits[i]; hasHit=true; end=impact.point;
            }
            // World-space tracers must start at the rendered muzzle despite the viewmodel FOV.
            // Keep the physical muzzle/raycast above unchanged.
            Vector3 visualStart=start;
            if(UsesViewmodel && weaponCamera && viewCamera)
                visualStart=viewCamera.ViewportToWorldPoint(weaponCamera.WorldToViewportPoint(start));
            var worldMuzzle=UsesViewmodel && worldGun && muzzle ? worldGun.Find(muzzle.name) : null;
            shotVfx?.Play(visualStart,end,hasHit ? impact.normal : -ray.direction,hasHit,
                UsesViewmodel ? viewCamera : null,UsesViewmodel ? weaponCamera : null,Muzzle,worldMuzzle);
            if(hasHit) {
                var target=impact.collider.GetComponentInParent<CharacterHealth>();
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
        private void OnDisable()
        {
            sharedBody?.Restore();
            Fired -= OnShot; ResetFeedback(); CancelReload(); shotVfx?.Clear();
            if(playerAnimator && layer>=0)playerAnimator.SetLayerWeight(layer,0f);
            if(bodyAnimator){bodyAnimator.SetInteger(WeaponModeId,0);bodyAnimator.SetTrigger(WeaponChangedId);}
            lastBodyMode=-1;
            if(bodyAnimator && bodyActionLayer>=0)bodyAnimator.SetLayerWeight(bodyActionLayer,0f);
            if(viewmodelRoot)viewmodelRoot.gameObject.SetActive(false);
        }
        private void OnDestroy() { sharedBody?.Dispose(); shotVfx?.Dispose(); if(flash) Destroy(flash); if(worldFlash) Destroy(worldFlash); }
        [SerializeField] private Transform slide;
        [SerializeField] private Transform muzzle;
        [Tooltip("Backwards in the slide parent's local coordinates; M1911 barrel points along +Z.")]
        [SerializeField] private Vector3 slideBackward = Vector3.back;
        
        private Vector3 closedSlidePosition;
        private GameObject flash;
        private ParticleSystem[] particles;
        private GameObject worldFlash;
        private ParticleSystem[] worldParticles;
        private float displacement, displacementVelocity, pitch, pitchVelocity;
        private float shotAge = float.PositiveInfinity;
        private float cameraPitch, cameraVelocity;
        private float yaw, yawVelocity, roll, rollVelocity;
        public float CameraRecoil => cameraPitch;
        public float Kickback => displacement;
        public float SlideOffset { get; private set; }
        public int FlashCount { get; private set; }
        public GameObject FlashInstance => flash;
        public Transform Muzzle => SharedBodyActive && sharedBody.Barrel ? sharedBody.Barrel : muzzle;
        

        public void ConfigureWeapon(Transform barrel, Transform mechanism)
        {
            ResetFeedback();
            if (flash) { flash.SetActive(false); Destroy(flash); }
            if (worldFlash) { worldFlash.SetActive(false); Destroy(worldFlash); }
            worldFlash=null;worldParticles=null;
            flash = null; particles = null;
            muzzle = UsesFullBody ? FindGunPart(barrel) : barrel;
            slide = UsesFullBody ? FindGunPart(mechanism) : mechanism;
            BuildFlash();
        }
        private void BuildFlash()
        {
            if (slide) closedSlidePosition = slide.localPosition;
            if (Data && Data.muzzleFlashPrefab && muzzle)
            {
                flash = Instantiate(Data.muzzleFlashPrefab, muzzle, false);
                flash.name = "Pistol Muzzle Flash (Reusable)";
                foreach (var child in flash.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = muzzle.gameObject.layer;
                flash.transform.localPosition = Vector3.zero;
                flash.transform.localRotation = Quaternion.identity;
                flash.transform.localScale = Vector3.one * Mathf.Max(.01f, Data.muzzleFlashScale);
                particles = flash.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var effect in particles)
                {
                    var main = effect.main;
                    main.loop = false; main.playOnAwake = false;
                    main.stopAction = ParticleSystemStopAction.None;
                    effect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            if (Data && Data.muzzleFlashPrefab && worldGun && muzzle)
            {
                var worldMuzzle=worldGun.Find(muzzle.name);
                if(worldMuzzle && worldMuzzle!=muzzle)
                {
                    worldFlash=Instantiate(Data.muzzleFlashPrefab,worldMuzzle,false);
                    worldFlash.name="World Muzzle Flash (Reusable)";
                    worldFlash.transform.localPosition=Vector3.zero;
                    worldFlash.transform.localRotation=Quaternion.identity;
                    worldFlash.transform.localScale=Vector3.one*Mathf.Max(.01f,Data.muzzleFlashScale);
                    foreach(var child in worldFlash.GetComponentsInChildren<Transform>(true))child.gameObject.layer=worldMuzzle.gameObject.layer;
                    worldParticles=worldFlash.GetComponentsInChildren<ParticleSystem>(true);
                    foreach(var effect in worldParticles)
                    {
                        var main=effect.main;main.loop=false;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;
                        effect.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
            }
        }
        private void OnEnable()
        {
            Fired += OnShot;
        }
        private void OnShot()
        {
            if (!Data) return;
            if(independentBodyAnimator)independentBodyAnimator.SetTrigger("PistolAttack");
            float weight = Mathf.Lerp(1f, Data.aimRecoilMultiplier, AimWeight);
            if(UsesFullBody && firstPerson.weapon && !firstPerson.weapon.PlayReferenceShot())firstPerson.weapon.Kick(Data.handKickback*weight,Data.recoilDegrees*weight,Data.recoilYawDegrees*weight);
            float frequency = 4f / Mathf.Max(.02f, Data.handReturnSeconds);
            // An impulse produces a quick rearward stroke, then a damped return without bouncing.
            displacementVelocity += Mathf.Max(0f, Data.handKickback) * frequency * 2.71828f * weight;
            pitchVelocity += Mathf.Max(0f, Data.recoilDegrees) * frequency * 2.71828f * weight;
            float side = (ShotsFired & 1) == 0 ? 1f : -1f;
            yawVelocity += side * Mathf.Max(0f, Data.recoilYawDegrees) * frequency * 2.71828f * weight;
            rollVelocity -= side * Mathf.Max(0f, Data.recoilRollDegrees) * frequency * 2.71828f * weight;
            float cameraFrequency = 4f / Mathf.Max(.02f, Data.cameraReturnSeconds);
            cameraVelocity += Mathf.Max(0f, Data.cameraRecoilDegrees) * cameraFrequency * 2.71828f * weight;
            shotAge = 0f;
            if (flash && particles != null)
            {
                foreach (var effect in particles)
                {
                    effect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    effect.Play(false);
                }
                FlashCount++;
            }
            if(worldParticles!=null)foreach(var effect in worldParticles)
            {
                effect.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.Play(false);
            }
        }
        private void StepFeedback()
        {
            if (!Data) return;
            if (!Equipped) { ResetFeedback(); return; }
            // The reload clip owns the mechanism in LateUpdate while the action is active.
            if (Reloading) { ResetFeedback(); return; }
            float dt = Time.deltaTime;
            float frequency = 4f / Mathf.Max(.02f, Data.handReturnSeconds);
            AdvanceSpring(ref displacement, ref displacementVelocity, frequency, dt);
            AdvanceSpring(ref pitch, ref pitchVelocity, frequency, dt);
            AdvanceSpring(ref yaw, ref yawVelocity, frequency, dt);
            AdvanceSpring(ref roll, ref rollVelocity, frequency, dt);
            AdvanceSpring(ref cameraPitch, ref cameraVelocity, 4f / Mathf.Max(.02f, Data.cameraReturnSeconds), dt);
            CameraShotKick = new Vector3(-cameraPitch, yaw * .3f, 0f);
            ShotDisplacement = new Vector3(0f, -displacement * .08f, -displacement);
            ShotKick = new Vector3(-pitch, yaw, roll);
            shotAge += dt;
            float open = Mathf.Max(.005f, Data.slideOpenSeconds);
            float close = Mathf.Max(.01f, Data.slideCloseSeconds);
            float cycle = shotAge < open ? shotAge / open : 1f - Mathf.Clamp01((shotAge - open) / close);
            SlideOffset = Mathf.Max(0f, Data.slideTravel) * Mathf.SmoothStep(0f, 1f, cycle);
            if (Ammo == 0) SlideOffset = Mathf.Max(0f, Data.slideTravel);
        }
        public static void AdvanceSpring(ref float position, ref float velocity, float frequency, float dt)
        {
            float term = velocity + frequency * position;
            float decay = Mathf.Exp(-frequency * dt);
            position = (position + term * dt) * decay;
            velocity = (velocity - frequency * term * dt) * decay;
        }
        private void ApplyMechanism()
        {
            // Reload's authored mechanism clip has sole ownership during reload.
            if (!slide || Reloading) return;
            if(UsesFullBody && firstPerson.weapon && firstPerson.weapon.UsesReference && !RifleEquipped) return;
            Vector3 direction = slideBackward.sqrMagnitude > .0001f ? slideBackward.normalized : Vector3.back;
            slide.localPosition = closedSlidePosition + direction * SlideOffset;
        }
        private void ResetFeedback()
        {
            displacement = displacementVelocity = pitch = pitchVelocity = SlideOffset = 0f;
            cameraPitch = cameraVelocity = 0f;
            yaw = yawVelocity = roll = rollVelocity = 0f;
            shotAge = float.PositiveInfinity;
            if (slide && !Reloading) slide.localPosition = closedSlidePosition;
            { ShotKick = Vector3.zero; ShotDisplacement = Vector3.zero; CameraShotKick = Vector3.zero; }
            if (particles != null) foreach (var effect in particles) effect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (worldParticles != null) foreach (var effect in worldParticles) effect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        [System.Serializable]
        public sealed class ViewFrame
        {
            public Vector3 position = new Vector3(-.006566f,-1.583307f,-.157071f), euler;
            [Range(20f,120f)] public float fov=85f;
            [Min(.01f)] public float nearClip=.12f;
            [Header("Aim frame authored in Blender (optional per weapon)")]
            public bool useAimFrame;
            public Vector3 aimPosition, aimEuler;
            [Range(20f,120f)] public float aimFov=63f;
            [Min(.01f)] public float aimNearClip=.06f;
            [Min(.01f)] public float aimSeconds=.18f;
        }
        [System.Serializable]
        public sealed class BobCycle
        {
            [Min(.1f)] public float strideMeters=2.8f;
            public AnimationCurve positionX=AnimationCurve.Constant(0,1,0),positionY=AnimationCurve.Constant(0,1,0),positionZ=AnimationCurve.Constant(0,1,0);
            public AnimationCurve pitch=AnimationCurve.Constant(0,1,0),yaw=AnimationCurve.Constant(0,1,0),roll=AnimationCurve.Constant(0,1,0);
            public Vector3 Position(float phase)=>new(positionX.Evaluate(phase),positionY.Evaluate(phase),positionZ.Evaluate(phase));
            public Vector3 Rotation(float phase)=>new(pitch.Evaluate(phase),yaw.Evaluate(phase),roll.Evaluate(phase));
        }
        [Header("Movement bob — curves authored in Blender")]
        [SerializeField] private BobCycle walkBob=new(),runBob=new();
        [SerializeField,Min(.01f)] private float bobBlendSeconds=.15f;
        [SerializeField,Range(0,1)] private float aimedBobMultiplier=.3f;
        private GameData.Player.Manager.InputManager movementInput;
        private AnimationRunMana runMana;
        private Vector3 lastBobPosition,bobPosition,bobRotation;
        private float bobPhase,bobWeight,runBobBlend,bobSpeed;
        public Vector3 BobbingOffset=>bobPosition;
        public Vector3 BobbingEuler=>bobRotation;
        private void ResetBobbing() {lastBobPosition=transform.position;bobPhase=bobWeight=runBobBlend=bobSpeed=0;bobPosition=bobRotation=Vector3.zero;}
        private void StepBobbing()
        {
            var delta=transform.position-lastBobPosition;lastBobPosition=transform.position;delta.y=0;
            float dt=Time.deltaTime;
            if(dt<=0)return;
            // Actual travelled distance, not input alone: pushing a wall must not bob.
            float distance=delta.magnitude;
            bool teleported=distance>=Mathf.Max(1f,dt*20f);
            // Physics and rendering run at different rates; smooth measured travel so
            // frames between physics steps do not repeatedly start/stop the bob.
            bobSpeed=teleported?0:Mathf.Lerp(bobSpeed,distance/dt,1-Mathf.Exp(-dt/.06f));
            bool moving=bobSpeed>.1f&&bodyAnimator&&bodyAnimator.GetBool("Grounded")&&alive&&Equipped&&!Reloading&&!inputBlocked;
            bool running=movementInput&&movementInput.Run&&(!runMana||runMana.CanRun);
            float change=dt/Mathf.Max(.01f,bobBlendSeconds);
            runBobBlend=Mathf.MoveTowards(runBobBlend,running?1:0,change);
            bobWeight=Mathf.MoveTowards(bobWeight,moving?Mathf.Clamp01(bobSpeed):0,change);
            if(moving)bobPhase=Mathf.Repeat(bobPhase+bobSpeed*dt/Mathf.Max(.1f,Mathf.Lerp(walkBob.strideMeters,runBob.strideMeters,runBobBlend)),1);
            float strength=bobWeight*Mathf.Lerp(1,aimedBobMultiplier,AimWeight);
            bobPosition=Vector3.Lerp(walkBob.Position(bobPhase),runBob.Position(bobPhase),runBobBlend)*strength;
            bobRotation=Vector3.Lerp(walkBob.Rotation(bobPhase),runBob.Rotation(bobPhase),runBobBlend)*strength;
        }
        [Header("Local FPS view — animation clips own the bones")]
        [SerializeField] private Animator playerAnimator;
        [Tooltip("Optional independent world-body Animator. FPS animation/IK remains on playerAnimator.")]
        [SerializeField] private Animator independentBodyAnimator;
        private bool worldWasGrounded=true;
        private static readonly int[] WorldFloatIds = { Animator.StringToHash("Blend"), Animator.StringToHash("X_velocity"), Animator.StringToHash("Y_velocity"), Animator.StringToHash("Z_velocity") };
        private static readonly int[] WorldBoolIds = { Animator.StringToHash("Grounded"), Animator.StringToHash("Falling"), Animator.StringToHash("Drawing"), Animator.StringToHash("PistolAttacking") };
        [SerializeField] private Camera viewCamera, weaponCamera;
        [SerializeField] private Transform gun, worldGun, viewmodelRoot;
        [SerializeField] private string poseLayer="Pistol Presentation";
        [SerializeField] private bool startEquipped=true;
        [SerializeField, Min(.01f)] private float blendSeconds=.22f;
        [SerializeField, HideInInspector] private ViewFrame viewFrame = new();
        private Animator bodyAnimator;
        private int bodyActionLayer=-1, weaponMode=1, lastBodyMode=-1;
        private CharacterHealth health;
        private UiPanelCoordinator ui;
        private GameMainMenu mainMenu;
        private int layer=-1;
        private static readonly int AimWeightId=Animator.StringToHash("AimWeight");
        private static readonly int ReloadingId=Animator.StringToHash("Reloading");
        private static readonly int WeaponModeId=Animator.StringToHash("WeaponMode");
        private static readonly int WeaponChangedId=Animator.StringToHash("WeaponChanged");
        private float blend, aimBlend;
        private bool requireAimRelease;
        private bool alive=true,inputBlocked,cursorReleased,previouslyBlocked,releasedBeforeUi;
        public bool WeaponAvailable { get; private set; } = true;
        public bool CanChangeWeapon => isActiveAndEnabled && alive && !inputBlocked && !Reloading && Cursor.lockState==CursorLockMode.Locked && Time.timeScale>0f;
        public void SetWeaponAvailable(bool value) { WeaponAvailable=value; if(!value)SetEquipped(false); }
        public bool Equipped { get; private set; }
        public bool RifleEquipped => weaponMode==2;
        public bool Aiming { get; private set; }
        public float AimWeight => Mathf.SmoothStep(0f,1f,aimBlend);
        public float PoseWeight => Mathf.SmoothStep(0f,1f,blend);
        public Camera ViewCamera => viewCamera;
        public Transform Gun => gun;
        public Transform WorldGun => worldGun;
        public Animator PoseAnimator => playerAnimator;
        public bool UsesViewmodel => !UsesFullBody && !SharedBodyActive && viewmodelRoot && weaponCamera;
        public bool CanFire => Equipped && PoseWeight>.95f && alive && !inputBlocked && (!UsesFullBody || !firstPerson.Drawing) && Cursor.lockState==CursorLockMode.Locked && Time.timeScale>0f;
        public bool CanMaintainWeaponAction => isActiveAndEnabled && Equipped && alive;
        public bool ShowCrosshair => CanFire && !Aiming && !Reloading;
        public Vector3 ShotKick {get;set;}
        public Vector3 CameraShotKick {get;set;}
        public Vector3 ShotDisplacement {get;set;}
        public void SetEquipped(bool value) { Equipped=value && WeaponAvailable; if(!value)CancelReload(); }
        public void Toggle()=>SetEquipped(!Equipped);
        public void SetCursorReleased(bool value) {
            cursorReleased=value;Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;
        }
        private void InitializeView() {
            health=GetComponent<CharacterHealth>();bodyAnimator=independentBodyAnimator?independentBodyAnimator:GetComponent<Animator>();
            firstPerson=GetComponent<FullBodyFirstPerson>();
            if(UsesFullBody){playerAnimator=firstPerson.animator?firstPerson.animator:GetComponent<Animator>();if(!independentBodyAnimator)gun=worldGun;if(viewmodelRoot)viewmodelRoot.gameObject.SetActive(false);if(weaponCamera)weaponCamera.enabled=false;}
            if(bodyAnimator)bodyActionLayer=bodyAnimator.GetLayerIndex("Weapon Actions");
            movementInput=GetComponent<GameData.Player.Manager.InputManager>();runMana=GetComponent<AnimationRunMana>();ResetBobbing();
            ui=FindAnyObjectByType<UiPanelCoordinator>(FindObjectsInactive.Include);
            mainMenu=FindAnyObjectByType<GameMainMenu>(FindObjectsInactive.Include);
            Equipped=startEquipped;
            if(!playerAnimator || !viewCamera || !gun){enabled=false;return;}
            layer=playerAnimator.GetLayerIndex(UsesFullBody?"Weapon Actions":poseLayer);
            weaponMode=poseLayer.StartsWith("Rifle")?2:1;
            if (!UsesFullBody && unifiedPistolBody && sharedLocalBody && sharedBodyMesh)
                sharedBody=new SharedBodyPose(this);
        }
        private void UpdateView() {
            if(layer<0 || !playerAnimator)return;
            alive=!health || health.Health>0f;
            inputBlocked=(ui && ui.BlocksGameplay)||(mainMenu && mainMenu.IsOpen);
            if(inputBlocked&&!previouslyBlocked){releasedBeforeUi=cursorReleased;SetCursorReleased(true);}
            if(!inputBlocked&&previouslyBlocked)SetCursorReleased(releasedBeforeUi);
            previouslyBlocked=inputBlocked;
            if(!inputBlocked && Keyboard.current!=null) {
                if(Keyboard.current.escapeKey.wasPressedThisFrame)SetCursorReleased(!cursorReleased);
                if(alive && Time.timeScale>0 && Keyboard.current.qKey.wasPressedThisFrame)Toggle();
            }
            if(!alive)Equipped=false;
            if (Mouse.current == null || !Mouse.current.rightButton.isPressed) requireAimRelease = false;
            Aiming=CanFire && !Reloading && !requireAimRelease && Mouse.current!=null && Mouse.current.rightButton.isPressed;
            aimBlend=Mathf.MoveTowards(aimBlend,Aiming?1f:0f,Time.deltaTime/Mathf.Max(.01f,viewFrame.aimSeconds));
            playerAnimator.SetFloat(AimWeightId,AimWeight);
            blend=Mathf.MoveTowards(blend,Equipped?1f:0f,Time.deltaTime/Mathf.Max(.01f,blendSeconds));
            playerAnimator.SetLayerWeight(layer,PoseWeight);
            if(bodyAnimator) {
                SyncIndependentWorldBody();
                bodyAnimator.SetFloat(AimWeightId,AimWeight);
                int mode=Equipped?weaponMode:0;
                if(lastBodyMode!=mode) {
                    bodyAnimator.SetInteger(WeaponModeId,mode);
                    bodyAnimator.SetTrigger(WeaponChangedId);
                    if(independentBodyAnimator && playerAnimator!=bodyAnimator){playerAnimator.SetInteger(WeaponModeId,mode);playerAnimator.SetTrigger(WeaponChangedId);}
                    lastBodyMode=mode;
                }
                if(bodyActionLayer>=0)bodyAnimator.SetLayerWeight(bodyActionLayer,
                    mode!=0 ? PoseWeight : 0f);
                // World-body locomotion must not replace the Blender-authored FPS grip.
            }
            if(gun)gun.gameObject.SetActive(blend>.001f);
            if(worldGun)worldGun.gameObject.SetActive(blend>.001f);
            if(viewmodelRoot)viewmodelRoot.gameObject.SetActive(!UsesFullBody && blend>.001f);
        }
        private void SyncIndependentWorldBody()
        {
            if(!independentBodyAnimator || !playerAnimator || playerAnimator==bodyAnimator)return;
            // Share intent/state only. Each Animator evaluates its own clips;
            // never copy transforms, humanoid poses or IK results between rigs.
            foreach(int id in WorldFloatIds)bodyAnimator.SetFloat(id,playerAnimator.GetFloat(id));
            foreach(int id in WorldBoolIds)bodyAnimator.SetBool(id,playerAnimator.GetBool(id));
            bool grounded=playerAnimator.GetBool(WorldBoolIds[0]);
            if(worldWasGrounded && !grounded && playerAnimator.GetFloat(WorldFloatIds[3])>.1f)
                bodyAnimator.SetTrigger("Jump");
            worldWasGrounded=grounded;
        }
        private void ApplyView() {
            if(UsesFullBody)return; // WeaponAnchor owns sway/bob/ADS/recoil, not this legacy FPS root.
            StepBobbing();
            if(!viewmodelRoot || !weaponCamera || !Equipped)return;
            float aim=viewFrame.useAimFrame?AimWeight:0f;
            viewmodelRoot.localPosition=Vector3.Lerp(viewFrame.position,viewFrame.aimPosition,aim)+ShotDisplacement+bobPosition;
            viewmodelRoot.localRotation=Quaternion.Slerp(Quaternion.Euler(viewFrame.euler),Quaternion.Euler(viewFrame.aimEuler),aim)*Quaternion.Euler(bobRotation)*Quaternion.Euler(ShotKick);
            weaponCamera.fieldOfView=Mathf.Lerp(viewFrame.fov,viewFrame.aimFov,aim);
            weaponCamera.nearClipPlane=Mathf.Lerp(viewFrame.nearClip,viewFrame.aimNearClip,aim);
        }
        public void ConfigureWeapon(Transform model,ViewFrame frame,string animationLayer,Transform worldModel=null) {
            if(gun)gun.gameObject.SetActive(false);
            if(worldGun)worldGun.gameObject.SetActive(false);
            if(layer>=0)playerAnimator.SetLayerWeight(layer,0f);
            if(bodyAnimator && bodyActionLayer>=0)bodyAnimator.SetLayerWeight(bodyActionLayer,0f);
            gun=UsesFullBody&&!independentBodyAnimator?worldModel:model;worldGun=worldModel;viewFrame=frame??new ViewFrame();poseLayer=animationLayer;
            layer=playerAnimator.GetLayerIndex(UsesFullBody?"Weapon Actions":poseLayer);blend=aimBlend=0f;
            weaponMode=poseLayer.StartsWith("Rifle")?2:1;
            ResetBobbing();
            if(!UsesFullBody)playerAnimator.Play(animationLayer.StartsWith("Rifle") ? "Rifle Base" : "Pistol Base",0,0f);
            if(UsesViewmodel){weaponCamera.fieldOfView=viewFrame.fov;weaponCamera.nearClipPlane=viewFrame.nearClip;ApplyView();}
        }
        private Transform FindGunPart(Transform original){
            if(!gun||!original)return null;
            foreach(var t in gun.GetComponentsInChildren<Transform>(true))if(t.name==original.name)return t;
            return null;
        }
    }
}
