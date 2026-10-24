using UnityEngine;

namespace MiningGame.Vfx
{
    [DisallowMultipleComponent]
    public sealed class ChestCoinBurstVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem coins;
        [Header("Open trigger (optional)")]
        [SerializeField] private bool watchLid = true;
        [SerializeField] private Transform lid;
        [SerializeField] private Transform chestFrame;
        [SerializeField] private Quaternion closedRelativeRotation = Quaternion.identity;
        [Range(1f, 170f), SerializeField] private float openingAngle = 40f;
        [Range(0f, 30f), SerializeField] private float rearmAngle = 5f;
        [Header("Editable burst")]
        [Range(1, 128), SerializeField] private int coinCount = 48;
        [SerializeField] private Vector3 emissionHalfExtents = new Vector3(.22f, .015f, .12f);
        [SerializeField] private Vector2 upwardSpeed = new Vector2(4f, 6f);
        [SerializeField] private Vector2 outwardSpeed = new Vector2(.8f, 2.2f);
        [SerializeField] private Vector2 lifetime = new Vector2(1.5f, 2f);
        [SerializeField] private Vector2 diameter = new Vector2(.10f, .16f);
        [Min(.001f), SerializeField] private float coinThickness = .012f;
        [Min(0f), SerializeField] private float spinDegreesPerSecond = 720f;
        [Min(0f), SerializeField] private float repeatCooldown = .25f;

        private bool armed;
        private float lastBurst = float.NegativeInfinity;

        private void OnEnable()
        {
            lastBurst = float.NegativeInfinity;
            // An already-open decorative chest must not burst merely on activation.
            armed = lid == null || CurrentAngle() <= rearmAngle;
        }

        private float CurrentAngle()
        {
            Quaternion reference = chestFrame != null ? chestFrame.rotation : Quaternion.identity;
            // World-relative orientation also follows a visual hinge that MiningChest
            // reparents beneath its own pivot during Awake.
            return Quaternion.Angle(closedRelativeRotation, Quaternion.Inverse(reference) * lid.rotation);
        }

        private void LateUpdate()
        {
            if (!watchLid || lid == null) return;
            float angle = CurrentAngle();
            if (angle <= rearmAngle) armed = true;
            if (!armed || angle < openingAngle) return;
            armed = false;
            PlayBurst();
        }

        // May also be called by a UnityEvent, Animation Event (same GameObject),
        // or the existing chest-opening code. It never changes wallet/inventory.
        [ContextMenu("Preview Coin Burst")]
        public void PlayBurst()
        {
            if (coins == null || !isActiveAndEnabled) return;
            if (Application.isPlaying && Time.time - lastBurst < repeatCooldown) return;
            lastBurst = Time.time;
            armed = false;
            coins.Play(false);
            for (int i = 0; i < Mathf.Clamp(coinCount, 1, 128); i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float horizontal = Sample(outwardSpeed);
                float size = Mathf.Max(.001f, Sample(diameter));
                Vector3 local = new Vector3(Random.Range(-emissionHalfExtents.x, emissionHalfExtents.x),
                    Random.Range(-emissionHalfExtents.y, emissionHalfExtents.y),
                    Random.Range(-emissionHalfExtents.z, emissionHalfExtents.z));
                var particle = new ParticleSystem.EmitParams
                {
                    position = coins.transform.TransformPoint(local),
                    velocity = new Vector3(Mathf.Cos(angle) * horizontal, Sample(upwardSpeed), Mathf.Sin(angle) * horizontal),
                    rotation3D = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)),
                    angularVelocity3D = new Vector3(Random.Range(-spinDegreesPerSecond, spinDegreesPerSecond),
                        Random.Range(-spinDegreesPerSecond, spinDegreesPerSecond), Random.Range(-spinDegreesPerSecond, spinDegreesPerSecond)),
                    startSize3D = new Vector3(size, coinThickness * .5f, size),
                    startLifetime = Mathf.Max(.01f, Sample(lifetime)),
                    startColor = Color.white
                };
                coins.Emit(particle, 1);
            }
        }

        public void BindClosedLid(Transform visualLid, Transform referenceFrame)
        {
            lid = visualLid;
            chestFrame = referenceFrame;
            if (lid != null)
            {
                Quaternion reference = chestFrame != null ? chestFrame.rotation : Quaternion.identity;
                closedRelativeRotation = Quaternion.Inverse(reference) * lid.rotation;
            }
            armed = true;
        }

        private static float Sample(Vector2 range) => Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));

        private void OnDisable()
        {
            armed = false;
            if (coins != null) coins.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnValidate()
        {
            rearmAngle = Mathf.Clamp(rearmAngle, 0f, Mathf.Max(0f, openingAngle - 1f));
            emissionHalfExtents = new Vector3(Mathf.Abs(emissionHalfExtents.x), Mathf.Abs(emissionHalfExtents.y), Mathf.Abs(emissionHalfExtents.z));
        }
    }
}
