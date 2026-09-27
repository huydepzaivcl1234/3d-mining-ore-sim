using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum WeaponHitMode { StraightSingleTarget, ForwardSweep }

    [CreateAssetMenu(menuName = "Mining Simulator/Weapon Attack", fileName = "WeaponAttack")]
    public sealed class WeaponAttackData : ScriptableObject
    {
        public WeaponHitMode hitMode = WeaponHitMode.StraightSingleTarget;
        [Min(0)] public float damageMultiplier = 1f;
        [Min(0.1f)] public float range = 1.75f;
        [Range(1, 180)] public float angle = 30f;
        [Min(0.1f)] public float animationSpeedMultiplier = 1f;
        [Range(0, 1)] public float hitTime = 0.45f;
        public Vector3 hitOriginOffset = new Vector3(0, 1, 0);
        [Tooltip("Vertical reach above and below the strike origin. Angle is evaluated horizontally so short enemies can be hit.")]
        [Min(0.1f)] public float hitHalfHeight = 0.9f;
        [Tooltip("Optional override controller. Keep the existing combat layer, Attack, CombatMode, Speed and AttackSpeed parameters.")]
        public AnimatorOverrideController animationOverrides;
        public AudioClip swingSfx;
        [Range(0, 1)] public float swingVolume = 0.7f;
        public GameObject strikeVfxPrefab;
        public GameObject impactVfxPrefab;
        [Min(0.1f)] public float effectLifetime = 2f;
        public Color trailColor = new Color(0.3f, 0.85f, 1f);
    }
}
