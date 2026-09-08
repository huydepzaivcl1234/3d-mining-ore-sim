using System.Collections;
using System.Collections.Generic;
using Microlight.MicroBar;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Drops pooled Lucky Blocks into unoccupied positions inside the ore spawn area.</summary>
    [DisallowMultipleComponent]
    public sealed class LuckyBlockDropSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LuckyBlockData data;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private OreSpawnData oreSpawnData;
        [SerializeField] private Transform spawnAreaOrigin;
        [SerializeField] private Transform droppedBlockParent;
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private OreRewardPopup rewardPopupPrefab;
        [SerializeField] private GameObject healthBarPrefab;

        private readonly HashSet<LuckyBlock> activeBlocks = new();
        private readonly Dictionary<LuckyBlockType, Queue<LuckyBlock>> pools = new();
        private readonly HashSet<LuckyBlock> pooledBlocks = new();
        private readonly Collider[] overlapResults = new Collider[64];
        private Coroutine dropRoutine;

        private const float AuthoredModelWorldSize = 2.1507f;

        public int ActiveCount => activeBlocks.Count;
        public int PooledCount => pooledBlocks.Count;

        private void OnEnable()
        {
            EnsureDropRoutine();
        }

        private void Start()
        {
            // Setup tools can assign references after OnEnable has already run.
            EnsureDropRoutine();
        }

        private void EnsureDropRoutine()
        {
            if (dropRoutine == null && data != null && oreSpawnData != null &&
                spawnAreaOrigin != null)
            {
                dropRoutine = StartCoroutine(DropLoop());
            }
        }

        private void OnDisable()
        {
            if (dropRoutine != null)
            {
                StopCoroutine(dropRoutine);
                dropRoutine = null;
            }

            if (activeBlocks.Count == 0)
            {
                return;
            }

            var remaining = new List<LuckyBlock>(activeBlocks);
            foreach (LuckyBlock block in remaining)
            {
                ReturnToPool(block);
            }
            activeBlocks.Clear();
        }

        private IEnumerator DropLoop()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(data.DropCheckIntervalSeconds);
                float chance = data.DropChancePerCheckPercent;
                if (activeBlocks.Count >= data.MaximumActiveBlocks || chance <= 0f ||
                    (chance < 100f && Random.value >= chance * 0.01f))
                {
                    continue;
                }

                TryDropOne();
            }
        }

        [ContextMenu("Drop Lucky Block Now")]
        public bool TryDropOne()
        {
            if (data == null || wallet == null || oreSpawnData == null || spawnAreaOrigin == null ||
                activeBlocks.Count >= data.MaximumActiveBlocks)
            {
                return false;
            }

            LuckyBlockVariantData variant = ChooseVariant();
            if (variant == null || variant.Model == null ||
                !TryChooseLandingPosition(variant, out Vector3 landingPosition))
            {
                return false;
            }

            LuckyBlock block = TakeFromPool(variant) ?? CreateBlock(variant);
            if (block == null)
            {
                return false;
            }

            float sizeMultiplier = data.BlockSize * variant.SizeMultiplier;
            float worldSize = AuthoredModelWorldSize * sizeMultiplier;
            float minimumAngle = Mathf.Min(data.RandomYRotationMinimum, data.RandomYRotationMaximum);
            float maximumAngle = Mathf.Max(data.RandomYRotationMinimum, data.RandomYRotationMaximum);
            block.transform.SetPositionAndRotation(
                landingPosition + Vector3.up * data.DropHeight,
                Quaternion.identity);
            block.transform.localScale = Vector3.one;
            NormalizeVisualAndCollider(block, worldSize);
            block.transform.rotation = Quaternion.Euler(0f,
                Random.Range(minimumAngle, maximumAngle), 0f);
            block.gameObject.SetActive(true);
            float minimumSpin = Mathf.Min(data.FallingSpinRange.x, data.FallingSpinRange.y);
            float maximumSpin = Mathf.Max(data.FallingSpinRange.x, data.FallingSpinRange.y);
            block.Initialize(variant, data, wallet, block.transform.GetChild(0),
                Random.Range(minimumSpin, maximumSpin));
            Subscribe(block);
            activeBlocks.Add(block);
            return true;
        }

        private LuckyBlockVariantData ChooseVariant()
        {
            float totalWeight = 0f;
            foreach (LuckyBlockVariantData variant in data.Variants)
            {
                if (variant?.Model != null)
                {
                    totalWeight += Mathf.Max(0f, variant.SelectionWeight);
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float selection = Random.value * totalWeight;
            foreach (LuckyBlockVariantData variant in data.Variants)
            {
                if (variant?.Model == null)
                {
                    continue;
                }

                selection -= Mathf.Max(0f, variant.SelectionWeight);
                if (selection <= 0f)
                {
                    return variant;
                }
            }

            return null;
        }

        private bool TryChooseLandingPosition(LuckyBlockVariantData variant,
            out Vector3 landingPosition)
        {
            Vector3 areaSize = oreSpawnData.AreaSize;
            float worldSize = data.BlockSize * variant.SizeMultiplier * AuthoredModelWorldSize;
            float checkRadius = worldSize * 0.5f + data.PlacementClearance;
            for (int attempt = 0; attempt < data.PositionAttemptsPerDrop; attempt++)
            {
                Vector3 local = oreSpawnData.AreaCenter + new Vector3(
                    Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                    Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f),
                    Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f));
                Vector3 candidate = spawnAreaOrigin.TransformPoint(local);
                if (oreSpawnData.AlignToGround)
                {
                    Vector3 rayOrigin = candidate + Vector3.up * oreSpawnData.GroundRayStartHeight;
                    if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                        oreSpawnData.GroundRayDistance, oreSpawnData.GroundLayers,
                        QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    candidate = hit.point;
                }

                candidate.y += oreSpawnData.HeightOffset;
                if (OverlapsMineable(candidate + Vector3.up * checkRadius, checkRadius))
                {
                    continue;
                }

                landingPosition = candidate;
                return true;
            }

            landingPosition = default;
            return false;
        }

        private bool OverlapsMineable(Vector3 center, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, overlapResults, ~0,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider candidate = overlapResults[i];
                if (candidate != null &&
                    (candidate.GetComponentInParent<Ore>() != null ||
                     candidate.GetComponentInParent<LuckyBlock>() != null))
                {
                    return true;
                }
            }

            return false;
        }

        private LuckyBlock CreateBlock(LuckyBlockVariantData variant)
        {
            GameObject root = new(variant.DisplayName);
            root.transform.SetParent(droppedBlockParent != null ? droppedBlockParent : transform, false);
            root.SetActive(false);
            GameObject visual = Instantiate(variant.Model, root.transform);
            visual.name = "Model";

            BoxCollider targetCollider = root.AddComponent<BoxCollider>();
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            LuckyBlock block = root.AddComponent<LuckyBlock>();
            AddHealthBar(root, block);
            return block;
        }

        private void AddHealthBar(GameObject root, LuckyBlock block)
        {
            if (healthBarPrefab == null)
            {
                return;
            }

            GameObject barObject = Instantiate(healthBarPrefab, root.transform);
            barObject.name = "Lucky Block Health Bar";
            MicroBar bar = barObject.GetComponent<MicroBar>();
            LuckyBlockHealthBar binding = barObject.AddComponent<LuckyBlockHealthBar>();
            binding.Configure(block, bar, barObject.transform);
        }

        private static void NormalizeVisualAndCollider(LuckyBlock block, float targetWorldSize)
        {
            Transform root = block.transform;
            Transform visual = root.childCount > 0 ? root.GetChild(0) : null;
            BoxCollider targetCollider = block.GetComponent<BoxCollider>();
            if (visual == null || targetCollider == null)
            {
                return;
            }

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (!TryGetLocalRendererBounds(root, renderers, out Bounds localBounds))
            {
                targetCollider.center = new Vector3(0f, targetWorldSize * 0.5f, 0f);
                targetCollider.size = Vector3.one * targetWorldSize;
                return;
            }

            float currentSize = Mathf.Max(localBounds.size.x,
                Mathf.Max(localBounds.size.y, localBounds.size.z));
            if (currentSize > Mathf.Epsilon)
            {
                visual.localScale *= targetWorldSize / currentSize;
            }

            if (TryGetLocalRendererBounds(root, renderers, out localBounds))
            {
                visual.localPosition += new Vector3(-localBounds.center.x, -localBounds.min.y,
                    -localBounds.center.z);
            }

            if (TryGetLocalRendererBounds(root, renderers, out localBounds))
            {
                targetCollider.center = localBounds.center;
                targetCollider.size = localBounds.size;
            }
        }

        private static bool TryGetLocalRendererBounds(Transform root, Renderer[] renderers,
            out Bounds localBounds)
        {
            bool hasBounds = false;
            localBounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Bounds rendererBounds = renderer.localBounds;
                Vector3 minimum = rendererBounds.min;
                Vector3 maximum = rendererBounds.max;
                for (int x = 0; x <= 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                    {
                        for (int z = 0; z <= 1; z++)
                        {
                            Vector3 rendererCorner = new(
                                x == 0 ? minimum.x : maximum.x,
                                y == 0 ? minimum.y : maximum.y,
                                z == 0 ? minimum.z : maximum.z);
                            Vector3 localCorner = root.InverseTransformPoint(
                                renderer.transform.TransformPoint(rendererCorner));
                            if (!hasBounds)
                            {
                                localBounds = new Bounds(localCorner, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                localBounds.Encapsulate(localCorner);
                            }
                        }
                    }
                }
            }

            return hasBounds;
        }

        private LuckyBlock TakeFromPool(LuckyBlockVariantData variant)
        {
            if (!pools.TryGetValue(variant.Type, out Queue<LuckyBlock> pool))
            {
                return null;
            }

            while (pool.Count > 0)
            {
                LuckyBlock block = pool.Dequeue();
                if (block == null)
                {
                    continue;
                }

                pooledBlocks.Remove(block);
                block.transform.SetParent(droppedBlockParent != null ? droppedBlockParent : transform,
                    false);
                return block;
            }

            return null;
        }

        private void ReturnToPool(LuckyBlock block)
        {
            if (block == null)
            {
                return;
            }

            Unsubscribe(block);
            activeBlocks.Remove(block);
            if (pooledBlocks.Count >= data.MaximumPooledBlocks)
            {
                Destroy(block.gameObject);
                return;
            }

            if (!pooledBlocks.Add(block))
            {
                return;
            }

            block.gameObject.SetActive(false);
            if (!pools.TryGetValue(block.Type, out Queue<LuckyBlock> pool))
            {
                pool = new Queue<LuckyBlock>();
                pools.Add(block.Type, pool);
            }
            pool.Enqueue(block);
        }

        private void Subscribe(LuckyBlock block)
        {
            block.RewardGranted -= HandleRewardGranted;
            block.RewardGranted += HandleRewardGranted;
            block.Broken -= HandleBlockFinished;
            block.Broken += HandleBlockFinished;
            block.Expired -= HandleBlockFinished;
            block.Expired += HandleBlockFinished;
        }

        private void Unsubscribe(LuckyBlock block)
        {
            block.RewardGranted -= HandleRewardGranted;
            block.Broken -= HandleBlockFinished;
            block.Expired -= HandleBlockFinished;
        }

        private void HandleRewardGranted(LuckyBlock block, int amount)
        {
            if (block == null || amount <= 0 || rewardPopupPrefab == null || uiData == null)
            {
                return;
            }

            Vector3 position = block.GetWorldTopCenter();
            OreRewardPopup popup = Instantiate(rewardPopupPrefab, position, Quaternion.identity);
            popup.Initialize(amount, position, uiData);
        }

        private void HandleBlockFinished(LuckyBlock block)
        {
            ReturnToPool(block);
        }
    }
}
