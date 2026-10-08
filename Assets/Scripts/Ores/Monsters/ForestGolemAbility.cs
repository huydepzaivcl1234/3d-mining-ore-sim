using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class ForestGolemSettings
    {
        public bool enabled;
        [Min(0)] public float waveExtraRadius = 1.2f;
        [Min(.05f)] public float waveSeconds = .65f;
        [Min(.01f)] public float waveWidth = .12f;
        public Color waveColor = new(.5f, 1f, .18f, .85f);
        public Material waveMaterial;
        [Tooltip("Optional grass impact visual. Uses the slam's actual radius and clock.")]
        public ForestGrassShockwave impactPrefab;
        [Min(1)] public int hitsToCharge = 3;
        public string chargeState = "ChargeUp";
        [Range(0, 1)] public float chargeReleaseMoment = 1f;
        [Min(0)] public float burnDamagePerTick = 5f;
        [Min(.1f)] public float burnTickSeconds = 1f;
        [Min(0)] public float burnSeconds = 5f;
        [Header("Boss-only pulse and regeneration")]
        public ForestGolemBossSettings bossSkills = new();
    }

    /// <summary>One travelling ground front per slam; three successful player hits prime a charge.</summary>
    public sealed partial class ForestGolemAbility : MonoBehaviour
    {
        private MushroomMonster owner;
        private Animator animator;
        private MiningCharacterHealth player, chargeVictim;
        private Collider playerCollider;
        private readonly HashSet<UnityEngine.Object> struck = new();
        private LineRenderer ring;
        private Material ringMaterial;
        private ForestGrassShockwave grassImpact;
        private Vector3 center, visualCenter;
        private float radius, elapsed, previousRadius, scale, height;
        private int playerHits;
        private bool wave, chargePending;
        public bool IsCharging { get; private set; }
        private ForestGolemSettings Settings => owner != null && owner.SpeciesData is ForestGolemData data ? data.forestGolem : null;
        private void Awake() { owner = GetComponent<MushroomMonster>(); animator = GetComponentInChildren<Animator>(); }
        public void Initialize(MiningCharacterHealth target)
        {
            Cancel(); player = target; playerCollider = target != null ? target.GetComponent<Collider>() : null;
        }

        public void BeginSlam(Vector3 point, float baseRadius, float hitScale, float hitHeight)
        {
            if (Settings == null || !Settings.enabled) return;
            struck.Clear(); center = point; scale = hitScale; height = hitHeight;
            visualCenter = center;
            if (WorldNavigationGrid.Instance != null && WorldNavigationGrid.Instance.TryGetGroundPoint(center, out Vector3 ground))
                visualCenter = ground;
            else
            {
                foreach (Terrain terrain in Terrain.activeTerrains)
                {
                    Vector3 local = center - terrain.transform.position;
                    Vector3 size = terrain.terrainData.size;
                    if (local.x < 0 || local.z < 0 || local.x > size.x || local.z > size.z) continue;
                    visualCenter.y = terrain.SampleHeight(center) + terrain.transform.position.y;
                    break;
                }
            }
            radius = baseRadius + Mathf.Max(0, Settings.waveExtraRadius) * scale;
            NotifyBossSlam(hitScale);
            elapsed = previousRadius = 0; wave = true;
            if (Settings.impactPrefab != null)
            {
                if (grassImpact == null)
                {
                    grassImpact = Instantiate(Settings.impactPrefab, transform);
                    grassImpact.name = "Forest grass impact";
                }
                grassImpact.transform.SetPositionAndRotation(visualCenter, Quaternion.identity);
                grassImpact.PlayControlled(radius, scale);
                Vector3 parentScale = transform.lossyScale;
                grassImpact.transform.localScale = new Vector3(scale / Mathf.Max(.01f, Mathf.Abs(parentScale.x)),
                    scale / Mathf.Max(.01f, Mathf.Abs(parentScale.y)), scale / Mathf.Max(.01f, Mathf.Abs(parentScale.z)));
                if (ring != null) ring.enabled = false;
            }
            else { EnsureRing(); ring.enabled = true; DrawRing(0); }
        }
        public void RememberHit(UnityEngine.Object victim) { if (wave && victim != null) struck.Add(victim); }
        public void NotifyPlayerHit(MiningCharacterHealth victim, float dealt)
        {
            if (Settings == null || !Settings.enabled || dealt <= 0 || victim == null || IsCharging || chargePending) return;
            if (++playerHits < Mathf.Max(1, Settings.hitsToCharge)) return;
            playerHits = 0; chargeVictim = victim; chargePending = true;
        }

        // Called by the AI so Rune time, death and despawning suspend the whole ability together.
        public void Tick(float delta, bool canStartCharge)
        {
            if (Settings == null || !Settings.enabled) { Cancel(); return; }
            TickBossSkills(Mathf.Max(0, delta));
            if (wave)
            {
                elapsed += Mathf.Max(0, delta);
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(.05f, Settings.waveSeconds));
                float current = radius * progress;
                if (player != null && player.Health > 0 && !struck.Contains(player) && CrossesFront(player.transform, playerCollider, current))
                {
                    struck.Add(player); owner.ApplyProjectileHit(player);
                }

                if (grassImpact != null)
                {
                    grassImpact.transform.SetPositionAndRotation(visualCenter, Quaternion.identity);
                    grassImpact.Present(progress);
                }
                else DrawRing(current);
                previousRadius = current;
                if (progress >= 1) { wave = false; HideWave(); struck.Clear(); }
            }
            if (chargePending && canStartCharge && !wave)
            {
                chargePending = false;
                if (chargeVictim == null || chargeVictim.Health <= 0) return;
                int hash = Animator.StringToHash(Settings.chargeState);
                if (!animator.HasState(0, hash)) { chargeVictim = null; return; }
                IsCharging = true;
                animator.speed = 1f;
                animator.CrossFadeInFixedTime(hash, owner.CombatData.animationBlendSeconds, 0, 0);
                // CrossFade is evaluated next animation update; the old completed
                // charge state must not release a newly started charge immediately.
                return;
            }
            if (IsCharging) animator.speed = 1f;
            if (!IsCharging || animator.IsInTransition(0)) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(Settings.chargeState) || state.normalizedTime < Settings.chargeReleaseMoment) return;
            // No distance check: the charged curse follows the player, not the slam's hit volume.
            if (chargeVictim != null && chargeVictim.Health > 0)
                chargeVictim.ApplyBurn(Settings.burnDamagePerTick * owner.RewardData.StatMultiplier(owner.Level), Settings.burnTickSeconds, Settings.burnSeconds);
            chargeVictim = null; IsCharging = false;
            animator.CrossFadeInFixedTime("Idle", owner.CombatData.animationBlendSeconds);
        }
        private bool CrossesFront(Transform victim, Collider collider, float current)
        {
            Vector3 point = collider != null && collider.enabled ? collider.ClosestPoint(center + Vector3.up * .5f) : victim.position;
            if (Mathf.Abs(point.y - center.y) > height) return false;
            float distance = Vector3.ProjectOnPlane(point - center, Vector3.up).magnitude;
            return distance <= current && distance + Settings.waveWidth * scale >= previousRadius && owner.HasStrikeLineOfSight(victim, point);
        }
        private void EnsureRing()
        {
            if (ring != null) return;
            var go = new GameObject("Forest Golem shockwave"); go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>(); ring.useWorldSpace = true; ring.loop = true;
            ring.positionCount = 64; ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; ring.receiveShadows = false;
            ringMaterial = Settings.waveMaterial != null ? new Material(Settings.waveMaterial) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            ring.sharedMaterial = ringMaterial;
        }
        private void DrawRing(float current)
        {
            ring.widthMultiplier = Settings.waveWidth * scale;
            ring.startColor = ring.endColor = Settings.waveColor;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / ring.positionCount;
                Vector3 point = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * current;
                // Terrain height without hitting the golem or the player's collider.
                var terrain = Terrain.activeTerrain;
                if (terrain != null) point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
                ring.SetPosition(i, point + Vector3.up * .06f);
            }
        }
        public void Cancel()
        {
            wave = chargePending = IsCharging = false; playerHits = 0; chargeVictim = null; struck.Clear();
            ResetBossSkills();
            HideWave();
        }
        private void HideWave()
        {
            if (ring != null) ring.enabled = false;
            if (grassImpact != null) grassImpact.Stop();
        }
        private void OnDisable() => Cancel();
        private void OnDestroy() { if (ringMaterial != null) Destroy(ringMaterial); }
    }
}
