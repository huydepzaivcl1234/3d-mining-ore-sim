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
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningItemSystem inventory;
        private MiningPlayerStats playerStats;
        private OreSpawner oreSpawner;
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
            if (player == null)
            {
                var stats = FindAnyObjectByType<MiningPlayerStats>();
                if (stats != null) player = stats.GetComponent<MiningCharacterHealth>();
            }
            if (player != null) playerStats = player.GetComponent<MiningPlayerStats>();
            if (wallet == null) wallet = FindAnyObjectByType<PlayerWallet>();
            if (inventory == null) inventory = FindAnyObjectByType<MiningItemSystem>();
            oreSpawner = FindAnyObjectByType<OreSpawner>();
            for (int i = 0; i < Mathf.Min(initialCount, maximumAlive); i++) SpawnOne();
        }
        public void GrantRewards(MonsterRewardData data, Vector3 origin, float goldMultiplier = 1)
        {
            if (data == null) return;
            if (playerStats != null) playerStats.AddExperience(Mathf.Max(0, data.experience));
            if (wallet != null)
            {
                float previousMoney = wallet.CurrentMoney;
                wallet.AddMoney(Mathf.Max(0, data.gold) * Mathf.Max(0, goldMultiplier));
                if (oreSpawner == null) oreSpawner = FindAnyObjectByType<OreSpawner>();
                if (oreSpawner != null)
                    oreSpawner.ShowMoneyRewardPopup(wallet.CurrentMoney - previousMoney, origin);
            }
            if (data.drops == null) return;
            foreach (var drop in data.drops)
            {
                if (drop == null || drop.item == null || drop.chancePercent <= 0) continue;
                if (drop.chancePercent < 100 && UnityEngine.Random.value >= drop.chancePercent / 100) continue;
                int min = Mathf.Clamp(drop.minimumAmount, 1, 10000);
                int max = Mathf.Clamp(drop.maximumAmount, min, 10000);
                int count = UnityEngine.Random.Range(min, max + 1);
                MonsterItemPickup.Spawn(data, drop, count, origin, player, inventory, groundLayers);
            }
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
                if (capsule == null) continue;
                Vector3 scale = Vector3.Scale(prefab.transform.lossyScale, transform.lossyScale);
                float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) * 0.9f;
                float height = Mathf.Max(radius * 2, capsule.height * Mathf.Abs(scale.y));
                Vector3 bottom = ground.point + Vector3.up * (radius + 0.1f);
                Vector3 top = ground.point + Vector3.up * Mathf.Max(radius + 0.1f, height - radius);
                if (Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore)) continue;
                bool blocked = false;
                foreach (var other in alive)
                    if (other != null && (other.transform.position - ground.point).sqrMagnitude < minimumSpacing * minimumSpacing)
                        blocked = true;
                if (blocked) continue;
                var instance = Instantiate(prefab, ground.point,
                    Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0), transform);
                instance.Initialize(this, player);
                instance.AlignToGround(ground.point);
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
