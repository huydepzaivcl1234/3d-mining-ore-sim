using UnityEngine;

namespace GameData.Player.Script
{
    /// <summary>Slide cycle, two-handed kickback and a reusable muzzle effect. Never edits the pose asset.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PistolShooting)), DefaultExecutionOrder(120)]
    public sealed class PistolShotFeedback : MonoBehaviour
    {
        [SerializeField] private Transform slide;
        [SerializeField] private Transform muzzle;
        [Tooltip("Backwards in the slide parent's local coordinates; M1911 barrel points along +Z.")]
        [SerializeField] private Vector3 slideBackward = Vector3.back;
        private PistolShooting shooting;
        private PistolPresentation presentation;
        private Vector3 closedSlidePosition;
        private GameObject flash;
        private ParticleSystem[] particles;
        private float displacement, displacementVelocity, pitch, pitchVelocity;
        private float shotAge = float.PositiveInfinity;
        private float cameraPitch, cameraVelocity;
        private float yaw, yawVelocity, roll, rollVelocity;
        public float CameraRecoil => cameraPitch;
        public float Kickback => displacement;
        public float SlideOffset { get; private set; }
        public int FlashCount { get; private set; }
        public GameObject FlashInstance => flash;
        public Transform Muzzle => muzzle;
        private PistolWeaponData Data => shooting != null ? shooting.Data : null;

        private void Awake()
        {
            shooting = GetComponent<PistolShooting>();
            presentation = GetComponent<PistolPresentation>();
            BuildFlash();
        }
        public void ConfigureWeapon(Transform barrel, Transform mechanism)
        {
            ResetFeedback();
            if (flash) { flash.SetActive(false); Destroy(flash); }
            flash = null; particles = null;
            muzzle = barrel; slide = mechanism;
            BuildFlash();
        }
        private void BuildFlash()
        {
            if (slide) closedSlidePosition = slide.localPosition;
            if (Data && Data.muzzleFlashPrefab && muzzle)
            {
                flash = Instantiate(Data.muzzleFlashPrefab, muzzle, false);
                flash.name = "Pistol Muzzle Flash (Reusable)";
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
        }
        private void OnEnable()
        {
            if (!shooting) shooting = GetComponent<PistolShooting>();
            shooting.Fired += OnShot;
        }
        private void OnShot()
        {
            if (!Data) return;
            float weight = Mathf.Lerp(1f, Data.aimRecoilMultiplier, presentation.AimWeight);
            float frequency = 4f / Mathf.Max(.02f, Data.handReturnSeconds);
            // An impulse produces a quick rearward stroke, then a damped return without bouncing.
            displacementVelocity += Mathf.Max(0f, Data.handKickback) * frequency * 2.71828f * weight;
            pitchVelocity += Mathf.Max(0f, Data.recoilDegrees) * frequency * 2.71828f * weight;
            float side = (shooting.ShotsFired & 1) == 0 ? 1f : -1f;
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
        }
        private void Update()
        {
            if (!Data || !presentation) return;
            if (!presentation.Equipped) { ResetFeedback(); return; }
            // The reload clip owns the mechanism in LateUpdate while the action is active.
            if (shooting.Reloading) { ResetFeedback(); return; }
            float dt = Time.deltaTime;
            float frequency = 4f / Mathf.Max(.02f, Data.handReturnSeconds);
            AdvanceSpring(ref displacement, ref displacementVelocity, frequency, dt);
            AdvanceSpring(ref pitch, ref pitchVelocity, frequency, dt);
            AdvanceSpring(ref yaw, ref yawVelocity, frequency, dt);
            AdvanceSpring(ref roll, ref rollVelocity, frequency, dt);
            AdvanceSpring(ref cameraPitch, ref cameraVelocity, 4f / Mathf.Max(.02f, Data.cameraReturnSeconds), dt);
            presentation.CameraShotKick = new Vector3(-cameraPitch, yaw * .3f, 0f);
            presentation.ShotDisplacement = new Vector3(0f, -displacement * .08f, -displacement);
            presentation.ShotKick = new Vector3(-pitch, yaw, roll);
            shotAge += dt;
            float open = Mathf.Max(.005f, Data.slideOpenSeconds);
            float close = Mathf.Max(.01f, Data.slideCloseSeconds);
            float cycle = shotAge < open ? shotAge / open : 1f - Mathf.Clamp01((shotAge - open) / close);
            SlideOffset = Mathf.Max(0f, Data.slideTravel) * Mathf.SmoothStep(0f, 1f, cycle);
            if (shooting.Ammo == 0) SlideOffset = Mathf.Max(0f, Data.slideTravel);
            if (slide) slide.localPosition = closedSlidePosition + slideBackward.normalized * SlideOffset;
        }
        public static void AdvanceSpring(ref float position, ref float velocity, float frequency, float dt)
        {
            float term = velocity + frequency * position;
            float decay = Mathf.Exp(-frequency * dt);
            position = (position + term * dt) * decay;
            velocity = (velocity - frequency * term * dt) * decay;
        }
        private void ResetFeedback()
        {
            displacement = displacementVelocity = pitch = pitchVelocity = SlideOffset = 0f;
            cameraPitch = cameraVelocity = 0f;
            yaw = yawVelocity = roll = rollVelocity = 0f;
            shotAge = float.PositiveInfinity;
            if (presentation) { presentation.ShotKick = Vector3.zero; presentation.ShotDisplacement = Vector3.zero; presentation.CameraShotKick = Vector3.zero; }
            if (slide) slide.localPosition = closedSlidePosition;
            if (particles != null) foreach (var effect in particles) effect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnDisable()
        {
            if (shooting) shooting.Fired -= OnShot;
            ResetFeedback();
        }
        private void OnDestroy() { if (flash) Destroy(flash); }
    }
}
