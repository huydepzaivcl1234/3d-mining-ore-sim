using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Spawn manager for independent ore prefabs using designer-owned OreSpawnData.</summary>
    [DisallowMultipleComponent]
    public sealed class OreSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private Transform spawnedOreParent;
        [SerializeField] private OreSpawnData spawnData;

        private readonly HashSet<Ore> activeOres = new();
        private Coroutine spawnRoutine;
        private bool initialSpawnCompleted;

        public int ActiveCount => activeOres.Count;

        public bool TryReserveClosestOre(MiningNpc miner, Vector3 origin, int miningPower,
            out Ore reservedOre, out int slotIndex)
        {
            if (spawnData == null)
            {
                reservedOre = null;
                slotIndex = -1;
                return false;
            }

            Ore closest = null;
            float closestSqrDistance = float.PositiveInfinity;

            foreach (Ore ore in activeOres)
            {
                if (ore == null || !ore.CanAcceptMiner(miner, miningPower))
                {
                    continue;
                }

                float sqrDistance = (ore.transform.position - origin).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                closest = ore;
                closestSqrDistance = sqrDistance;
            }

            if (closest != null && closest.TryReserveMiner(miner, miningPower, out slotIndex))
            {
                reservedOre = closest;
                return true;
            }

            reservedOre = null;
            slotIndex = -1;
            return false;
        }

        private void OnEnable()
        {
            RegisterExistingOres();
            if (spawnData == null || !spawnData.SpawnOnEnable)
            {
                return;
            }

            if (!initialSpawnCompleted)
            {
                int amount = Mathf.Min(spawnData.InitialSpawnCount, spawnData.MaximumAliveOres);
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
            if (spawnData == null || spawnData.MaximumAliveOres <= 0 ||
                activeOres.Count >= spawnData.MaximumAliveOres)
            {
                return false;
            }

            OreData data = ChooseOre();
            if (data == null || data.Prefab == null)
            {
                return false;
            }

            Vector3 position = ChoosePosition();
            Quaternion rotation = spawnData.RandomYRotation
                ? Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f)
                : Quaternion.identity;
            Transform parent = spawnedOreParent != null ? spawnedOreParent : transform;
            GameObject instance = Instantiate(data.Prefab, position, rotation, parent);
            float scale = UnityEngine.Random.Range(
                Mathf.Max(0.01f, Mathf.Min(spawnData.UniformScaleRange.x, spawnData.UniformScaleRange.y)),
                Mathf.Max(0.01f, Mathf.Max(spawnData.UniformScaleRange.x, spawnData.UniformScaleRange.y)));
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
                yield return new WaitForSeconds(spawnData.SecondsPerSpawn);
                SpawnOne();
            }
        }

        private OreData ChooseOre()
        {
            float totalWeight = 0f;
            foreach (OreSpawnEntry entry in spawnData.OreSpawnTable)
            {
                if (entry?.Ore != null && entry.SpawnWeight > 0f && entry.Ore.Prefab != null)
                {
                    totalWeight += entry.SpawnWeight;
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float choice = UnityEngine.Random.value * totalWeight;
            foreach (OreSpawnEntry entry in spawnData.OreSpawnTable)
            {
                if (entry?.Ore == null || entry.SpawnWeight <= 0f || entry.Ore.Prefab == null)
                {
                    continue;
                }

                choice -= entry.SpawnWeight;
                if (choice <= 0f)
                {
                    return entry.Ore;
                }
            }

            return null;
        }

        private Vector3 ChoosePosition()
        {
            Vector3 areaSize = spawnData.AreaSize;
            Vector3 local = spawnData.AreaCenter + new Vector3(
                UnityEngine.Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                UnityEngine.Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f),
                UnityEngine.Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f));
            Vector3 world = transform.TransformPoint(local);

            if (spawnData.AlignToGround)
            {
                Vector3 rayOrigin = world + Vector3.up * spawnData.GroundRayStartHeight;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    spawnData.GroundRayDistance, spawnData.GroundLayers, QueryTriggerInteraction.Ignore))
                {
                    world = hit.point;
                }
            }

            world.y += spawnData.HeightOffset;
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

        private void OnDrawGizmosSelected()
        {
            if (spawnData == null)
            {
                return;
            }

            Gizmos.color = new Color(0.95f, 0.65f, 0.12f, 0.65f);
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(spawnData.AreaCenter, spawnData.AreaSize);
            Gizmos.matrix = previous;
        }
    }
}
