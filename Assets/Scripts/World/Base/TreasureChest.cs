using UnityEngine;
using MiningGame.Vfx;
namespace MiningSimulator.Ores
{
    [RequireComponent(typeof(MiningCharacterHealth))]
    [DisallowMultipleComponent]
    public sealed class TreasureChest : MonoBehaviour
    {
        public static TreasureChest Active { get; private set; }
        [SerializeField] private TreasureChestData data;
        [SerializeField] private GameObject animationRoot;
        
[SerializeField] private Transform lid;
        [SerializeField] private ChestCoinBurstVfx coinBurst;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private Collider body;
        [SerializeField] private string saveKey = "BaseTreasureChest.v1";
        [SerializeField] private bool persistProgress = true;
        private MiningCharacterHealth health;
        private Quaternion closed;
        private float payoutClock, visualClock;
        private int pendingTicks;
        private bool rewarded;
        private bool closingSoundPlayed;
        private AudioSource chestAudio;
        private MiningAudioManager audioManager;
        private float nextHitSoundTime;
        private Transform visualPivot;
        private float punchAge = 10f, punchStrength;
        private GameObject damagedVisual;
        private Renderer[] intactRenderers;
        private bool[] intactVisibility;
        
private bool initialized;
        
private bool opening;
        public int Level { get; private set; } = 1;
        public float Experience { get; private set; }
        public float ExperienceRequired => data.RequiredExperience(Level);
        public MiningCharacterHealth Health => health;
        public TreasureChestData Data => data;
        public PlayerWallet Wallet => wallet;
        public float RepairCost => data != null ? Mathf.Max(0f, data.repairCost) : 0f;
        public bool CanRepair => isActiveAndEnabled && IsBroken && data != null &&
            !float.IsNaN(RepairCost) && !float.IsInfinity(RepairCost) &&
            (RepairCost == 0f || wallet != null && wallet.CurrentMoney >= RepairCost);
        public bool IsAlive => isActiveAndEnabled && health != null && health.Health > 0f;
        public bool IsBroken => health != null && health.Health <= 0f;
        public event System.Action Changed;
        public event System.Action<float> Paid;
        private void Awake()
        {
            health = GetComponent<MiningCharacterHealth>();
            health.ConfigureRegeneration(0f,1f);
            if (body == null) body = GetComponent<Collider>();
            if (lid != null) closed = lid.localRotation;
            if (data == null) data = Resources.Load<TreasureChestData>("TreasureChestData");
        }
private void OnEnable()
        {
            // Only the outer chest owns gameplay; an imported model child is cosmetic.
            if (transform.parent != null && transform.parent.GetComponentInParent<TreasureChest>() != null)
            { enabled = false; return; }
            if (Active != null && Active != this)
            { Debug.LogError("Only one base treasure chest may be active.", this); enabled = false; return; }
            Active = this;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
        private void Start()
        {
            if (data == null) { Debug.LogError("Treasure chest GameData is missing.",this); enabled=false; return; }
            if (wallet == null) wallet = FindAnyObjectByType<PlayerWallet>();
            if (persistProgress)
            {
                Level = Mathf.Clamp(GameSave.GetInt(saveKey+".Level",1),1,data.maximumLevel);
                Experience = Mathf.Max(0,GameSave.GetFloat(saveKey+".XP",0));
            }
            initialized=true;
            ConfigureAudio();
            BuildVisualPivot();
            
health.ConfigureSpawnHealth(data.MaxHealth(Level));
            SampleLid(false,1);
            BuildBreakVisuals();
            if (persistProgress && GameSave.HasKey(saveKey + ".Health"))
            {
                health.RestoreSavedHealth(GameSave.GetFloat(saveKey + ".Health", health.MaxHealth));
                SetBrokenVisual(IsBroken);
            }
            GetComponent<TreasureChestHud>()?.Bind(this);
            Changed?.Invoke();
            if (persistProgress) TowerWorldSaveHost.Ensure(this);
        }
private void Update()
        {
            if (!IsAlive || data == null || RuneStation.PlayerUsesRuneTime) return;
            if (wallet != null)
            {
                payoutClock += Time.deltaTime;
                float interval = 1f / Mathf.Max(.01f,data.ticksPerSecond);
                int due = Mathf.Min(32,Mathf.FloorToInt(payoutClock/interval));
                if (due > 0) { payoutClock -= due*interval; pendingTicks += due; }
            }
            if (!opening && pendingTicks > 0)
            {
                opening=true; rewarded=false; visualClock=0; closingSoundPlayed=false;
                PlaySound(data.openSfx, data.openVolume);
                Punch(data.openPunchStrength);
            }
            if (!opening) return;
            visualClock += Time.deltaTime;
            float openTime=Mathf.Max(.01f,data.openingSeconds);
            float holdTime=Mathf.Max(0,data.openHoldSeconds);
            float closeTime=Mathf.Max(.01f,data.closingSeconds);
            if (visualClock < openTime) SampleLid(true,visualClock/openTime);
            else
            {
                if (!rewarded)
                {
                    SampleLid(true,1);
                    int ticks=pendingTicks;pendingTicks=0;rewarded=true;
                    Pay(ticks);
                    PlaySound(data.payoutSfx, data.payoutVolume);
                    coinBurst?.PlayBurst();
                }
                if (visualClock < openTime+holdTime) SampleLid(true,1);
                else
                {
                    if (!closingSoundPlayed) { closingSoundPlayed=true; PlaySound(data.closeSfx, data.closeVolume); }
                    float t=(visualClock-openTime-holdTime)/closeTime;
                    SampleLid(false,t);
                    if(t>=1) opening=false;
                }
            }
        }
        private void Pay(int ticks)
        {
            float amount=0;
            for(int i=0;i<ticks;i++)
            {
                amount += data.Gold(Level);
            }
            wallet.AddMoney(amount);

            Paid?.Invoke(amount);
            Changed?.Invoke();
            SaveProgress();
        }

        public void GrantKillExperience(float amount)
        {
            if (!IsAlive || data == null || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            float gained = amount * data.killExperienceMultiplier;
            if (gained <= 0f || float.IsNaN(gained) || float.IsInfinity(gained)) return;
            Experience += gained;
            while (Level < data.maximumLevel && Experience >= ExperienceRequired)
            {
                Experience -= ExperienceRequired;
                Level++;
                health.ConfigureMaximumHealth(data.MaxHealth(Level));
            }
            if (Level >= data.maximumLevel) Experience = 0f;
            Changed?.Invoke();
            SaveProgress();
        }

        private void BuildVisualPivot()
        {
            if (animationRoot == null || animationRoot == gameObject || visualPivot != null) return;
            // The outer body is authoritative. Imported model colliders must never animate.
            foreach (var collider in animationRoot.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var renderers = animationRoot.GetComponentsInChildren<Renderer>(true);
            var bounds = new Bounds(animationRoot.transform.position, Vector3.zero);
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            }
            visualPivot = new GameObject("Chest visual punch pivot").transform;
            visualPivot.SetParent(transform, false);
            visualPivot.position = bounds.center;
            animationRoot.transform.SetParent(visualPivot, true);
        }

        private void Punch(float strength) { punchAge = 0f; punchStrength = strength; }

        private void LateUpdate()
        {
            if (visualPivot == null || data == null) return;
            if (!RuneStation.PlayerUsesRuneTime) punchAge += Time.deltaTime;
            float t = Mathf.Clamp01(punchAge / Mathf.Max(.01f, data.punchSeconds));
            float pulse = IsAlive ? Mathf.Sin(t * Mathf.PI) * (1f - t) * punchStrength : 0f;
            visualPivot.localScale = Vector3.one * (1f + pulse);
        }

private void SampleLid(bool open,float progress)
        {
            var clip=open?data.openAnimation:data.closeAnimation;
            if(clip!=null && animationRoot!=null && lid!=null)
            {
                // Imported clips contain unit-converted translation keys. Only the
                // hinge rotation is animated; its authored anchor must stay fixed.
                Vector3 anchor=lid.localPosition, scale=lid.localScale;
                clip.SampleAnimation(animationRoot,Mathf.Clamp01(progress)*clip.length);
                lid.localPosition=anchor;
                lid.localScale=scale;
            }
            else if(lid!=null)
            {
                Quaternion raised=closed*Quaternion.Euler(data.lidOpenEuler);
                lid.localRotation=Quaternion.Slerp(open?closed:raised,open?raised:closed,Mathf.SmoothStep(0,1,Mathf.Clamp01(progress)));
            }
        }

        public Vector3 ApproachGoal(Vector3 origin) => body != null ? body.ClosestPoint(origin) : transform.position;
        private void ConfigureAudio()
        {
            chestAudio = gameObject.AddComponent<AudioSource>();
            chestAudio.playOnAwake = false;
            chestAudio.spatialBlend = 1f;
            chestAudio.rolloffMode = AudioRolloffMode.Linear;
            chestAudio.minDistance = Mathf.Max(.01f, data.sfxNearDistance);
            chestAudio.maxDistance = Mathf.Max(chestAudio.minDistance + .01f, data.sfxFarDistance);
            chestAudio.dopplerLevel = 0f;
            chestAudio.volume = Mathf.Clamp01(data.sfxVolume);
            audioManager = FindAnyObjectByType<MiningAudioManager>();
            if (audioManager != null) audioManager.RegisterSfxSource(chestAudio, chestAudio.volume);
        }

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip != null && chestAudio != null && isActiveAndEnabled)
            {
                if (audioManager != null) audioManager.RegisterSfxSource(chestAudio, data.sfxVolume);
                else chestAudio.volume = Mathf.Clamp01(data.sfxVolume);
                chestAudio.pitch = Mathf.Clamp(data.sfxPitch, .1f, 3f);
                chestAudio.minDistance = Mathf.Max(.01f, data.sfxNearDistance);
                chestAudio.maxDistance = Mathf.Max(chestAudio.minDistance + .01f, data.sfxFarDistance);
                chestAudio.PlayOneShot(clip, Mathf.Clamp01(volume));
            }
        }

