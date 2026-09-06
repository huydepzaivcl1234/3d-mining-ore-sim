using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Spawns independent ore prefabs from a designer-configurable weighted pool.</summary>
    [DisallowMultipleComponent]
    public sealed class OreSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private Transform spawnedOreParent;
        [SerializeField] private MiningGameData gameData;

        private readonly HashSet<Ore> activeOres = new();
        private Coroutine spawnRoutine;
        private bool initialSpawnCompleted;

        public int ActiveCount => activeOres.Count;

        public bool TryReserveClosestOre(MiningNpc miner, Vector3 origin, int miningPower,
            out Ore reservedOre, out int slotIndex)
        {
            if (gameData == null)
            {
                reservedOre = null;
                slotIndex = -1;
                return false;
            }

            Ore closest = null;
            float closestSqrDistance = float.PositiveInfinity;

            foreach (Ore ore in activeOres)
            {
                if (ore == null || !ore.CanAcceptMiner(miner, miningPower, gameData.MaximumNpcsPerOre))
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

            if (closest != null && closest.TryReserveMiner(miner, miningPower,
                gameData.MaximumNpcsPerOre, out slotIndex))
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
            if (gameData == null || !gameData.SpawnOnEnable)
            {
                return;
            }

            if (!initialSpawnCompleted)
            {
                int amount = Mathf.Min(gameData.InitialSpawnCount, gameData.MaximumAliveOres);
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
            if (gameData == null || gameData.MaximumAliveOres <= 0 ||
                activeOres.Count >= gameData.MaximumAliveOres)
            {
                return false;
            }

            OreData data = ChooseOre();
            if (data == null || data.Prefab == null)
            {
                return false;
            }

            Vector3 position = ChoosePosition();
            Quaternion rotation = gameData.RandomOreYRotation
                ? Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f)
                : Quaternion.identity;
            Transform parent = spawnedOreParent != null ? spawnedOreParent : transform;
            GameObject instance = Instantiate(data.Prefab, position, rotation, parent);
            float scale = UnityEngine.Random.Range(
                Mathf.Max(0.01f, Mathf.Min(gameData.OreUniformScaleRange.x, gameData.OreUniformScaleRange.y)),
                Mathf.Max(0.01f, Mathf.Max(gameData.OreUniformScaleRange.x, gameData.OreUniformScaleRange.y)));
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
                yield return new WaitForSeconds(gameData.OreSpawnInterval);
                SpawnOne();
            }
        }

        private OreData ChooseOre()
        {
            float totalWeight = 0f;
            foreach (OreSpawnEntry entry in gameData.OrePool)
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
            foreach (OreSpawnEntry entry in gameData.OrePool)
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
            Vector3 areaSize = gameData.OreAreaSize;
            Vector3 local = gameData.OreAreaCenter + new Vector3(
                UnityEngine.Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                UnityEngine.Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f),
                UnityEngine.Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f));
            Vector3 world = transform.TransformPoint(local);

            if (gameData.AlignOresToGround)
            {
                Vector3 rayOrigin = world + Vector3.up * gameData.OreGroundRayStartHeight;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    gameData.OreGroundRayDistance, gameData.OreGroundLayers, QueryTriggerInteraction.Ignore))
                {
                    world = hit.point;
                }
            }

            world.y += gameData.OreHeightOffset;
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
            if (gameData == null)
            {
                return;
            }

            Gizmos.color = new Color(0.95f, 0.65f, 0.12f, 0.65f);
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(gameData.OreAreaCenter, gameData.OreAreaSize);
            Gizmos.matrix = previous;
        }
    }
}
