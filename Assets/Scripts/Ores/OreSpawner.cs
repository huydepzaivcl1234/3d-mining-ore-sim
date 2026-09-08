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
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private OreRewardPopup rewardPopupPrefab;
        [SerializeField] private DayNightSystem dayNightSystem;

        private readonly HashSet<Ore> activeOres = new();
        private readonly Dictionary<OreData, Queue<Ore>> orePools = new();
        private readonly Dictionary<Ore, OreData> poolOwnedOres = new();
        private readonly HashSet<Ore> inactivePooledOres = new();
        private readonly HashSet<Ore> pendingPoolReturns = new();
        private Coroutine spawnRoutine;
        private bool initialSpawnCompleted;

        public int ActiveCount => activeOres.Count;
        public int PooledCount => inactivePooledOres.Count;
        public MiningUpgradeSystem UpgradeSystem => upgradeSystem;
        public event System.Action<Ore> OreDamaged;
        public event System.Action<Ore, int> OreRewardGranted;

        public bool TryReserveClosestOre(MiningNpc miner, Vector3 origin, int miningPower,
            out Ore reservedOre, out int slotIndex)
        {
            return TryReserveClosestOre(miner, origin, miningPower, null, null,
                out reservedOre, out slotIndex);
        }

        public bool TryReserveClosestOre(MiningNpc miner, Vector3 origin, int miningPower,
            Ore excludedOre, out Ore reservedOre, out int slotIndex)
        {
            return TryReserveClosestOre(miner, origin, miningPower, excludedOre, null,
                out reservedOre, out slotIndex);
        }

        public bool TryReserveClosestOre(MiningNpc miner, Vector3 origin, int miningPower,
            Ore excludedOre, Ore additionallyExcludedOre, out Ore reservedOre, out int slotIndex)
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
                if (ore == null || ore == excludedOre || ore == additionallyExcludedOre ||
                    !ore.CanAcceptMiner(miner, miningPower))
                {
                    continue;
                }

                float sqrDistance = ore.SqrDistanceToSurface(origin);
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

        public bool TryReserveOre(MiningNpc miner, Ore ore, int miningPower, out int slotIndex)
        {
            slotIndex = -1;
            return ore != null && activeOres.Contains(ore) &&
                   ore.TryReserveMiner(miner, miningPower, out slotIndex);
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

            if (pendingPoolReturns.Count > 0)
            {
                var pending = new List<Ore>(pendingPoolReturns);
                pendingPoolReturns.Clear();
                foreach (Ore ore in pending)
                {
                    ReturnOreToPool(ore);
                }
            }

            foreach (Ore ore in activeOres)
            {
                if (ore != null)
                {
                    ore.Depleted -= HandleOreDepleted;
                    ore.Damaged -= HandleOreDamaged;
                    ore.RewardGranted -= HandleRewardGranted;
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
            Vector2 yRange = spawnData.RandomYRotationRange;
            float randomY = spawnData.RandomYRotation
                ? UnityEngine.Random.Range(Mathf.Min(yRange.x, yRange.y), Mathf.Max(yRange.x, yRange.y))
                : 0f;
            Quaternion rotation = Quaternion.Euler(0f, randomY, 0f) *
                                  Quaternion.Euler(data.SpawnRotationOffset);
            Transform parent = spawnedOreParent != null ? spawnedOreParent : transform;
            Ore ore = TakeOreFromPool(data, parent);
            if (ore == null)
            {
                GameObject created = Instantiate(data.Prefab, parent);
                ore = created.GetComponent<Ore>();
                if (ore == null)
                {
                    Debug.LogError($"Ore prefab '{data.Prefab.name}' has no Ore component.", data.Prefab);
                    Destroy(created);
                    return false;
                }

                created.SetActive(false);
                poolOwnedOres[ore] = data;
            }

            GameObject instance = ore.gameObject;
            instance.transform.SetPositionAndRotation(position, rotation);
            float scale = UnityEngine.Random.Range(
                Mathf.Max(0.01f, Mathf.Min(spawnData.UniformScaleRange.x, spawnData.UniformScaleRange.y)),
                Mathf.Max(0.01f, Mathf.Max(spawnData.UniformScaleRange.x, spawnData.UniformScaleRange.y)));
            instance.transform.localScale = data.Prefab.transform.localScale * scale;
            ore.Initialize(data, wallet, upgradeSystem, false);
            instance.SetActive(true);
            KeepAboveSurface(instance, position.y + data.SpawnHeightOffset);
            ore.Depleted += HandleOreDepleted;
            ore.Damaged += HandleOreDamaged;
            ore.RewardGranted += HandleRewardGranted;
            activeOres.Add(ore);
            return true;
        }

        private Ore TakeOreFromPool(OreData data, Transform parent)
        {
            if (!orePools.TryGetValue(data, out Queue<Ore> pool))
            {
                return null;
            }

            while (pool.Count > 0)
            {
                Ore ore = pool.Dequeue();
                if (ore == null)
                {
                    continue;
                }

                inactivePooledOres.Remove(ore);
                ore.transform.SetParent(parent, false);
                return ore;
            }

            return null;
        }

        private void ReturnOreToPool(Ore ore)
        {
            if (ore == null || !poolOwnedOres.TryGetValue(ore, out OreData data) || data == null)
            {
                return;
            }

            if (!inactivePooledOres.Add(ore))
            {
                return;
            }

            int maximumPooledOres = spawnData != null ? spawnData.MaximumPooledOres : 0;
            if (inactivePooledOres.Count > maximumPooledOres)
            {
                inactivePooledOres.Remove(ore);
                poolOwnedOres.Remove(ore);
                Destroy(ore.gameObject);
                return;
            }

            ore.gameObject.SetActive(false);
            Transform parent = spawnedOreParent != null ? spawnedOreParent : transform;
            ore.transform.SetParent(parent, false);
            if (!orePools.TryGetValue(data, out Queue<Ore> pool))
            {
                pool = new Queue<Ore>();
                orePools.Add(data, pool);
            }
            pool.Enqueue(ore);
        }

        private IEnumerator ReturnOreToPoolAfterDelay(Ore ore, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!pendingPoolReturns.Remove(ore))
            {
                yield break;
            }

            ReturnOreToPool(ore);
        }

        private IEnumerator SpawnLoop()
        {
            while (enabled)
            {
                float speedMultiplier = upgradeSystem != null
                    ? upgradeSystem.GetMultiplier(MiningUpgradeType.OreSpawnSpeed)
                    : 1f;
                yield return new WaitForSeconds(spawnData.SecondsPerSpawn / speedMultiplier);
                SpawnOne();
            }
        }

        private OreData ChooseOre()
        {
            if (dayNightSystem != null &&
                dayNightSystem.TryChooseSpecialOre(UnityEngine.Random.value * 100f,
                    out OreData specialOre) && specialOre != null && specialOre.Prefab != null &&
                CanSpawnSpecialOre(specialOre))
            {
                return specialOre;
            }

            float totalWeight = 0f;
            foreach (OreSpawnEntry entry in spawnData.OreSpawnTable)
            {
                if (entry?.Ore != null && entry.Ore.Prefab != null)
                {
                    totalWeight += Mathf.Max(0f, GetEffectiveWeight(entry));
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float choice = UnityEngine.Random.value * totalWeight;
            foreach (OreSpawnEntry entry in spawnData.OreSpawnTable)
            {
                if (entry?.Ore == null || entry.Ore.Prefab == null)
                {
                    continue;
                }

                float effectiveWeight = GetEffectiveWeight(entry);
                if (effectiveWeight <= 0f)
                {
                    continue;
                }
                choice -= effectiveWeight;
                if (choice <= 0f)
                {
                    return entry.Ore;
                }
            }

            return null;
        }

        private bool CanSpawnSpecialOre(OreData specialOre)
        {
            int maximum = dayNightSystem.GetMaximumActiveSpecialOres(specialOre.Kind);
            if (maximum <= 0)
            {
                return false;
            }

            int activeCount = 0;
            foreach (Ore ore in activeOres)
            {
                if (ore != null && ore.Data != null && ore.Data.Kind == specialOre.Kind)
                {
                    activeCount++;
                }
            }
            return activeCount < maximum;
        }

        private float GetEffectiveWeight(OreSpawnEntry entry)
        {
            OreRaritySpawnRule rule = spawnData.GetRarityRule(entry.Ore.Rarity);
            float rarityMultiplier = rule != null ? rule.BaseWeightMultiplier : 1f;
            float baseWeight = Mathf.Max(0f, entry.SpawnWeight) * rarityMultiplier;
            if (rule == null || !rule.AffectedByRareUpgrade || upgradeSystem == null)
            {
                return baseWeight;
            }

            float bonusFraction = Mathf.Max(0f,
                upgradeSystem.GetMultiplier(MiningUpgradeType.RareOreSpawn) - 1f);
            if (baseWeight > 0f)
            {
                return baseWeight * (1f + bonusFraction);
            }

            return rule.ZeroWeightUnlockAtOneHundredPercentBonus * bonusFraction;
        }

        private void KeepAboveSurface(GameObject instance, float surfaceY)
        {
            if (!spawnData.KeepOreAboveSurface)
            {
                return;
            }

            Collider[] colliders = instance.GetComponentsInChildren<Collider>();
            bool hasBounds = false;
            Bounds combinedBounds = default;
            foreach (Collider targetCollider in colliders)
            {
                if (!targetCollider.enabled || targetCollider.isTrigger)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(targetCollider.bounds);
                }
            }

            if (hasBounds)
            {
                float requiredLift = surfaceY + spawnData.SurfaceClearance - combinedBounds.min.y;
                if (requiredLift > 0f)
                {
                    instance.transform.position += Vector3.up * requiredLift;
                }
            }
            else
            {
                float fallbackLift = surfaceY - instance.transform.position.y;
                if (fallbackLift > 0f)
                {
                    instance.transform.position += Vector3.up * fallbackLift;
                }
            }
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
            ore.Damaged -= HandleOreDamaged;
            ore.RewardGranted -= HandleRewardGranted;
            activeOres.Remove(ore);
            if (!poolOwnedOres.ContainsKey(ore))
            {
                return;
            }

            float delay = ore.Data != null ? ore.Data.DestroyDelay : 0f;
            if (delay > 0f && isActiveAndEnabled)
            {
                pendingPoolReturns.Add(ore);
                StartCoroutine(ReturnOreToPoolAfterDelay(ore, delay));
            }
            else
            {
                ReturnOreToPool(ore);
            }
        }

        private void HandleOreDamaged(Ore ore)
        {
            OreDamaged?.Invoke(ore);
        }

        private void HandleRewardGranted(Ore ore, int reward)
        {
            if (ore == null)
            {
                return;
            }

            OreRewardGranted?.Invoke(ore, reward);
            if (rewardPopupPrefab == null || uiData == null)
            {
                return;
            }

            Vector3 popupPosition = ore.GetWorldTopCenter();
            OreRewardPopup popup = Instantiate(rewardPopupPrefab, popupPosition, Quaternion.identity);
            popup.Initialize(reward, popupPosition, uiData);
        }

        private void RegisterExistingOres()
        {
            activeOres.Clear();
            Transform parent = spawnedOreParent != null ? spawnedOreParent : transform;
            Ore[] existing = parent.GetComponentsInChildren<Ore>(true);
            foreach (Ore ore in existing)
            {
                if (!ore.gameObject.activeInHierarchy)
                {
                    continue;
                }

                ore.ConfigureRuntime(wallet, upgradeSystem);
                ore.Depleted -= HandleOreDepleted;
                ore.Depleted += HandleOreDepleted;
                ore.Damaged -= HandleOreDamaged;
                ore.Damaged += HandleOreDamaged;
                ore.RewardGranted -= HandleRewardGranted;
                ore.RewardGranted += HandleRewardGranted;
                activeOres.Add(ore);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (spawnData == null)
            {
                return;
            }

            Gizmos.color = spawnData.SpawnAreaGizmoColor;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(spawnData.AreaCenter, spawnData.AreaSize);
            Gizmos.matrix = previous;
        }
    }
}