        private void OnDamaged()
        {
            SaveProgress();
            if (data != null && IsAlive) Punch(data.hitPunchStrength);
            if (data != null && IsAlive && Time.unscaledTime >= nextHitSoundTime)
            {
                nextHitSoundTime = Time.unscaledTime + Mathf.Max(0f, data.hitSfxCooldown);
                PlaySound(data.hitSfx, data.hitVolume);
            }
            Changed?.Invoke();
        }
        private void OnDied()
        {
            SaveProgress();
            if (chestAudio != null) chestAudio.Stop();
            if (data != null) PlaySound(data.breakSfx, data.breakVolume);
            opening = rewarded = false;
            pendingTicks = 0;
            payoutClock = visualClock = 0f;
            if (data != null) SampleLid(false, 1);
            if (coinBurst != null)
                foreach (var particles in coinBurst.GetComponentsInChildren<ParticleSystem>(true))
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            SetBrokenVisual(true);
            if (data != null && data.breakEffect != null)
            {
                var burst = Instantiate(data.breakEffect, transform.TransformPoint(data.breakEffectOffset), transform.rotation);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(burst.gameObject, gameObject.scene);
            }
            Changed?.Invoke();
        }

        private void BuildBreakVisuals()
        {
            if (damagedVisual != null || intactRenderers != null) return;
            var root = animationRoot != null ? animationRoot : gameObject;
            intactRenderers = root.GetComponentsInChildren<Renderer>(true);
            intactVisibility = new bool[intactRenderers.Length];
            for (int i = 0; i < intactRenderers.Length; i++) intactVisibility[i] = intactRenderers[i].enabled;
            if (data.damagedModel == null) return;
            damagedVisual = Instantiate(data.damagedModel, transform);
            damagedVisual.name = "Broken chest visual";
            damagedVisual.transform.localPosition = data.damagedModelOffset;
            damagedVisual.transform.localRotation = Quaternion.Euler(data.damagedModelEuler);
            damagedVisual.transform.localScale = Vector3.one * data.damagedModelScale;
            // Presentation only: the anchored gameplay collider remains unchanged.
            foreach (var collider in damagedVisual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            damagedVisual.SetActive(false);
        }

        private void SetBrokenVisual(bool broken)
        {
            if (intactRenderers != null)
                for (int i = 0; i < intactRenderers.Length; i++)
                    if (intactRenderers[i] != null) intactRenderers[i].enabled = !broken && intactVisibility[i];
            if (damagedVisual != null) damagedVisual.SetActive(broken);
        }

        public bool Repair()
        {
            if (!CanRepair) return false;
            if (RepairCost > 0f && !wallet.TrySpend(RepairCost)) return false;
            // Repair preserves progression, clears lingering burn, and starts a fresh payout interval.
            health.ConfigureSpawnHealth(data.MaxHealth(Level));
            health.Respawn();
            payoutClock = visualClock = 0f;
            pendingTicks = 0;
            opening = rewarded = false;
            SampleLid(false, 1f);
            SetBrokenVisual(false);
            PlaySound(data.repairSfx, data.repairVolume);
            SaveProgress();
            Changed?.Invoke();
            return true;
        }
        private void SaveProgress()
        {
            if(!persistProgress || !initialized)return;
            GameSave.SetInt(saveKey+".Level",Level);
            GameSave.SetFloat(saveKey+".XP",Experience);
            if (health != null) GameSave.SetFloat(saveKey + ".Health", health.Health);
        }
        private void OnDisable()
        {
            if (visualPivot != null) visualPivot.localScale = Vector3.one;
            if (chestAudio != null) chestAudio.Stop();
            if(Active==this)Active=null;
            health.Damaged-=OnDamaged; health.Died-=OnDied;
            SaveProgress();
            if(lid!=null)lid.localRotation=closed;
        }
    

public static void ResetSavedProgress()
        {
            GameSave.DeleteKey("BaseTreasureChest.v1.Level");
            GameSave.DeleteKey("BaseTreasureChest.v1.XP");
            GameSave.DeleteKey("BaseTreasureChest.v1.Health");
            foreach (var chest in FindObjectsByType<TreasureChest>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (chest.transform.parent != null &&
                    chest.transform.parent.GetComponentInParent<TreasureChest>() != null) continue;
                chest.ResetProgress();
            }
            GameSave.Save();
        }


public void ResetProgress()
        {
            Level = 1;
            Experience = 0f;
            payoutClock = visualClock = 0f;
            pendingTicks = 0;
            opening = rewarded = false;
            if (health == null) health = GetComponent<MiningCharacterHealth>();
            if (data != null)
            {
                health.ConfigureSpawnHealth(data.MaxHealth(1));
                health.Respawn();
                SetBrokenVisual(false);
                SampleLid(false, 1f);
            }
            if (persistProgress)
            {
                GameSave.DeleteKey(saveKey + ".Level");
                GameSave.DeleteKey(saveKey + ".XP");
                GameSave.DeleteKey(saveKey + ".Health");
            }
            Changed?.Invoke();
        }
}
}
