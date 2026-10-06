using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Optional ranged loadout; movement, melee and rewards stay with the shared AI.</summary>
    [DisallowMultipleComponent]
    public sealed class MonsterRangedAttack : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private HomingMonsterProjectile projectile;
        [SerializeField] private string fireState = "Fire";
        [SerializeField, Min(.1f)] private float range = 8f;
        [SerializeField, Range(0f, 1f)] private float releaseMoment = .45f;
        private readonly RaycastHit[] sightHits = new RaycastHit[32];
        public string FireState => fireState;
        public float ReleaseMoment => releaseMoment;
        public float Range => range * Mathf.Max(.01f, transform.lossyScale.x);
        public bool IsReady => projectile != null && muzzle != null && !string.IsNullOrEmpty(fireState);

        public bool CanReach(Transform victim)
        {
            if (!IsReady || victim == null) return false;
            Vector3 ray = HomingMonsterProjectile.AimPoint(victim) - muzzle.position;
            if (ray.sqrMagnitude < .001f) return true;
            int count = Physics.RaycastNonAlloc(muzzle.position, ray.normalized, sightHits,
                ray.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (count == sightHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Transform obstacle = sightHits[i].transform;
                if (obstacle.IsChildOf(transform) || obstacle.IsChildOf(victim) ||
                    obstacle.GetComponentInParent<Ore>() != null) continue;
                return false;
            }
            return true;
        }

        public void Fire(MushroomMonster owner, Transform victim)
        {
            if (!IsReady || owner == null || victim == null) return;
            Vector3 aim = HomingMonsterProjectile.AimPoint(victim) - muzzle.position;
            Quaternion rotation = aim.sqrMagnitude > .001f ? Quaternion.LookRotation(aim) : muzzle.rotation;
            var shot = Instantiate(projectile, muzzle.position, rotation);
            shot.Launch(owner, victim);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, Range);
        }
    }
}
