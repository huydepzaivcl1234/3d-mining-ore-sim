using MiningSimulator.Ores;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace GameData.Player.Script
{
    /// <summary>Weapon grip, ADS and visual motion. Does not own movement or combat.</summary>
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class PistolPresentation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform gun;
        [SerializeField] private string poseLayer = "Pistol Presentation";
        [Range(0f,1f), SerializeField] private float torsoLookWeight = 1f;
        [Tooltip("The existing controller's camera socket on the original player skeleton.")]
        [SerializeField] private Transform cameraAnchor;
        private Transform lookChest;
        private Quaternion chestBeforeLook;
        private bool chestLookApplied;
        private Quaternion neutralChest;
        private Vector3 chestCameraOffset;
        private bool chestFrameReady;
        public Transform GetPoseBone(HumanBodyBones id)
        {
            var animator=playerAnimator ? playerAnimator : GetComponent<Animator>();
            return animator ? animator.GetBoneTransform(id) : null;
        }
        [Header("Persistent hand pose")]
        [SerializeField] private PistolHandPose savedPose;
        [SerializeField] private bool useSavedPose = true;
        [Tooltip("Use the imported FPS Shooter Aiming two-bone IK; do not also enable CoreAnimComponent on this rig.")]
        [SerializeField] private bool useImportedAimingIK = true;
        private PistolAimingRig aimingRig;
        public bool UsesImportedAimingIK => useImportedAimingIK && aimingRig != null;

        [Header("Draw / holster")]
        [SerializeField] private Key toggleKey = Key.Q;
        [SerializeField] private bool startEquipped;
        [SerializeField, Min(.01f)] private float blendSeconds = .22f;

        [Header("Aim - hold right mouse button")]
        [Tooltip("Camera-relative sight position. X/Y = 0 centers the Aim Point.")]
        [SerializeField] private Vector3 aimHandsPosition = new Vector3(0f, 0f, .20f);
        [SerializeField] private Vector3 aimHandsEuler;
        [SerializeField] private Transform aimPoint;
        [SerializeField, Min(.01f)] private float aimBlendSeconds = .18f;
        [SerializeField] private bool separateAimGunOffsets;
        [SerializeField] private Vector3 aimGunLocalPosition, aimGunLocalEuler;
        [Header("Scene editing")]
        [Tooltip("Turn on while authoring the ADS pose. Turn off to return to normal input.")]
        [SerializeField] private bool editAimPoseInScene;
        [Tooltip("Scene gizmo adjusts gun inside the hand instead of the whole hand pose.")]
        [SerializeField] private bool editGunInScene;

        [Header("Gun - relative to right hand")]
        [SerializeField] private Vector3 gunLocalPosition;
        [SerializeField] private Vector3 gunLocalEuler;
        [SerializeField, Min(.01f)] private float gunScale = 1f;

        [Header("Whole hand pose - relative to camera")]
        [Tooltip("Extra eye-to-grip distance in meters. Moves the armed view back, without stretching the original arms. Requires Camera Anchor.")]
        [SerializeField, Range(0f, .15f)] private float viewDistance;
        [Tooltip("Moves both hands and the gun together. Does not separate the supporting hand.")]
        [FormerlySerializedAs("rightHandPosition")]
        [SerializeField] private Vector3 handsPosition = new Vector3(.10f, -.14f, .28f);
        [Tooltip("Rotates the complete grip, preserving the wrist and finger pose from the clip.")]
        [SerializeField] private Vector3 handsEuler;
        // Imported grip orientation. Authored by the setup tool, not an independent wrist override.
        [SerializeField, HideInInspector] private Vector3 rightHandEuler;
        [SerializeField, HideInInspector] private Vector3 leftHandPosition;
        [SerializeField, HideInInspector] private Vector3 leftHandEuler;
        [Header("Elbow bend hints - relative to camera")]
        [SerializeField] private Vector3 rightElbowHint = new Vector3(.4f, -.4f, .12f);
        [SerializeField] private Vector3 leftElbowHint = new Vector3(-.4f, -.4f, .12f);

        [Header("Gentle movement bob")]
        [SerializeField] private Vector2 bobAmplitude = new Vector2(.004f, .008f);
        [SerializeField, Min(0f)] private float walkBobFrequency = 1.5f;
        [SerializeField, Min(0f)] private float runBobFrequency = 2.1f;
        [SerializeField, Min(0f)] private float runBobMultiplier = 1.3f;
        [SerializeField, Min(0f)] private float bobRotationDegrees = .4f;
        [SerializeField, Min(.1f)] private float bobSmoothing = 12f;

        private Transform rightUpper, rightLower, rightHand, leftUpper, leftLower, leftHand;
        private Transform[] animatedGripBones;
        private readonly Quaternion[] beforeGripRotations = new Quaternion[6];
        private bool animatedGripApplied;
        private MiningUiPanelCoordinator ui;
        private MiningMainMenu mainMenu;
        private MiningCharacterHealth health;
        private int layer = -1;
        private int lastToggleFrame = -1;
        private float blend, phase, originalLayerWeight;
        private Vector3 bob;
        private float bobRoll;
        private PistolShooting shooting;
        private PistolReloadVisual reloadVisual;
        private Transform[] leftGraspBones;
        public float ReloadMagazineGripError { get; private set; }
        public float ReloadHandWeight => reloadVisual != null ? reloadVisual.HandReleaseWeight : 0f;
        public float ReloadMagazineTravel => reloadVisual != null ? reloadVisual.MagazineTravel : 0f;
        public float ReloadSlideTravel => reloadVisual != null ? reloadVisual.SlideTravel : 0f;
        public float ReloadGripError { get; private set; }
        private readonly FpsWeaponMotion weaponMotion = new FpsWeaponMotion();
        private Quaternion previousViewRotation;
        private FpsWeaponMotionProfile previousMotionProfile;
        private bool hasPreviousView;
        public Vector3 MotionPosition => weaponMotion.Position;
        public Vector3 MotionEuler => weaponMotion.Euler;
        private PistolHandPose.BonePose[] cachedPoseBones;
        private Transform[] savedPoseTransforms;
        private float aimBlend;
        private bool cursorReleased, previouslyBlocked, releasedBeforeUi, inputBlocked;
        private bool alive = true;
        private readonly int xVelocity = Animator.StringToHash("X_velocity");
        private readonly int yVelocity = Animator.StringToHash("Y_velocity");
        private readonly int groundedHash = Animator.StringToHash("Grounded");
        private static readonly int LocomotionPose = Animator.StringToHash("PistolLocomotion");
        private bool hasLocomotionPose;

        public bool Equipped { get; private set; }
        public float PoseWeight => Mathf.SmoothStep(0f, 1f, blend);
        public float SupportGripError { get; private set; }
        public Camera ViewCamera => viewCamera;
        public Vector3 HandsPosition => handsPosition;
        public Vector3 HandsEuler => handsEuler;
        public float ViewDistance => viewDistance;
        public Vector3 AimHandsPosition => aimHandsPosition;
        public Vector3 AimHandsEuler => aimHandsEuler;
        public Transform Gun => gun;
        public bool CanFire => Equipped && PoseWeight > .95f && alive && !inputBlocked && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0f;
        public bool CanMaintainWeaponAction => isActiveAndEnabled && Equipped && alive;
        public Vector3 ShotKick { get; set; }
        public Vector3 CameraShotKick { get; set; }
        private Quaternion unmodifiedCameraRotation, appliedCameraRotation;
        private bool cameraKickApplied;
        // Camera-relative displacement shared by both arms; never changes the saved grip.
        public Vector3 ShotDisplacement { get; set; }
        public bool EditingAimPose => editAimPoseInScene;
        public bool EditingGun => editGunInScene;
        public Vector3 EditableGunPosition => editAimPoseInScene && separateAimGunOffsets ? aimGunLocalPosition : gunLocalPosition;
        public Vector3 EditableGunEuler => editAimPoseInScene && separateAimGunOffsets ? aimGunLocalEuler : gunLocalEuler;
        public bool Aiming { get; private set; }

        public float AimWeight => Mathf.SmoothStep(0f, 1f, aimBlend);
        public bool ShowCrosshair => Equipped && alive && !inputBlocked && Cursor.lockState == CursorLockMode.Locked && !Aiming && aimBlend < .05f && !(shooting && shooting.Reloading);

        public void SetHandPoseOffsets(Vector3 position, Vector3 euler, bool aim = false)
        {
            if (aim) { aimHandsPosition = position; aimHandsEuler = euler; }
            else { handsPosition = position; handsEuler = euler; }
        }

        public void SetGunOffsets(Vector3 position, Vector3 euler, bool aim)
        {
            if (aim) { separateAimGunOffsets = true; aimGunLocalPosition = position; aimGunLocalEuler = euler; }
            else { gunLocalPosition = position; gunLocalEuler = euler; }
            if (!Application.isPlaying && gun) { gun.localPosition = position; gun.localRotation = Quaternion.Euler(euler); }
        }

        public void SetCursorReleased(bool released)
        {
            cursorReleased = released;
            Cursor.lockState = released ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = released;
            if (released) Aiming = false;
        }

        private void Awake()
        {
            if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
            if (playerAnimator == null || !playerAnimator.isHuman || viewCamera == null || gun == null)
            {
                Debug.LogError("PistolPresentation needs a Humanoid Animator, camera and gun.", this);
                enabled = false;
                return;
            }
            layer = playerAnimator.GetLayerIndex(poseLayer);
            if (layer < 0)
            {
                Debug.LogError("PistolPresentation pose layer is missing.", this);
                enabled = false;
                return;
            }
            originalLayerWeight = playerAnimator.GetLayerWeight(layer);
            rightUpper = GetPoseBone(HumanBodyBones.RightUpperArm);
            rightLower = GetPoseBone(HumanBodyBones.RightLowerArm);
            rightHand = GetPoseBone(HumanBodyBones.RightHand);
            leftUpper = GetPoseBone(HumanBodyBones.LeftUpperArm);
            leftLower = GetPoseBone(HumanBodyBones.LeftLowerArm);
            leftHand = GetPoseBone(HumanBodyBones.LeftHand);
            animatedGripBones = new[] { rightUpper, rightLower, rightHand, leftUpper, leftLower, leftHand };
            leftGraspBones = new[] {
                GetPoseBone(HumanBodyBones.LeftIndexDistal),
                GetPoseBone(HumanBodyBones.LeftMiddleDistal),
                GetPoseBone(HumanBodyBones.LeftRingDistal),
                GetPoseBone(HumanBodyBones.LeftLittleDistal)
            };
            if (!rightUpper || !rightLower || !rightHand || !leftUpper || !leftLower || !leftHand)
            {
                Debug.LogError("PistolPresentation arm bones are incomplete.", this);
                enabled = false;
                return;
            }
            ui = FindAnyObjectByType<MiningUiPanelCoordinator>(FindObjectsInactive.Include);
            mainMenu = FindAnyObjectByType<MiningMainMenu>(FindObjectsInactive.Include);
            health = GetComponent<MiningCharacterHealth>();
            shooting = GetComponent<PistolShooting>();
            Equipped = startEquipped;
            foreach (var parameter in playerAnimator.parameters)
                if (parameter.nameHash == LocomotionPose && parameter.type == AnimatorControllerParameterType.Float) hasLocomotionPose = true;
            // Main menu may already have released the cursor; that is not an
            // explicit player request to keep it released after returning to gameplay.
            cursorReleased = false;
            LoadSavedPose();
            gun.SetParent(rightHand, false);
            ApplyGunOffset();
            reloadVisual = new PistolReloadVisual(shooting && shooting.Data ? shooting.Data.reloadProfile : null, gun);
            aimingRig = new PistolAimingRig(transform);
            gun.gameObject.SetActive(startEquipped);
            playerAnimator.SetLayerWeight(layer, 0f);
        }

        private void Update()
        {
            RestoreCameraKick();
            RestoreChestLook();
            RestoreAnimatedGrip();
            if (layer < 0) return;
            alive = health == null || health.Health > 0f;
            bool blocked = (ui != null && ui.BlocksGameplay) || (mainMenu != null && mainMenu.IsOpen);
            inputBlocked = blocked;
            if (blocked && !previouslyBlocked) { releasedBeforeUi = cursorReleased; SetCursorReleased(true); }
            if (!blocked && previouslyBlocked) SetCursorReleased(releasedBeforeUi);
            previouslyBlocked = blocked;
            if (!blocked && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetCursorReleased(!cursorReleased);
            if (alive && !blocked && Time.timeScale > 0f && Keyboard.current != null &&
                Keyboard.current[toggleKey].wasPressedThisFrame && lastToggleFrame != Time.frameCount)
            {
                lastToggleFrame = Time.frameCount;
                Toggle();
            }
            if (!alive) Equipped = false;
            Aiming = Equipped && alive && !blocked && !(shooting && shooting.Reloading) && (editAimPoseInScene ||
                (Time.timeScale > 0f && Cursor.lockState == CursorLockMode.Locked && Mouse.current != null && Mouse.current.rightButton.isPressed));
            aimBlend = editAimPoseInScene && Aiming ? 1f : Mathf.MoveTowards(aimBlend, Aiming ? 1f : 0f, Time.deltaTime / Mathf.Max(.01f, aimBlendSeconds));
            blend = Mathf.MoveTowards(blend, Equipped ? 1f : 0f, Time.deltaTime / Mathf.Max(.01f, blendSeconds));
            // Keep the user's weapon animation on the original full-body rig.
            playerAnimator.SetLayerWeight(layer, PoseWeight);
            if (hasLocomotionPose) playerAnimator.SetFloat(LocomotionPose, PoseWeight);
            gun.gameObject.SetActive(blend > .001f);
        }

        public void Toggle() => SetEquipped(!Equipped);
        public void SetEquipped(bool value) => Equipped = value;

        public void ConfigureWeapon(Transform model, PistolHandPose pose, string animationLayer, Transform sight, Vector3 wristEuler)
        {
            RestoreCameraKick(); RestoreChestLook();
            RestoreAnimatedGrip();
            if (gun) gun.gameObject.SetActive(false);
            if (layer >= 0) playerAnimator.SetLayerWeight(layer, 0f);
            reloadVisual?.Dispose();
            gun = model; savedPose = pose; poseLayer = animationLayer; aimPoint = sight; rightHandEuler = wristEuler;
            layer = playerAnimator.GetLayerIndex(poseLayer);
            originalLayerWeight = 0f; cachedPoseBones = null;
            aimBlend = blend = 0f; Aiming = false;
            weaponMotion.Reset(); hasPreviousView = false;
            LoadSavedPose(); gun.SetParent(rightHand, false); ApplyGunOffset();
            reloadVisual = new PistolReloadVisual(shooting.Data.reloadProfile, gun);
            gun.gameObject.SetActive(Equipped);
        }

        public void LoadSavedPose()
        {
            if (!useSavedPose || savedPose == null || !savedPose.valid) return;
            handsPosition = savedPose.handsPosition;
            handsEuler = savedPose.handsEuler;
            viewDistance = savedPose.viewDistance;
            gunLocalPosition = savedPose.gunLocalPosition;
            gunLocalEuler = savedPose.gunLocalEuler;
            gunScale = savedPose.gunScale;
            if (savedPose.useAnimationPose) rightHandEuler = savedPose.animationWristEuler;
            rightElbowHint = savedPose.rightElbowHint;
            leftElbowHint = savedPose.leftElbowHint;
            if (savedPose.hasAimSettings)
            {
                aimHandsPosition = savedPose.aimHandsPosition; aimHandsEuler = savedPose.aimHandsEuler;
                separateAimGunOffsets = savedPose.separateAimGunOffsets;
                aimGunLocalPosition = savedPose.aimGunLocalPosition; aimGunLocalEuler = savedPose.aimGunLocalEuler;
                if (aimPoint) aimPoint.localPosition = savedPose.aimPointLocalPosition;
            }
        }

        private void ApplySavedBones()
        {
            if (!useSavedPose || savedPose == null || !savedPose.valid || savedPose.useAnimationPose && !savedPose.useAuthoredFingers || savedPose.bones == null) return;
            if (cachedPoseBones != savedPose.bones)
            {
                cachedPoseBones = savedPose.bones;
                savedPoseTransforms = new Transform[cachedPoseBones.Length];
                for (int i = 0; i < cachedPoseBones.Length; i++)
                    if (PistolHandPose.IsArmBone(cachedPoseBones[i].bone) && (!savedPose.useAnimationPose || cachedPoseBones[i].bone >= HumanBodyBones.LeftThumbProximal && cachedPoseBones[i].bone <= HumanBodyBones.RightLittleDistal))
                        savedPoseTransforms[i] = GetPoseBone(cachedPoseBones[i].bone);
            }
            for (int i = 0; i < cachedPoseBones.Length; i++)
            {
                Transform bone = savedPoseTransforms[i];
                if (bone == null) continue;
                bone.localPosition = Vector3.Lerp(bone.localPosition, cachedPoseBones[i].position, PoseWeight);
                bone.localRotation = Quaternion.Slerp(bone.localRotation, cachedPoseBones[i].rotation, PoseWeight);
            }
        }

        private void LateUpdate()
        {
            if (layer < 0 || !gun || !viewCamera || !rightHand) return;
            bool reloading = shooting && shooting.Reloading && reloadVisual != null && reloadVisual.Valid;
            if (reloading) reloadVisual.Sample(shooting.ReloadProgress, shooting.Data.reloadSeconds);
            else reloadVisual?.Reset();
            // Look sway follows controller aim, not our own visual recoil impulse.
            Quaternion currentView = viewCamera.transform.rotation;
            if (Equipped && !inputBlocked && CameraShotKick.sqrMagnitude > .000001f)
            {
                unmodifiedCameraRotation = viewCamera.transform.localRotation;
                appliedCameraRotation = unmodifiedCameraRotation * Quaternion.Euler(CameraShotKick);
                viewCamera.transform.localRotation = appliedCameraRotation;
                cameraKickApplied = true;
            }
            // Profile motion replaces legacy bob when assigned, leaving older weapons unchanged.
            var profile = shooting && shooting.Data ? shooting.Data.motionProfile : null;
            if (!profile)
            {
                float gait = new Vector2(playerAnimator.GetFloat(xVelocity), playerAnimator.GetFloat(yVelocity)).magnitude;
                bool moving = gait > .08f && playerAnimator.GetBool(groundedHash);
                float running = Mathf.Clamp01((gait - 2f) / 4f);
                phase += Time.deltaTime * Mathf.Lerp(walkBobFrequency, runBobFrequency, running) * Mathf.PI * 2f;
                float intensity = moving ? PoseWeight * Mathf.Lerp(1f, runBobMultiplier, running) : 0f;
                Vector3 desiredBob = new Vector3(Mathf.Sin(phase) * bobAmplitude.x, Mathf.Sin(phase * 2f) * bobAmplitude.y, 0f) * intensity;
                float smoothing = 1f - Mathf.Exp(-bobSmoothing * Time.deltaTime);
                bob = Vector3.Lerp(bob, desiredBob, smoothing);
                bobRoll = Mathf.Lerp(bobRoll, Mathf.Sin(phase) * bobRotationDegrees * intensity, smoothing);
            }
            else { bob = Vector3.zero; bobRoll = 0f; }
            bool motionActive = profile && Equipped && alive && !inputBlocked &&
                Cursor.lockState == CursorLockMode.Locked && !editAimPoseInScene && !editGunInScene && !reloading && Time.timeScale > 0f;
            if (profile != previousMotionProfile) { weaponMotion.Reset(); hasPreviousView = false; previousMotionProfile = profile; }
            if (motionActive)
            {
                Vector2 look = hasPreviousView ? FpsWeaponMotion.LookVelocity(previousViewRotation, currentView, Time.deltaTime) : Vector2.zero;
                weaponMotion.Step(profile, look, new Vector2(playerAnimator.GetFloat(xVelocity), playerAnimator.GetFloat(yVelocity)),
                    playerAnimator.GetBool(groundedHash), AimWeight, Time.deltaTime);
            }
            else weaponMotion.Reset();
            if (Time.deltaTime > 0f) { previousViewRotation = currentView; hasPreviousView = motionActive; }
            if (PoseWeight > .001f) ApplyGripPose(reloading);
            ApplyGunOffset();
        }

        // One authoritative grip/ADS/IK path for both Editor authoring and gameplay.
        private void ApplyGripPose(bool reloading)
        {
            RestoreChestLook();
            if(!lookChest)lookChest=GetPoseBone(HumanBodyBones.UpperChest) ?? GetPoseBone(HumanBodyBones.Chest);
            // Measure in shoulder space: camera pitch and root-motion extraction
            // must not become a second rotation of the authored grip.
            bool animated = useSavedPose && savedPose && savedPose.useAnimationPose && lookChest;
            Transform animationFrame = lookChest ? lookChest.parent : transform;
            Quaternion animatedRotation = Quaternion.identity;
            Vector3 animatedPosition = Vector3.zero;
            if (animated)
            {
                for (int i = 0; i < animatedGripBones.Length; i++) beforeGripRotations[i] = animatedGripBones[i].localRotation;
                animatedGripApplied = true;
                Quaternion reference = Quaternion.Inverse(savedPose.animationReferenceRotation);
                animatedPosition = reference * (animationFrame.InverseTransformPoint(rightHand.position) - savedPose.animationReferencePosition);
                animatedRotation = reference * Quaternion.Inverse(animationFrame.rotation) * rightHand.rotation;
            }
            if(lookChest && torsoLookWeight>0f)
            {
                if(!chestFrameReady)
                {
                    neutralChest=Quaternion.Inverse(transform.rotation)*lookChest.rotation;
                    chestCameraOffset=lookChest.InverseTransformPoint(cameraAnchor ? cameraAnchor.position : viewCamera.transform.position);
                    chestFrameReady=true;
                }
                chestBeforeLook=lookChest.localRotation;
                Vector3 forward=Vector3.ProjectOnPlane(viewCamera.transform.forward,transform.up).normalized;
                Quaternion yaw=Quaternion.LookRotation(forward,transform.up);
                float pitch=Vector3.SignedAngle(forward,viewCamera.transform.forward,yaw*Vector3.right);
                var facing=yaw*Quaternion.Euler(pitch,0f,0f)*neutralChest;
                lookChest.rotation=Quaternion.Slerp(lookChest.rotation,facing,torsoLookWeight*PoseWeight);
                chestLookApplied=true;
            }
            // Sample the socket AFTER the torso pose, so the eye and shoulders
            // share one frame of the original skeleton rather than drifting apart.
            float distance = cameraAnchor ? viewDistance : 0f;
            if(lookChest && chestFrameReady && cameraAnchor)
                viewCamera.transform.position=Vector3.Lerp(viewCamera.transform.position,lookChest.TransformPoint(chestCameraOffset) - viewCamera.transform.forward * distance,PoseWeight);
            ApplySavedBones();
            Transform camera = viewCamera.transform;
            // Read the animated grip before IK. The support hand stays in the clip's
            // right-hand space, even if the dominant arm cannot reach a requested offset.
            Vector3 supportPosition = rightHand.InverseTransformPoint(leftHand.position);
            Quaternion supportRotation = Quaternion.Inverse(rightHand.rotation) * leftHand.rotation;
            if (animated && savedPose.useAuthoredSupport)
            {
                supportPosition = savedPose.supportPosition;
                supportRotation = Quaternion.Euler(savedPose.supportEuler);
            }
            Quaternion placement = Quaternion.Slerp(Quaternion.Euler(handsEuler), Quaternion.Euler(aimHandsEuler), AimWeight);
            Quaternion rotation = camera.rotation * placement * Quaternion.Euler(0f, 0f, bobRoll * (1f - AimWeight * .8f));
            Quaternion wristRotation = rotation * Quaternion.Euler(rightHandEuler) * animatedRotation;
            // The barrel uses local +Z. Align the weapon, not just its sight position.
            Quaternion localGunRotation = Quaternion.Euler(separateAimGunOffsets ? aimGunLocalEuler : gunLocalEuler);
            Quaternion sightRotation = aimPoint && aimPoint.parent == gun ? aimPoint.localRotation : Quaternion.identity;
            wristRotation = Quaternion.Slerp(wristRotation, camera.rotation * Quaternion.Euler(aimHandsEuler) * Quaternion.Inverse(localGunRotation * sightRotation), AimWeight);
            wristRotation = camera.rotation * Quaternion.Euler(weaponMotion.Euler * PoseWeight + ShotKick) * Quaternion.Inverse(camera.rotation) * wristRotation;
            if (reloading) wristRotation *= localGunRotation * reloadVisual.WeaponRotation * Quaternion.Inverse(localGunRotation);
            Vector3 depthOffset = Vector3.forward * distance;
            Vector3 aimTarget = camera.TransformPoint(aimHandsPosition + depthOffset + bob * .2f);
            if (aimPoint && aimPoint.parent == gun)
            {
                Vector3 gunOffset = separateAimGunOffsets ? aimGunLocalPosition : gunLocalPosition;
                Quaternion gunRotation = Quaternion.Euler(separateAimGunOffsets ? aimGunLocalEuler : gunLocalEuler);
                Vector3 sightOffset = gunOffset + gunRotation * (aimPoint.localPosition * gunScale);
                aimTarget -= wristRotation * Vector3.Scale(sightOffset, rightHand.lossyScale);
            }
            Vector3 supportOffset = wristRotation * Vector3.Scale(supportPosition, rightHand.lossyScale);
            // Keep the common grip inside both reach spheres instead of clamping
            // the wrists independently and pulling the support hand away.
            float rightReach = ArmReach(rightUpper, rightLower, rightHand);
            float leftReach = ArmReach(leftUpper, leftLower, leftHand);
            bool keepSupport = !reloading || reloadVisual.HandReleaseWeight < .001f;
            // Blend reachable endpoints; changing the constraint only at full ADS
            // causes a depth snap in the final frames of the transition.
            Vector3 hipPosition = camera.TransformPoint(handsPosition + depthOffset + bob);
            if (animated) hipPosition += camera.rotation * Quaternion.Euler(handsEuler) * Quaternion.Euler(rightHandEuler) * animatedPosition;
            Vector3 hipTarget = ClampGrip(hipPosition, supportOffset, rightReach, leftReach, keepSupport, 6);
            if (!reloading && aimPoint)
                ConstrainAimDepth(ref aimTarget, camera.forward, supportOffset, rightUpper.position, rightReach, leftUpper.position, leftReach);
            Vector3 target = Vector3.Lerp(hipTarget, aimTarget, AimWeight);
            target += camera.TransformVector(weaponMotion.Position * PoseWeight);
            if (reloading) target += camera.TransformVector(reloadVisual.WeaponPosition);
            target = ClampGrip(target, supportOffset, rightReach, leftReach, keepSupport, 6);
            // A hip pose can sit on the reach boundary. Apply kick after resolving the
            // resting grip, toward the shoulders, rather than clipping it away at that boundary.
            Vector3 kick = camera.TransformVector(ShotDisplacement);
            Vector3 inward = ((rightUpper.position + leftUpper.position) * .5f - target).normalized;
            kick = Vector3.Lerp(inward * ShotDisplacement.magnitude, kick, AimWeight);
            target += kick;
            target = ClampGrip(target, supportOffset, rightReach, leftReach, keepSupport, 3);
            SolveArm(rightUpper, rightLower, rightHand, target, wristRotation, camera.TransformPoint(rightElbowHint), PoseWeight);
            Vector3 supportTarget = rightHand.TransformPoint(supportPosition);
            Quaternion leftRotation = rightHand.rotation * supportRotation;
            Vector3 leftTarget = supportTarget;
            Vector3 graspPoint = Vector3.zero;
            if (reloading)
            {
                Vector3 graspOffset = leftHand.InverseTransformPoint(LeftGraspCenter());
                ApplyGunOffset();
                reloadVisual.ApplyMechanism(shooting.EmptyReload, shooting.Data.slideTravel);
                Quaternion magazineDelta = reloadVisual.Magazine.rotation * Quaternion.Inverse(reloadVisual.Magazine.parent.rotation);
                Quaternion graspRotation = magazineDelta * leftRotation * Quaternion.Euler(reloadVisual.LeftGripEuler);
                graspPoint = reloadVisual.MagazineGripPoint;
                Vector3 magazineTarget = graspPoint - graspRotation * Vector3.Scale(graspOffset, leftHand.lossyScale);
                Vector3 actionTarget = Vector3.Lerp(reloadVisual.PickupHandPoint, magazineTarget, reloadVisual.PickupBlend);
                leftTarget = Vector3.Lerp(supportTarget, actionTarget, reloadVisual.HandReleaseWeight);
                leftRotation = Quaternion.Slerp(leftRotation, graspRotation, reloadVisual.HandReleaseWeight);
                // Locomotion can lean the shoulders away from the reload path. Move
                // the common weapon pose into both reach spheres, never stretch a wrist
                // independently or detach the magazine from its authored trajectory.
                Vector3 actionOffset = leftTarget - rightHand.position;
                Vector3 reachableRight = ClampGrip(rightHand.position, actionOffset, rightReach, leftReach, true, 8);
                if ((reachableRight - rightHand.position).sqrMagnitude > .00000001f)
                {
                    SolveArm(rightUpper, rightLower, rightHand, reachableRight, wristRotation, camera.TransformPoint(rightElbowHint), PoseWeight);
                    supportTarget = rightHand.TransformPoint(supportPosition);
                    graspPoint = reloadVisual.MagazineGripPoint;
                    magazineTarget = graspPoint - graspRotation * Vector3.Scale(graspOffset, leftHand.lossyScale);
                    actionTarget = Vector3.Lerp(reloadVisual.PickupHandPoint, magazineTarget, reloadVisual.PickupBlend);
                    leftTarget = Vector3.Lerp(supportTarget, actionTarget, reloadVisual.HandReleaseWeight);
                }
            }
            SolveArm(leftUpper, leftLower, leftHand, leftTarget, leftRotation, camera.TransformPoint(leftElbowHint), PoseWeight);
            SupportGripError = Vector3.Distance(leftHand.position, supportTarget);
            ReloadGripError = Vector3.Distance(leftHand.position, leftTarget);
            ReloadMagazineGripError = reloading && reloadVisual.MagazineInHand && reloadVisual.HandReleaseWeight > .99f ? Vector3.Distance(LeftGraspCenter(), graspPoint) : 0f;
        }

        private Vector3 ClampGrip(Vector3 target, Vector3 supportOffset, float rightReach, float leftReach, bool keepSupport, int iterations)
        {
            Vector3 rightRoot = rightUpper.position, leftRoot = leftUpper.position;
            for(int i=0;i<iterations;i++)
            {
                target=rightRoot+Vector3.ClampMagnitude(target-rightRoot,rightReach);
                if(keepSupport)target=leftRoot+Vector3.ClampMagnitude(target+supportOffset-leftRoot,leftReach)-supportOffset;
            }
            return target;
        }
        private void RestoreChestLook()
        {
            if(chestLookApplied && lookChest)lookChest.localRotation=chestBeforeLook;
            chestLookApplied=false;
        }
        private void RestoreAnimatedGrip()
        {
            if (!animatedGripApplied) return;
            for (int i = 0; i < animatedGripBones.Length; i++)
                if (animatedGripBones[i]) animatedGripBones[i].localRotation = beforeGripRotations[i];
            animatedGripApplied = false;
        }
        private static float ArmReach(Transform upper, Transform lower, Transform hand)
        {
            return Mathf.Max(.0001f, Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position) - .001f);
        }

        private static bool ConstrainAimDepth(ref Vector3 target, Vector3 axis, Vector3 supportOffset,
            Vector3 rightRoot, float rightReach, Vector3 leftRoot, float leftReach)
        {
            float low = float.NegativeInfinity, high = float.PositiveInfinity;
            bool Sphere(Vector3 delta, float reach)
            {
                float b = Vector3.Dot(delta, axis);
                float discriminant = b*b-delta.sqrMagnitude+reach*reach;
                if(discriminant < 0f) return false;
                float root = Mathf.Sqrt(discriminant);
                low = Mathf.Max(low,-b-root); high = Mathf.Min(high,-b+root);
                return low <= high;
            }
            if(!Sphere(target-rightRoot,rightReach) || !Sphere(target+supportOffset-leftRoot,leftReach))return false;
            target += axis*Mathf.Clamp(0f,low,high);
            return true;
        }

        private Vector3 LeftGraspCenter()
        {
            Vector3 point = Vector3.zero; int count = 0;
            foreach (var bone in leftGraspBones) if (bone) { point += bone.position; count++; }
            return count > 0 ? point / count : leftHand.position;
        }

        private void ApplyGunOffset()
        {
            if (!gun) return;
            gun.localPosition = Vector3.Lerp(gunLocalPosition, separateAimGunOffsets ? aimGunLocalPosition : gunLocalPosition, AimWeight);
            gun.localRotation = Quaternion.Slerp(Quaternion.Euler(gunLocalEuler), Quaternion.Euler(separateAimGunOffsets ? aimGunLocalEuler : gunLocalEuler), AimWeight);
            gun.localScale = Vector3.one * Mathf.Max(.01f, gunScale);
        }

        private void SolveArm(Transform upper, Transform lower, Transform hand, Vector3 target, Quaternion rotation, Vector3 hint, float weight)
        {
            if (UsesImportedAimingIK)
            {
                aimingRig.Solve(upper == rightUpper, upper, lower, hand, target, rotation, hint, weight);
                return;
            }
            target = Vector3.Lerp(hand.position, target, weight);
            Vector3 root = upper.position;
            float first = Vector3.Distance(root, lower.position), second = Vector3.Distance(lower.position, hand.position);
            if (first < .0001f || second < .0001f) return;
            Vector3 direction = target - root;
            if (direction.sqrMagnitude < .000001f) return;
            float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(first - second) + .0001f, first + second - .0001f);
            direction.Normalize();
            Vector3 bend = Vector3.ProjectOnPlane(hint - root, direction);
            if (bend.sqrMagnitude < .000001f) bend = Vector3.ProjectOnPlane(lower.position - root, direction);
            if (bend.sqrMagnitude < .000001f) bend = Vector3.Cross(direction, upper.right);
            bend.Normalize();
            float cosine = Mathf.Clamp((first * first + distance * distance - second * second) / (2f * first * distance), -1f, 1f);
            Vector3 elbow = root + direction * (first * cosine) + bend * (first * Mathf.Sqrt(1f - cosine * cosine));
            Quaternion originalHandRotation = hand.rotation;
            upper.rotation = Quaternion.FromToRotation(lower.position - root, elbow - root) * upper.rotation;
            Vector3 clampedTarget = root + direction * distance;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, clampedTarget - lower.position) * lower.rotation;
            hand.rotation = Quaternion.Slerp(originalHandRotation, rotation, weight);
        }

        private void RestoreCameraKick()
        {
            if (cameraKickApplied && viewCamera && Quaternion.Angle(viewCamera.transform.localRotation, appliedCameraRotation) < .001f)
                viewCamera.transform.localRotation = unmodifiedCameraRotation;
            cameraKickApplied = false;
        }

        private void OnDisable()
        {
            RestoreCameraKick();
            RestoreChestLook();
            RestoreAnimatedGrip();
            if (playerAnimator != null && layer >= 0) playerAnimator.SetLayerWeight(layer, originalLayerWeight);
            if (playerAnimator != null && hasLocomotionPose) playerAnimator.SetFloat(LocomotionPose, 0f);
            if (gun != null) gun.gameObject.SetActive(false);
            blend = 0f;
            aimBlend = 0f;
            weaponMotion.Reset();
            hasPreviousView = false;
            reloadVisual?.Reset();
            if (shooting) shooting.CancelReload();
        }
        private void OnDestroy() { reloadVisual?.Dispose(); aimingRig?.Dispose(); }
    }
}
