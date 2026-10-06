using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Authored species loadout. Runtime rolls and boss buffs never modify this asset.</summary>
    [Serializable]
    public sealed class MonsterCombatSettings
    {
        [Header("Health")]
        [Min(1f)] public float maxHealth = 100f;
        [Min(0f)] public float regenAmount = 5f;
        [Min(.1f)] public float regenInterval = 5f;
        [Header("Attack speed")]
        [Tooltip("1 = original speed, 2 = twice as fast. Scales attack playback and cooldown only.")]
        [Min(.01f)] public float attackSpeed = 1f;
        public float SafeAttackSpeed => float.IsNaN(attackSpeed) || float.IsInfinity(attackSpeed) ? 1f : Mathf.Max(.01f, attackSpeed);
        [Header("Melee, movement and hit volumes")]
        public string attackState = "Headbutt";
        public string secondAttackState;
        [Range(0f, 1f)] public float secondHitMoment = .52f;
        [Min(.1f)] public float secondAreaRadius = 1.6f;
        public Vector2 secondAreaOffset = new Vector2(.3f, 1.71f);
        [Min(0f)] public float attackTurnSpeed = 720f;
        [Range(0f, 1f)] public float trackingEndFraction = .85f;
        [Min(0f)] public float chaseTurnSpeed = 240f;
        [Min(0f)] public float animationBlendSeconds = .15f;
        [Min(0f)] public float moveSpeed = 1.5f;
        [Min(0f)] public float detectionRange = 6f;
        [Min(.1f)] public float attackRange = 1.5f;
        [Range(1f, 180f)] public float attackArc = 120f;
        [Min(0f)] public float damage = 5f;
        [Min(.1f)] public float attackCooldown = 2f;
        [Range(0f, 1f)] public float hitMoment = 0.45f;
        public MushroomMonster.HitShape hitShape;
        [Min(.1f)] public float areaRadius = 1.6f;
        [Min(0f)] public float areaForwardOffset = 1.6f;
        public float areaSideOffset;
        [Min(.1f)] public float hitHeight = 2.5f;
        public Color warningColor = new Color(1f, .08f, .02f, .65f);
        public Shader warningShader;
        [Min(0f)] public float deathDelay = 3f;
        [Header("Chase navigation")]
        [Min(.05f)] public float chaseRepathSeconds = .4f;
        [Min(4)] public int chaseStandDirections = 16;
        [Range(.1f, .95f)] public float chaseStandRangeFraction = .75f;
        [Range(.1f, 1f)] public float chaseStandMaximumRangeFraction = .95f;
        [Header("Ranged attack (optional)")]
        public HomingMonsterProjectile projectile;
        public string fireState = "Fire";
        [Min(.1f)] public float rangedRange = 8f;
        [Range(0f,1f)] public float releaseMoment = .45f;
        [Header("Projectile")]
        [Min(.1f)] public float projectileSpeed = 8f;
        [Min(0f)] public float projectileTurnDegreesPerSecond = 540f;
        [Min(.01f)] public float projectileRadius = .12f;
        [Min(.1f)] public float projectileLifetime = 8f;
        public LayerMask projectileCollisionLayers = ~0;
    }
}
