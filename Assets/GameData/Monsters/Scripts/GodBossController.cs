using System.Collections.Generic;
using GameData.Player.Script;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [RequireComponent(typeof(CharacterController), typeof(MiningCharacterHealth), typeof(GodBossRifleReward))]
    public sealed class GodBossController : MonoBehaviour
    {
        public enum Action { Chase, Punch, Swipe, Windup, Jump, Roar, Dead }
        [SerializeField] private GodBossData data;
        [SerializeField] private Animator animator;
        private CharacterController motor;
        private Collider playerBody;
        private bool landingIgnoresPlayer, originalPlayerIgnore;
        private MiningCharacterHealth health, player;
        private PlayerController1 playerMotor;
        private MonsterSpawnZone zone;
        private float elapsed, duration, nextJump, nextRoar, nextRoarCheck, nextPath, gravity;
        private bool impacted, running, pathPending;
        private Vector3 jumpStart, jumpTarget;
        private readonly List<Vector3> path = new();
        private int pathIndex;
        private LineRenderer wave;
        private Material waveMaterial;
        private float waveDistance = -1;
        private Vector3 waveCenter;
        private bool waveHit;
        private readonly Vector3[] ring = new Vector3[65];
        public Action CurrentAction { get; private set; }
        public GodBossData Data => data;
        public MiningCharacterHealth Health => health;
        public int EncounterDay { get; private set; }
        public Vector3 JumpTarget => jumpTarget;
        public event System.Action<GodBossController> Defeated;

        private void Awake()
        {
            motor = GetComponent<CharacterController>(); health = GetComponent<MiningCharacterHealth>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            if (animator) animator.applyRootMotion = false;
            health.Died += Die;
        }
        private void IgnorePlayerForLanding()
        {
            if (!motor || !playerBody || landingIgnoresPlayer) return;
            originalPlayerIgnore = Physics.GetIgnoreCollision(motor, playerBody);
            Physics.IgnoreCollision(motor, playerBody, true);
            landingIgnoresPlayer = true;
        }
        private void OnEnable()
        {
            if (CurrentAction == Action.Jump) IgnorePlayerForLanding();
        }
        private void RestorePlayerCollision()
        {
            if (!landingIgnoresPlayer) return;
            if (motor && playerBody) Physics.IgnoreCollision(motor, playerBody, originalPlayerIgnore);
            landingIgnoresPlayer = false;
        }
        public void Initialize(MonsterSpawnZone owner, MiningCharacterHealth target, int day)
        {
            RestorePlayerCollision();
            zone = owner; player = target; EncounterDay = day;
            playerBody = player ? player.GetComponent<Collider>() : null;
            playerMotor = player ? player.GetComponent<PlayerController1>() : null;
            health.ConfigureSpawnHealth(data.health);
            health.ConfigureRegeneration(0, 5);
            health.ConfigureDefenses(data.armor, data.magicResistance);
            float oldHeight = motor.height;
            if (animator && oldHeight > 0)
            {
                float scale = data.height / oldHeight;
                animator.transform.localScale *= scale;
                animator.transform.localPosition *= scale;
            }
            motor.height = Mathf.Max(motor.radius * 2, data.height);
            motor.center = Vector3.up * motor.height * .5f;
            nextJump = Time.time + data.jumpCooldown;
            nextRoarCheck = Time.time + data.roarCheckSeconds;
            SetAnimation("Idle", 1);
        }
        private void Update()
        {
            if (!data || Time.deltaTime <= 0) return;
            // Re-enabling collisions while overlapping would push the player or
            // put the boss back onto their head. Wait until both bodies separate.
            if (landingIgnoresPlayer && CurrentAction != Action.Jump &&
                (!playerBody || !motor.bounds.Intersects(playerBody.bounds))) RestorePlayerCollision();
            TickWave();
            if (CurrentAction == Action.Dead) return;
            if (!player || player.Health <= 0) { SetAnimation("Idle", 1); return; }
            elapsed += Time.deltaTime;
            if (CurrentAction == Action.Chase) Chase();
            else if (CurrentAction == Action.Windup)
            {
                Face(jumpTarget);
                if (elapsed >= duration)
                {
                    jumpStart = transform.position;
                    IgnorePlayerForLanding();
                    float flight = data.jumpTravelSeconds / Mathf.Max(.01f, data.jumpSpeed);
                    Begin(Action.Jump, "Jump", (data.jump ? data.jump.length : 1) / Mathf.Max(.01f, flight), flight);
                }
            }
            else if (CurrentAction == Action.Jump)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 point = Vector3.Lerp(jumpStart, jumpTarget, t) + Vector3.up * (4 * data.jumpHeight * t * (1-t));
                motor.Move(point - transform.position);
                if (t >= 1)
                {
                    gravity = motor.isGrounded ? -2 : Mathf.Max(-30, gravity + Physics.gravity.y * Time.deltaTime);
                    motor.Move(Vector3.up * gravity * Time.deltaTime);
                    if (!motor.isGrounded) return;
                    StartWave(); nextJump = Time.time + data.jumpCooldown;
                    Begin(Action.Chase, "Idle", 1, 0);
                }
            }
            else
            {
                if (CurrentAction == Action.Punch || CurrentAction == Action.Swipe)
                {
                    var strike = CurrentAction == Action.Punch ? data.punch : data.swipe;
                    float hitAt = duration * strike.hitTime;
                    Face(player.transform.position);
                    if (elapsed <= hitAt && FlatDistance(player.transform.position) > motor.radius + .75f)
                        motor.Move(transform.forward * (data.lungeDistance / Mathf.Max(.01f, hitAt)) * Time.deltaTime);
                    if (!impacted && elapsed >= hitAt) { impacted = true; Melee(strike); }
                }
                else if (CurrentAction == Action.Roar && !impacted && elapsed >= duration * .5f)
                {
                    impacted = true;
                    float missing = 1 - health.Health / health.MaxHealth;
                    health.Heal(health.MaxHealth * data.healMaxHealthFraction);
                    if (zone) zone.SummonGodMinions(this, missing);
                }
                if (elapsed >= duration + data.recovery) Begin(Action.Chase, "Idle", 1, 0);
            }
            if (CurrentAction != Action.Jump)
            {
                gravity = motor.isGrounded ? -2 : Mathf.Max(-30, gravity + Physics.gravity.y * Time.deltaTime);
                motor.Move(Vector3.up * gravity * Time.deltaTime);
            }
        }
        private void Chase()
        {
            if(!player.IsTargetable){SetAnimation("Idle",1);return;}
            float distance = FlatDistance(player.transform.position);
            if (health.Health < health.MaxHealth && Time.time >= nextRoar && Time.time >= nextRoarCheck)
            {
                nextRoarCheck = Time.time + data.roarCheckSeconds;
                if (Random.value < data.roarChance)
                {
                    nextRoar = Time.time + data.roarCooldown;
                    Begin(Action.Roar, "Roar", data.roarSpeed, ClipSeconds(data.roar, data.roarSpeed)); return;
                }
            }
            if (distance > data.jumpRange && Time.time >= nextJump)
            {
                // Commit now, including destination: dodging during the tell remains useful.
                jumpTarget = player.transform.position;
                var grid = WorldNavigationGrid.Instance;
                if (grid && grid.TryProject(jumpTarget, 5, out var ground)) jumpTarget = ground;
                else if (Physics.Raycast(jumpTarget + Vector3.up * data.height, Vector3.down,
                    out var landing, data.height * 2, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore))
                    jumpTarget = landing.point;
                Begin(Action.Windup, "Idle", 1, data.jumpWindup); return;
            }
            if (distance <= data.meleeRange)
            {
                bool punch = Random.value < .5f;
                var strike = punch ? data.punch : data.swipe;
                Begin(punch ? Action.Punch : Action.Swipe, punch ? "Punch" : "Swipe", strike.speed,
                    ClipSeconds(punch ? data.punchClip : data.swipeClip, strike.speed)); return;
            }
            if (distance >= data.startRunningDistance) running = true;
            else if (distance <= data.stopRunningDistance) running = false;
            SetAnimation(running ? "Run" : "Walk", 1);
            var navigation = WorldNavigationGrid.Instance;
            if (navigation && navigation.HasBaked && !pathPending && Time.time >= nextPath)
            {
                pathPending = true; nextPath = Time.time + .75f;
                navigation.RequestPath(this, transform.position, player.transform.position, OnPath, motor.radius, motor.height);
            }
            Vector3 destination = player.transform.position;
            if (path.Count > 0)
            {
                while (pathIndex < path.Count - 1 && FlatDistance(path[pathIndex]) < 1) pathIndex++;
                destination = path[pathIndex];
            }
            Face(destination);
            Vector3 direction = destination - transform.position; direction.y = 0;
            motor.Move(direction.normalized * (running ? data.runSpeed : data.walkSpeed) * Time.deltaTime);
        }
        private void OnPath(bool success, List<Vector3> points)
        {
            pathPending = false; path.Clear(); pathIndex = 0;
            if (success) path.AddRange(points);
        }
        private float FlatDistance(Vector3 point) { point.y = transform.position.y; return Vector3.Distance(transform.position, point); }
        private void Face(Vector3 point)
        {
            Vector3 direction = point - transform.position; direction.y = 0;
            if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), data.turnSpeed * Time.deltaTime);
        }
        private string playing;
        private void SetAnimation(string state, float speed)
        {
            if (!animator) return;
            animator.speed = Mathf.Max(.01f, speed);
            if (playing == state) return;
            playing = state; animator.CrossFadeInFixedTime(state, data.crossfade);
        }
        private static float ClipSeconds(AnimationClip clip, float speed) => (clip ? clip.length : 1) / Mathf.Max(.01f, speed);
        private void Begin(Action action, string state, float speed, float seconds)
        {
            CurrentAction = action; elapsed = 0; duration = Mathf.Max(.001f, seconds); impacted = false;
            if (action != Action.Chase) playing = null;
            SetAnimation(state, speed);
        }
        private void Melee(GodBossData.Strike strike)
        {
            Vector3 delta = player.transform.position - transform.position;
            if (Mathf.Abs(delta.y) > data.height || FlatDistance(player.transform.position) > data.meleeRange) return;
            delta.y = 0;
            if (Vector3.Angle(transform.forward, delta) > data.attackHalfAngle) return;
            if (player.DealDamage(strike.damage, CombatDamageType.Physical, gameObject) <= 0) return;
            if (playerMotor && strike.knockback > 0) playerMotor.ApplyKnockback(delta.normalized * strike.knockback, .3f);
            if (CurrentAction == Action.Swipe)
            {
                var bleed = player.GetComponent<GodBossBleed>();
                if (!bleed) bleed = player.gameObject.AddComponent<GodBossBleed>();
                bleed.Apply(data.bleedMaxHealthPercentPerSecond, data.bleedDuration, gameObject);
            }
        }
        private void StartWave()
        {
            waveCenter = transform.position + Vector3.up * .15f; waveDistance = 0; waveHit = false;
            if (!wave)
            {
                var go = new GameObject("God Boss shockwave"); wave = go.AddComponent<LineRenderer>();
                waveMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                waveMaterial.SetColor("_BaseColor", new Color(1, .5f, .08f));
                wave.sharedMaterial = waveMaterial; wave.positionCount = ring.Length;
                wave.startWidth = wave.endWidth = .18f; wave.useWorldSpace = true;
                wave.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            wave.enabled = true;
        }
        private void TickWave()
        {
            if (waveDistance < 0 || !wave) return;
            float previous = waveDistance; waveDistance += data.waveSpeed * Time.deltaTime;
            for (int i = 0; i < ring.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / (ring.Length - 1);
                ring[i] = waveCenter + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * waveDistance;
            }
            wave.SetPositions(ring);
            if (!waveHit && player && player.Health > 0)
            {
                Vector3 delta = player.transform.position - waveCenter;
                float y = delta.y; delta.y = 0; float distance = delta.magnitude;
                if (Mathf.Abs(y) < 1.5f && distance >= previous - data.waveThickness && distance <= waveDistance + data.waveThickness)
                {
                    waveHit = true;
                    if (player.DealDamage(data.jumpDamage, CombatDamageType.Physical, gameObject) > 0 && playerMotor)
                        playerMotor.ApplyKnockback(delta.normalized * data.jumpKnockback + Vector3.up * 3, .7f);
                }
            }
            if (waveDistance >= data.waveRadius) { waveDistance = -1; wave.enabled = false; }
        }
        private void Die()
        {
            if (CurrentAction == Action.Dead) return;
            Begin(Action.Dead, "Dying", data.deathSpeed, ClipSeconds(data.dying, data.deathSpeed));
            if (motor) motor.enabled = false;
            waveDistance = -1; if (wave) wave.enabled = false;
            Defeated?.Invoke(this);
            Destroy(gameObject, duration + 2);
        }
        private void OnDisable()
        {
            RestorePlayerCollision();
            if (WorldNavigationGrid.Instance) WorldNavigationGrid.Instance.Cancel(this);
            pathPending = false;
            if (wave) wave.enabled = false;
        }
        private void OnDestroy()
        {
            if (health) health.Died -= Die;
            if (wave) Destroy(wave.gameObject);
            if (waveMaterial) Destroy(waveMaterial);
        }
    }
}
