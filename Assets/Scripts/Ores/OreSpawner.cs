using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class OreSpawnEntry
    {
        [SerializeField] private OreData data;
        [Min(0f), SerializeField] private float weight = 1f;

        public OreData Data => data;
        public float Weight => weight;
    }

    /// <summary>Spawns independent ore prefabs from a designer-configurable weighted pool.</summary>
    [DisallowMultipleComponent]
    public sealed class OreSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private Transform spawnedOreParent;
        [SerializeField] private List<OreSpawnEntry> orePool = new();

        [Header("Population")]
        [SerializeField] private bool spawnOnEnable = true;
        [Min(0), SerializeField] private int initialSpawnCount = 8;
        [Min(0), SerializeField] private int maximumAlive = 12;
        [Min(0.05f), SerializeField] private float spawnInterval = 2f;

        [Header("Spawn Area")]
        [SerializeField] private Vector3 areaCenter;
        [SerializeField] private Vector3 areaSize = new(16f, 0f, 16f);
        [SerializeField] private float heightOffset;
        [SerializeField] private bool randomYRotation = true;
        [SerializeField] private Vector2 uniformScaleRange = Vector2.one;

        [Header("Optional Ground Placement")]
        [SerializeField] private bool alignToGround;
        [SerializeField] private LayerMask groundLayers = ~0;
        [Min(0.1f), SerializeField] private float groundRayStartHeight = 20f;
        [Min(0.1f), SerializeField] private float groundRayDistance = 50f;

        private readonly HashSet<Ore> activeOres = new();
        private Coroutine spawnRoutine;
        private bool initialSpawnCompleted;

        public int ActiveCount => activeOres.Count;

        private void OnEnable()
        {
            RegisterExistingOres();
            if (!spawnOnEnable)
            {
                return;
            }

            if (!initialSpawnCompleted)
            {
                int amount = Mathf.Min(initialSpawnCount, maximumAlive);
                for (int i = activeOres.Count; i < amount; i++)
                {
                    SpawnOne();
                }
                initialSpawnCompleted = true;
            }

            spawnRoutine = StartCoroutine(SpawnLoop());
        }

        private void OnDisable()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            foreach (Ore ore in activeOres)
            {
                if (ore != null)
                {
                    ore.Depleted -= HandleOreDepleted;
                }
            }
            activeOres.Clear();
        }

        public bool SpawnOne()
        {
            if (maximumAlive <= 0 || activeOres.Count >= maximumAlive)
            {
                return false;
            }

            OreData data = ChooseOre();
            if (data == null || data.Prefab == null)
            {
                return false;
            }

            Vector3 position = ChoosePosition();
            Quaternion rotation = randomYRotation
                ? Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f)
                : Quaternion.identity;
            Transform parent = spawnedOreParent != null ? spawnedOreParent : transform;
            GameObject instance = Instantiate(data.Prefab, position, rotation, parent);
            float scale = UnityEngine.Random.Range(
                Mathf.Max(0.01f, Mathf.Min(uniformScaleRange.x, uniformScaleRange.y)),
                Mathf.Max(0.01f, Mathf.Max(uniformScaleRange.x, uniformScaleRange.y)));
            instance.transform.localScale *= scale;

            Ore ore = instance.GetComponent<Ore>();
            if (ore == null)
            {
                Debug.LogError($"Ore prefab '{data.Prefab.name}' has no Ore component.", data.Prefab);
                Destroy(instance);
                return false;
            }

            ore.Initialize(data, wallet);
            ore.Depleted += HandleOreDepleted;
            activeOres.Add(ore);
            return true;
        }

        private IEnumerator SpawnLoop()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(Mathf.Max(0.05f, spawnInterval));
                SpawnOne();
            }
        }

        private OreData ChooseOre()
        {
            float totalWeight = 0f;
            foreach (OreSpawnEntry entry in orePool)
            {
                if (entry?.Data != null && entry.Weight > 0f && entry.Data.Prefab != null)
                {
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float choice = UnityEngine.Random.value * totalWeight;
            foreach (OreSpawnEntry entry in orePool)
            {
                if (entry?.Data == null || entry.Weight <= 0f || entry.Data.Prefab == null)
                {
                    continue;
                }

                choice -= entry.Weight;
                if (choice <= 0f)
                {
                    return entry.Data;
                }
            }

            return null;
        }

        private Vector3 ChoosePosition()
        {
            Vector3 local = areaCenter + new Vector3(
                UnityEngine.Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                UnityEngine.Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f),
                UnityEngine.Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f));
            Vector3 world = transform.TransformPoint(local);

            if (alignToGround)
            {
                Vector3 rayOrigin = world + Vector3.up * groundRayStartHeight;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    groundRayDistance, groundLayers, QueryTriggerInteraction.Ignore))
                {
                    world = hit.point;
                }
            }

            world.y += heightOffset;
            return world;
        }

        private void HandleOreDepleted(Ore ore)
        {
            ore.Depleted -= HandleOreDepleted;
            activeOres.Remove(ore);
        }

        private void RegisterExistingOres()
        {
            activeOres.Clear();
            Transform parent = spawnedOreParent != null ? spawnedOreParent : transform;
            Ore[] existing = parent.GetComponentsInChildren<Ore>(true);
            foreach (Ore ore in existing)
            {
                ore.Depleted -= HandleOreDepleted;
                ore.Depleted += HandleOreDepleted;
                activeOres.Add(ore);
            }
        }

        private void OnValidate()
        {
            initialSpawnCount = Mathf.Max(0, initialSpawnCount);
            maximumAlive = Mathf.Max(0, maximumAlive);
            spawnInterval = Mathf.Max(0.05f, spawnInterval);
            areaSize = new Vector3(Mathf.Abs(areaSize.x), Mathf.Abs(areaSize.y), Mathf.Abs(areaSize.z));
            groundRayStartHeight = Mathf.Max(0.1f, groundRayStartHeight);
            groundRayDistance = Mathf.Max(0.1f, groundRayDistance);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.65f, 0.12f, 0.65f);
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(areaCenter, areaSize);
            Gizmos.matrix = previous;
        }
    }
}
