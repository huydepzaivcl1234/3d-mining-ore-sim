using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName = "Chest Defense/Weapons/Pistol", fileName = "PistolWeaponData")]
    public sealed class PistolWeaponData : ScriptableObject
    {
        public string displayName = "M1911";
        [Min(0f)] public float damage = 12f;
        [Min(.02f)] public float shotCooldown = .25f;
        [Tooltip("Hard maximum bullet travel in meters. No hit or impact beyond this distance.")]
        [Min(1f)] public float range = 500f;
        [Tooltip("Full damage until this distance; linearly decreases to zero at Range.")]
        [Min(0f)] public float damageFalloffStart = 300f;
        public float DamageAtDistance(float distance)
            => Mathf.Max(0f, damage) * DamageFalloffAtDistance(distance);
        public float DamageFalloffAtDistance(float distance)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0f || distance >= range) return 0f;
            float start = Mathf.Clamp(damageFalloffStart, 0f, Mathf.Max(0f, range));
            float weight = distance <= start ? 1f : 1f - Mathf.InverseLerp(start, range, distance);
            return weight;
        }
        [Min(1)] public int magazineSize = 12;
        [Min(0)] public int reserveAmmo = 120;
        [Min(.1f)] public float reloadSeconds = 1.5f;
        public bool automatic;
        public LayerMask hitLayers = ~0;
        [Header("Procedural presentation - optional")]
        public FpsWeaponMotionProfile motionProfile;
        public PistolReloadProfile reloadProfile;
        [Min(0f)] public float recoilDegrees = 2f;
        [Min(0f)] public float recoilYawDegrees = .20f;
        [Min(0f)] public float recoilRollDegrees = .35f;
        [HideInInspector, Min(.1f)] public float recoilRecovery = 12f; // Legacy; spring uses handReturnSeconds.
        [Header("Shot feedback - meters and seconds")]
        [Min(0f)] public float handKickback = .025f;
        [Min(.02f)] public float handReturnSeconds = .14f;
        [Header("Camera recoil - hip and ADS")]
        [Min(0f)] public float cameraRecoilDegrees = .9f;
        [Min(.02f)] public float cameraReturnSeconds = .20f;
        [Range(0f,1f)] public float aimRecoilMultiplier = .65f;
        [Min(0f)] public float slideTravel = .035f;
        [Min(.005f)] public float slideOpenSeconds = .018f;
        [Min(.01f)] public float slideCloseSeconds = .075f;
        public GameObject muzzleFlashPrefab;
        [Header("Visible bullet / impact (visual hitscan tracer)")]
        public Material shotVfxMaterial;
        [Min(1f)] public float tracerSpeed = 55f;
        [Min(.05f)] public float tracerLength = .8f;
        [Min(.001f)] public float tracerWidth = .018f;
        public Color tracerColor = new Color(1f, .78f, .25f, 1f);
        public Color impactColor = new Color(1f, .65f, .18f, 1f);
        [Min(.01f)] public float muzzleFlashScale = .35f;
        public AudioClip shotSound;
        [Range(0,1)] public float soundVolume = .5f;
    }
}
