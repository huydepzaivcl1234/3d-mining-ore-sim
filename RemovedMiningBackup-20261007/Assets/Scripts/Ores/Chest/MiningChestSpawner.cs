using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class MiningChestSpawnEntry
    {
        [SerializeField] private MiningChest prefab;
        [Min(0f), SerializeField] private float weight = 1f;
        public MiningChest Prefab => prefab;
        public float Weight => Mathf.Max(0f, weight);
    }

    /// <summary>Independent, scene-authored chest drops. No ore or lucky block spawn data.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningChestSpawner : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningItemSystem itemSystem;
        [SerializeField] private Transform chestParent;

        [Header("Independent Chest Drop Table")]
        [SerializeField] private List<MiningChestSpawnEntry> chests = new();
        [Min(.1f), SerializeField] private float attemptIntervalSeconds = 10f;
        [Range(0f, 100f), SerializeField] private float dropChancePerAttemptPercent = 25f;
        [Min(1), SerializeField] private int maximumActiveChests = 3;

        [Header("Spawn Area (local XZ, shown in Scene view)")]
        [SerializeField] private Vector2 areaSize = new Vector2(20f, 20f);
        [Min(1f), SerializeField] private float groundProbeHeight = 30f;
        [Min(1f), SerializeField] private float groundProbeDistance = 90f;
        [SerializeField] private LayerMask groundLayers = 1;
        [SerializeField] private LayerMask blockingLayers = ~0;
        [SerializeField] private Vector3 overlapBoxHalfExtents = new Vector3(1.6f, .8f, 1.6f);
        [Min(0), SerializeField] private int positionAttempts = 12;

        private readonly HashSet<MiningChest> activeChests = new();
        private readonly Dictionary<MiningChest, Queue<MiningChest>> pooled = new();
        private readonly Dictionary<MiningChest, MiningChest> instancePrefab = new();
        private readonly Collider[] collisionBuffer = new Collider[48];
        private Coroutine loop;

        public int ActiveCount => activeChests.Count;

        private void OnEnable()
        {
            if (Application.isPlaying) loop = StartCoroutine(SpawnLoop());
        }

        private void OnDisable()
        {
            if (loop != null) StopCoroutine(loop);
            loop = null;
            var remaining = new List<MiningChest>(activeChests);
            foreach (MiningChest chest in remaining) ReturnToPool(chest);
        }

        private IEnumerator SpawnLoop()
        {
            while (isActiveAndEnabled)
            {
                yield return new WaitForSeconds(Mathf.Max(.1f, attemptIntervalSeconds));
                if (activeChests.Count >= Mathf.Max(1, maximumActiveChests) ||
                    dropChancePerAttemptPercent <= 0f ||
                    (dropChancePerAttemptPercent < 100f &&
                     UnityEngine.Random.value >= dropChancePerAttemptPercent * .01f)) continue;
                SpawnOne();
            }
        }

        [ContextMenu("Spawn One Chest (Play Mode)")]
        public void SpawnOne()
        {
            if (!Application.isPlaying || activeChests.Count >= maximumActiveChests) return;
            MiningChest prefab = ChoosePrefab();
            if (prefab == null || !TryFindPosition(out Vector3 position)) return;
            if (!pooled.TryGetValue(prefab, out Queue<MiningChest> queue))
            {
                queue = new Queue<MiningChest>();
                pooled.Add(prefab, queue);
            }

            MiningChest chest = null;
            while (queue.Count > 0 && chest == null) chest = queue.Dequeue();
            if (chest == null)
            {
                chest = Instantiate(prefab, position, transform.rotation,
                    chestParent != null ? chestParent : transform);
            }
            else
            {
                chest.transform.SetPositionAndRotation(position, transform.rotation);
                chest.gameObject.SetActive(true);
            }

            chest.Initialize(wallet, itemSystem, ReturnToPool);
            instancePrefab[chest] = prefab;
            activeChests.Add(chest);
        }

        private MiningChest ChoosePrefab()
        {
            float total = 0f;
            foreach (MiningChestSpawnEntry entry in chests)
                if (entry?.Prefab != null) total += entry.Weight;
            if (total <= 0f) return null;
            float roll = UnityEngine.Random.value * total;
            MiningChest last = null;
            foreach (MiningChestSpawnEntry entry in chests)
            {
                if (entry?.Prefab == null || entry.Weight <= 0f) continue;
                last = entry.Prefab;
                roll -= entry.Weight;
                if (roll <= 0f) return entry.Prefab;
            }
            return last;
        }

        private bool TryFindPosition(out Vector3 position)
        {
            for (int i = 0; i < positionAttempts; i++)
            {
                Vector3 local = new Vector3(UnityEngine.Random.Range(-areaSize.x, areaSize.x) * .5f,
                    0f, UnityEngine.Random.Range(-areaSize.y, areaSize.y) * .5f);
                Vector3 origin = transform.TransformPoint(local) + Vector3.up * groundProbeHeight;
                if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundProbeDistance,
                    groundLayers, QueryTriggerInteraction.Ignore)) continue;
                if (hit.collider.GetComponentInParent<MiningChest>() != null ||
                    hit.collider.GetComponentInParent<Ore>() != null ||
                    hit.collider.GetComponentInParent<LuckyBlock>() != null) continue;

                int count = Physics.OverlapBoxNonAlloc(
                    hit.point + Vector3.up * overlapBoxHalfExtents.y,
                    overlapBoxHalfExtents, collisionBuffer, transform.rotation,
                    blockingLayers, QueryTriggerInteraction.Ignore);
                bool occupied = count >= collisionBuffer.Length;
                for (int j = 0; j < count && !occupied; j++)
                {
                    Collider collider = collisionBuffer[j];
                    if (collider == null || collider == hit.collider) continue;
                    occupied = true;
                }
                if (occupied) continue;
                position = hit.point;
                return true;
            }
            position = default;
            return false;
        }

        private void ReturnToPool(MiningChest chest)
        {
            if (chest == null || !activeChests.Remove(chest)) return;
            chest.gameObject.SetActive(false);
            if (instancePrefab.TryGetValue(chest, out MiningChest prefab) && prefab != null)
            {
                if (!pooled.TryGetValue(prefab, out Queue<MiningChest> queue))
                {
                    queue = new Queue<MiningChest>();
                    pooled.Add(prefab, queue);
                }
                queue.Enqueue(chest);
                return;
            }
            instancePrefab.Remove(chest);
            Destroy(chest.gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, .68f, .16f, .9f);
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, .1f, areaSize.y));
            Gizmos.matrix = previous;
        }
    }
}
