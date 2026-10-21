using StarterAssets;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController), typeof(ThirdPersonController))]
    public sealed class PlayerMonsterHeadDeflection : MonoBehaviour
    {
        private ThirdPersonController motor;
        private MiningPlayerStats stats;
        private CharacterController capsule;
        private void Awake()
        {
            motor = GetComponent<ThirdPersonController>();
            stats = GetComponent<MiningPlayerStats>();
            capsule = GetComponent<CharacterController>();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var data = stats != null ? stats.Data : null;
            if (data == null || !data.deflectFromMonsterHeads || !motor.enabled ||
                hit.normal.y < data.monsterHeadMinimumUpNormal) return;
            var monster = hit.collider.GetComponentInParent<MushroomMonster>();
            if (monster == null || monster.IsDespawning || monster.Health == null ||
                monster.Health.Health <= 0f || capsule.bounds.min.y < hit.collider.bounds.center.y) return;
            Vector3 away = Vector3.ProjectOnPlane(transform.position - hit.collider.bounds.center, Vector3.up);
            if (away.sqrMagnitude < .001f) away = -transform.forward;
            // Never teleport or push down through the enemy/floor; Move sweeps to the side, then gravity drops us.
            motor.DeflectFromMonsterHead(away, data.monsterHeadSidewaysSpeed,
                data.monsterHeadDownwardSpeed, data.monsterHeadDeflectionSeconds);
        }
    }
}
