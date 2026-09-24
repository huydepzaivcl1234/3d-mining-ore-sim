using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
        [Header("Lava World (same Ground)")]
        [Tooltip("Only these ore assets spawn in Lava World. Edit this list on the scene Ore System.")]
        [SerializeField] private List<OreSpawnEntry> lavaOreSpawnTable = new();
        [SerializeField] private bool lavaWorldActive;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private OreRewardPopup rewardPopupPrefab;
        [SerializeField] private DayNightSystem dayNightSystem;
        [Tooltip("Gates ore spawning by the shared mining power. Auto-found in Awake when left empty.")]
        [SerializeField] private NpcProgressionSystem progressionSystem;

        private readonly HashSet<Ore> activeOres = new();
        private readonly Dictionary<OreData, Queue<Ore>> orePools = new();
        private readonly Dictionary<Ore, OreData> poolOwnedOres = new();
        private readonly HashSet<Ore> inactivePooledOres = new();
        private readonly HashSet<Ore> pendingPoolReturns = new();
        private readonly Queue<OreData> guaranteedOreQueue = new();
        private readonly RaycastHit[] groundHitBuffer = new RaycastHit[32];
        private Coroutine spawnRoutine;
        private bool initialSpawnCompleted;

        public int ActiveCount => activeOres.Count;
        public int PooledCount => inactivePooledOres.Count;
        public OreSpawnData SpawnData => spawnData;
        public bool LavaWorldActive => lavaWorldActive;
        public IReadOnlyList<OreSpawnEntry> ActiveOreSpawnTable => lavaWorldActive
            ? lavaOreSpawnTable : spawnData != null ? spawnData.OreSpawnTable : System.Array.Empty<OreSpawnEntry>();
        public event System.Action WorldChanged;
        public MiningUpgradeSystem UpgradeSystem => upgradeSystem;
        public Vector3 SpawnAreaCenter => spawnData != null ? spawnData.AreaCenter : Vector3.zero;
        public Vector3 SpawnAreaSize => spawnData != null ? spawnData.AreaSize : Vector3.zero;
        public event System.Action<Ore, float> OreRewardGranted;

        private void Awake()
        {
            if (progressionSystem == null)
            {
                progressionSystem = FindFirstObjectByType<NpcProgressionSystem>(
                    FindObjectsInactive.Include);
            }
        }

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

            Ore bestOre = null;
            float bestPathDistance = float.PositiveInfinity;
            float bestDirectSqrDistance = float.PositiveInfinity;
            bool canComparePaths = MiningNavigation.PathfindingAvailable;

            foreach (Ore ore in activeOres)
            {
                if (ore == null || ore == excludedOre || ore == additionallyExcludedOre ||
                    !ore.CanAcceptMiner(miner, miningPower))
                {
                    continue;
                }

                float directSqrDistance = ore.SqrDistanceToSurface(origin);
                if (!canComparePaths)
                {
                    if (directSqrDistance < bestDirectSqrDistance)
                    {
                        bestOre = ore;
                        bestDirectSqrDistance = directSqrDistance;
                    }

                    continue;
                }

                Vector3 destination = ore.GetClosestSurfacePoint(origin);
                if (!MiningNavigation.TryGetPathDistance(origin, destination,
                        out float pathDistance))
                {
                    // A backend exists, so failure means the ore is currently unreachable.
                    continue;
                }

                const float tieTolerance = 0.01f;
                bool shorterPath = pathDistance < bestPathDistance - tieTolerance;
                bool equalPathButCloser = Mathf.Abs(pathDistance - bestPathDistance) <=
                                          tieTolerance &&
                                          directSqrDistance < bestDirectSqrDistance;
                if (!shorterPath && !equalPathButCloser)
                {
                    continue;
                }

                bestOre = ore;
                bestPathDistance = pathDistance;
                bestDirectSqrDistance = directSqrDistance;
            }

            if (bestOre != null && bestOre.TryReserveMiner(miner, miningPower, out slotIndex))
            {
                reservedOre = bestOre;
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
                    ore.RewardGranted -= HandleRewardGranted;
                }
            }
            activeOres.Clear();
        }

        /// <summary>
        /// Queues a specific ore so the very next spawn (right now if there's room, otherwise
        /// the next opening) uses it instead of the normal weighted roll. Used to guarantee
        /// a freshly power-unlocked ore is the first one players see.
        /// </summary>
        public bool SpawnGuaranteedOre(OreData oreData)
        {
            if (oreData == null || oreData.Prefab == null || !IsOreInActiveWorld(oreData))
            {
                return false;
            }

            guaranteedOreQueue.Enqueue(oreData);
            return SpawnOne();
        }

        /// <summary>
        /// Removes active ores that require more power than the supplied value. This is used
        /// when progression is reset so an ore spawned before a Rebirth cannot remain visible
        /// after it becomes locked again.
        /// </summary>
        public void RemoveOresAboveMiningPower(int miningPower)
        {
            miningPower = Mathf.Max(0, miningPower);
            guaranteedOreQueue.Clear();

            var oresToRemove = new List<Ore>();
            foreach (Ore ore in activeOres)
            {
                if (ore != null && ore.Data != null &&
                    ore.Data.MiningPowerRequired > miningPower)
                {
                    oresToRemove.Add(ore);
                }
            }

            foreach (Ore ore in oresToRemove)
            {
                ore.Depleted -= HandleOreDepleted;
                ore.RewardGranted -= HandleRewardGranted;
                activeOres.Remove(ore);
                pendingPoolReturns.Remove(ore);

                if (poolOwnedOres.ContainsKey(ore))
                {
                    ReturnOreToPool(ore);
                    continue;
                }

                // An authored/legacy ore is not part of the runtime pool. Disable it before
                // destroying it so any miner reservation is released immediately.
                ore.gameObject.SetActive(false);
                Destroy(ore.gameObject);
            }
        }

        public bool IsOreInActiveWorld(OreData oreData)
        {
            if (oreData == null) return false;
            foreach (OreSpawnEntry entry in ActiveOreSpawnTable)
                if (entry != null && entry.Ore == oreData) return true;
            return false;
        }

        /// <summary>Switch the existing spawner's ore table and replace only active ores.</summary>
        public bool SetLavaWorld(bool active)
        {
            if (lavaWorldActive == active) return true;
            if (active && !HasSpawnableLavaOre())
            {
                Debug.LogWarning("Lava World has no unlocked ore with a prefab. Configure the Lava World table on Ore System.", this);
                return false;
            }
            if (spawnRoutine != null) { StopCoroutine(spawnRoutine); spawnRoutine = null; }
            guaranteedOreQueue.Clear();
            foreach (Ore ore in new List<Ore>(activeOres))
            {
                if (ore == null) continue;
                ore.Depleted -= HandleOreDepleted;
                ore.RewardGranted -= HandleRewardGranted;
                activeOres.Remove(ore);
                ore.gameObject.SetActive(false);
                if (poolOwnedOres.ContainsKey(ore)) ReturnOreToPool(ore);
            }
            lavaWorldActive = active;
            WorldChanged?.Invoke();
            if (isActiveAndEnabled && spawnData != null)
            {
                int target = Mathf.Min(spawnData.InitialSpawnCount, spawnData.MaximumAliveOres);
                for (int i = 0; i < target; i++) if (!SpawnOne()) break;
                if (spawnData.SpawnOnEnable) spawnRoutine = StartCoroutine(SpawnLoop());
            }
            return true;
        }

        public bool HasSpawnableLavaOre()
        {
            foreach (OreSpawnEntry entry in lavaOreSpawnTable)
                if (entry != null && entry.Ore != null && entry.Ore.Prefab != null &&
                    entry.SpawnChancePercent > 0 && HasSufficientPower(entry.Ore.MiningPowerRequired)) return true;
            return false;
        }

        public bool SpawnOne()
        {
            if (spawnData == null || spawnData.MaximumAliveOres <= 0 ||
                activeOres.Count >= spawnData.MaximumAliveOres)
            {
                return false;
            }

            bool isGuaranteedSpawn = guaranteedOreQueue.Count > 0;
            OreData data = isGuaranteedSpawn ? guaranteedOreQueue.Peek() : ChooseOre();
            if (data == null || data.Prefab == null)
            {
                return false;
            }

            if (!TryChooseSpacedPosition(out Vector3 position))
            {
                return false;
            }

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
            EnsureCircularNavigationObstacle(ore);
            instance.SetActive(true);
            KeepAboveSurface(instance, position.y);
            instance.transform.position += Vector3.up * data.SpawnHeightOffset;
            // Initialize configures hit feedback before final surface placement. Capture the
            // completed position so a hit cannot restore the ore to that earlier Y value.
            ore.FinalizeSpawnPlacement();
            ore.Depleted += HandleOreDepleted;
            ore.RewardGranted += HandleRewardGranted;
            activeOres.Add(ore);
            if (isGuaranteedSpawn)
            {
                guaranteedOreQueue.Dequeue();
            }
            return true;
        }

        private bool TryChooseSpacedPosition(out Vector3 position)
        {
            for (int attempt = 0; attempt < spawnData.PlacementAttempts; attempt++)
            {
                position = ChoosePosition();
                if (HasMinimumOreSpacing(position))
                {
                    return true;
                }
            }

            position = default;
            return false;
        }

        private bool HasMinimumOreSpacing(Vector3 position)
        {
            float minimumSpacing = spawnData.NpcPassageWidth;
            foreach (Ore activeOre in activeOres)
            {
                if (activeOre == null)
                {
                    continue;
                }

                Vector3 offset = position - activeOre.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < minimumSpacing * minimumSpacing)
                {
                    return false;
                }
            }

            return MiningNpc.IsSpawnPositionClear(position, minimumSpacing);
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
            if (!lavaWorldActive && dayNightSystem != null &&
                dayNightSystem.TryChooseSpecialOre(UnityEngine.Random.value * 100f,
                    out OreData specialOre) && specialOre != null && specialOre.Prefab != null &&
                HasSufficientPower(specialOre.MiningPowerRequired) &&
                CanSpawnSpecialOre(specialOre))
            {
                return specialOre;
            }

            OreSpawnEntry selectedEntry = PercentageChanceSelector.Choose(
                ActiveOreSpawnTable,
                GetEffectiveSpawnChancePercent,
                IsSpawnableOreEntry,
                UnityEngine.Random.value);
            return selectedEntry?.Ore;
        }

        private bool IsSpawnableOreEntry(OreSpawnEntry entry)
        {
            return entry != null && entry.Ore != null && entry.Ore.Prefab != null &&
                   HasSufficientPower(entry.Ore.MiningPowerRequired);
        }

        /// <summary>
        /// True when the shared mining power (from <see cref="NpcProgressionSystem"/>) meets
        /// the ore's required power, so under-powered ores never enter the spawn roll.
        /// When no progression system is assigned or found, every power requirement passes
        /// so existing scenes keep spawning exactly as before.
        /// </summary>
        private bool HasSufficientPower(int requiredPower)
        {
            return progressionSystem == null || progressionSystem.CurrentMiningPower >= requiredPower;
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

        private float GetEffectiveSpawnChancePercent(OreSpawnEntry entry)
        {
            OreRaritySpawnRule rule = spawnData.GetRarityRule(entry.Ore.Rarity);
            float rarityMultiplier = rule != null ? rule.BaseChanceMultiplier : 1f;
            float baseChancePercent = Mathf.Clamp(entry.SpawnChancePercent, 0f, 100f) *
                                      rarityMultiplier;
            if (rule == null || !rule.AffectedByRareUpgrade || upgradeSystem == null)
            {
                return baseChancePercent;
            }

            float bonusFraction = Mathf.Max(0f,
                upgradeSystem.GetMultiplier(MiningUpgradeType.RareOreSpawn) - 1f);
            if (baseChancePercent > 0f)
            {
                return baseChancePercent * (1f + bonusFraction);
            }

            return rule.ZeroChanceUnlockAtOneHundredPercentBonus * bonusFraction;
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
            Vector3 areaSize = SpawnAreaSize;
            Vector3 local = SpawnAreaCenter + new Vector3(
                UnityEngine.Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                UnityEngine.Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f),
                UnityEngine.Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f));
            Vector3 world = transform.TransformPoint(local);

            if (spawnData.AlignToGround)
            {
                Vector3 rayOrigin = world + Vector3.up * spawnData.GroundRayStartHeight;
                int hitCount = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, groundHitBuffer,
                    spawnData.GroundRayDistance, spawnData.GroundLayers,
                    QueryTriggerInteraction.Ignore);
                float closestDistance = float.PositiveInfinity;
                for (int index = 0; index < hitCount; index++)
                {
                    RaycastHit hit = groundHitBuffer[index];
                    if (hit.collider == null || hit.distance >= closestDistance ||
                        IsDynamicSpawnBlocker(hit.collider))
                    {
                        continue;
                    }

                    closestDistance = hit.distance;
                    world = hit.point;
                }
            }

            world.y += spawnData.HeightOffset;
            return world;
        }

        private static bool IsDynamicSpawnBlocker(Collider targetCollider)
        {
            return targetCollider.GetComponentInParent<MiningNpc>() != null ||
                   targetCollider.GetComponentInParent<Ore>() != null ||
                   targetCollider.GetComponentInParent<LuckyBlock>() != null;
        }

        private void HandleOreDepleted(Ore ore)
        {
            ore.Depleted -= HandleOreDepleted;
            ore.RewardGranted -= HandleRewardGranted;
            activeOres.Remove(ore);
            if (!poolOwnedOres.ContainsKey(ore))
            {
                return;
            }

            float delay = ore.Data != null
                ? Mathf.Max(ore.Data.DestroyDelay, ore.Data.BreakAnimationDuration)
                : 0f;
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

        private void HandleRewardGranted(Ore ore, float reward)
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
            Sprite rewardIcon = ore.Data != null && ore.Data.Kind == OreKind.Gem
                ? uiData.GemIconSprite
                : null;
            popup.Initialize(reward, popupPosition, uiData, rewardIcon);
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
                EnsureCircularNavigationObstacle(ore);
                ore.Depleted -= HandleOreDepleted;
                ore.Depleted += HandleOreDepleted;
                ore.RewardGranted -= HandleRewardGranted;
                ore.RewardGranted += HandleRewardGranted;
                activeOres.Add(ore);
            }
        }

        private static void EnsureCircularNavigationObstacle(Ore ore)
        {
            if (ore == null)
            {
                return;
            }

            MiningNavMeshObstacle circularObstacle =
                ore.GetComponentInChildren<MiningNavMeshObstacle>(true);
            if (circularObstacle == null)
            {
                circularObstacle = ore.gameObject.AddComponent<MiningNavMeshObstacle>();
            }

            // Remove any extra legacy box obstacles before enabling the one circular owner.
            foreach (NavMeshObstacle obstacle in
                     ore.GetComponentsInChildren<NavMeshObstacle>(true))
            {
                obstacle.carving = false;
                obstacle.enabled = false;
            }

            circularObstacle.EnableCircularCarving();
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
            Gizmos.DrawWireCube(SpawnAreaCenter, SpawnAreaSize);
            Gizmos.matrix = previous;
        }
    }
}
