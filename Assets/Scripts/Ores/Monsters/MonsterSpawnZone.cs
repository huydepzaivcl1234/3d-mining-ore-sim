using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class MonsterSpawnEntry
    {
        public MushroomMonster prefab;
        [Min(0f)] public float chance = 100f;
    }
    public sealed class MonsterSpawnZone : MonoBehaviour
    {
        [SerializeField] private List<MonsterSpawnEntry> monsters = new();
        [SerializeField] private Vector3 areaSize = new(16f, 4f, 16f);
        [Min(0), SerializeField] private int initialCount = 3;
        [Min(0), SerializeField] private int maximumAlive = 6;
        [Min(0.1f), SerializeField] private float secondsPerSpawn = 5f;
        [Min(0.1f), SerializeField] private float minimumSpacing = 2f;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private MiningCharacterHealth player;
        private readonly List<MushroomMonster> alive = new();
        private float timer;
        public int AliveCount => alive.Count;
        public Vector3 RandomPoint() => transform.TransformPoint(new Vector3(
            UnityEngine.Random.Range(-areaSize.x / 2, areaSize.x / 2), 0f,
            UnityEngine.Random.Range(-areaSize.z / 2, areaSize.z / 2)));
        public bool Contains(Vector3 point)
        {
            Vector3 p = transform.InverseTransformPoint(point);
            return Mathf.Abs(p.x) <= areaSize.x / 2 && Mathf.Abs(p.z) <= areaSize.z / 2;
        }
        private void Start()
        {
            for (int i = 0; i < Mathf.Min(initialCount, maximumAlive); i++) SpawnOne();
        }
        private void Update()
        {
            alive.RemoveAll(m => m == null || m.Health.Health <= 0f);
            timer += Time.deltaTime;
            if (timer < Mathf.Max(0.1f, secondsPerSpawn)) return;
            timer = 0f;
            if (alive.Count < maximumAlive) SpawnOne();
        }
        private void SpawnOne()
        {
            float total = 0f;
            foreach (var e in monsters)
                if (e != null && e.prefab != null) total += Mathf.Max(0f, e.chance);
            if (total <= 0f) return;
            float roll = UnityEngine.Random.value * total;
            MushroomMonster prefab = null;
            foreach (var e in monsters)
            {
                if (e == null || e.prefab == null || e.chance <= 0f) continue;
                prefab = e.prefab;
                if ((roll -= e.chance) <= 0f) break;
            }
            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector3 position = RandomPoint();
                if (!Physics.Raycast(position + Vector3.up * 30f, Vector3.down,
                    out var ground, 60f, groundLayers, QueryTriggerInteraction.Ignore)) continue;
                if (ground.normal.y < 0.7f || ground.collider.GetComponentInParent<MushroomMonster>() != null ||
                    ground.collider.GetComponentInParent<MiningCharacterHealth>() != null) continue;
                var capsule = prefab.GetComponent<CharacterController>();
                float radius = capsule.radius * 0.9f;
                Vector3 bottom = ground.point + Vector3.up * (radius + 0.1f);
                Vector3 top = ground.point + Vector3.up * Mathf.Max(radius + 0.1f, capsule.height - radius);
                if (Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore)) continue;
                bool blocked = false;
                foreach (var other in alive)
                    if (other != null && (other.transform.position - ground.point).sqrMagnitude < minimumSpacing * minimumSpacing)
                        blocked = true;
                if (blocked) continue;
                var instance = Instantiate(prefab, ground.point + Vector3.up * 0.05f,
                    Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0), transform);
                instance.Initialize(this, player);
                alive.Add(instance);
                return;
            }
        }
        private void OnDrawGizmosSelected()
        {
            var old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.15f);
            Gizmos.DrawCube(Vector3.zero, areaSize);
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, areaSize);
            Gizmos.matrix = old;
        }
    }
}
