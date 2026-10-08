using UnityEngine;
namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Mining Simulator/Base/Treasure Chest")]
    public sealed class TreasureChestData : ScriptableObject
    {
        public TreasureChest prefab;
        public Vector3 spawnPosition = new Vector3(0,0,8);
        [Min(.01f)] public float ticksPerSecond = .2f;
        [Min(0)] public float goldPerTick = 5f, goldPerLevel = 2f;
        [Min(1)] public float baseHealth = 500f;
        [Min(0)] public float healthPerLevel = 100f;
        [HideInInspector] public float experiencePerTick = 1f; // Legacy serialized value; payouts no longer grant XP.
        [Min(0)] public float killExperienceMultiplier = 1f;
        [Min(1)] public float experienceForFirstLevel = 10f;
        [Min(1)] public float experienceGrowth = 1.25f;
        [Min(1)] public int maximumLevel = 100;
        [Min(.01f)] public float openingSeconds = .6f, closingSeconds = .6f;
        [Min(0)] public float openHoldSeconds = .6f;
        public AnimationClip openAnimation, closeAnimation;
        
public Vector3 lidOpenEuler = new Vector3(-95,0,0);
        [Min(0)] public float playerInterceptRange = 3f;
        [Range(-1,1)] public float playerInterceptDot = .2f;
        public Sprite moneyIcon;
        [Min(1)] public float moneyPopupFontSize=38f;
        [Min(1)] public float moneyPopupIconSize=44f;
        [Min(.1f)] public float moneyPopupLifetime=1.5f;
        [Min(.01f)] public float moneyPopupScale = 1.6f;
        [Min(.01f)] public float moneyPopupPopSeconds = .25f;
        [Min(0f)] public float moneyPopupRise = 65f;
        // Legacy separate-panel settings retained only for serialized compatibility.
        [HideInInspector]
        public bool showScreenStats = true;
        [HideInInspector] public Vector2 screenStatsOffset = new Vector2(24, 0); // Legacy screen pixels, not world metres.
        [HideInInspector]
        [Min(.1f)] public float screenStatsScale = 1f;
        [HideInInspector]
        public Vector3 statsWorldOffset = new Vector3(-2.6f, 1.8f, 0f);
        [HideInInspector] public float statsWorldScale = .006f;
        [HideInInspector] public float statsNearDistance = 4f, statsHideDistance = 6f;
        [Header("Visual-only centred punch")]
        [Range(0f, .5f)] public float openPunchStrength = .12f, hitPunchStrength = .08f;
        [Min(.01f)] public float punchSeconds = .35f;
        

        [Header("Chest HP / XP artwork")]
        public Sprite barBackground, healthFill, experienceFill, barHighlight;
        [Header("Combined chest status artwork")]
        public Sprite compactPanelFrame, chestStatusIcon, levelBadge;
        [Header("Monster spawn ring (metres from chest)")]
        [Min(.5f)] public float monsterSpawnMinimumDistance = 12f;
        [Min(.5f)] public float monsterSpawnMaximumDistance = 16f;
        [Header("Broken chest / repair")]
        [Min(0f)] public float repairCost = 100f;
        public Sprite repairButtonSprite;
        public Vector2 repairButtonOffset = new Vector2(0, -135);
        [Header("Chest spatial SFX")]
        public AudioClip openSfx, closeSfx, payoutSfx, hitSfx, breakSfx, repairSfx;
        [Range(0f, 1f)] public float openVolume = 1f, closeVolume = 1f, payoutVolume = 1f,
            hitVolume = 1f, breakVolume = 1f, repairVolume = 1f;
        [Range(.1f, 3f)] public float sfxPitch = 1f;
        [Range(0f, 1f)] public float sfxVolume = .7f;
        [Min(.01f)] public float sfxNearDistance = 2f;
        [Min(.01f)] public float sfxFarDistance = 25f;
        [Min(0f)] public float hitSfxCooldown = .12f;
        public GameObject damagedModel;
        public MiningGame.Vfx.ChestBreakBurst breakEffect;
        public Vector3 damagedModelOffset;
        public Vector3 damagedModelEuler;
        [Min(.01f)] public float damagedModelScale = 1f;
        public Vector3 breakEffectOffset = new Vector3(0, .7f, 0);
public Sprite panelFrame;
        public TMPro.TMP_FontAsset font;
        public Vector3 panelOffset = new Vector3(0,2.5f,0);
        [Min(.001f)] public float panelWorldScale = .006f;
        [Tooltip("Player proximity in metres: show inside Near, hide outside Hide.")]
        [Min(0)] public float panelNearDistance = 4f, panelHideDistance = 6f;
        [Min(.01f)] public float panelTransitionSeconds = .25f;
        public float MaxHealth(int level) => baseHealth + healthPerLevel * Mathf.Max(0,level-1);
        public float Gold(int level) => goldPerTick + goldPerLevel * Mathf.Max(0,level-1);
        public float RequiredExperience(int level) => Mathf.Max(1,experienceForFirstLevel * Mathf.Pow(experienceGrowth,Mathf.Max(0,level-1)));
    }
}
