using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Microlight.MicroBar;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class MiningChestItemChance
    {
        [SerializeField] private MiningItemData item;
        [Min(0f), SerializeField] private float weight = 1f;
        public MiningItemData Item => item;
        public float Weight => Mathf.Max(0f, weight);
    }

    /// <summary>A mineable chest with rewards independent of Ore and Lucky Block data.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningChest : MonoBehaviour
    {
        public enum ChestKind { WoodMoney, ItemRoll }

        [Header("Chest Model (edit the prefab)")]
        [SerializeField] private ChestKind kind;
        [SerializeField] private Transform lidHinge;
        [SerializeField] private Transform lockModel;
        [SerializeField] private Transform lootModel;
        [SerializeField] private SpriteRenderer lidRewardIcon;
        [SerializeField] private TextMeshPro rewardText;
        [SerializeField] private Collider hitCollider;
        [Header("Overhead presentation")]
        [SerializeField] private MicroBar healthBarPrefab;
        [SerializeField] private OreRewardPopup moneyPopupPrefab;
        [SerializeField] private MiningUiData moneyPopupUiData;
        [SerializeField] private Vector3 overheadOffset = new Vector3(0f, .3f, 0f);
        [Min(.01f), SerializeField] private float healthBarWorldScale = .65f;
        [Min(0f), SerializeField] private float rewardHeightAboveBar = .55f;
        [Header("Chest SFX (empty slots are silent)")]
        [SerializeField] private AudioClip chestHitSfx;
        [SerializeField] private AudioClip lockBreakSfx;
        [SerializeField] private AudioClip chestOpenSfx;
        [SerializeField] private AudioClip itemRollTickSfx;
        [Min(1), SerializeField] private int hitsToBreak = 5;
        [Header("NPC mining")]
        [Min(1), SerializeField] private int npcMiningPowerRequired = 1;
        [Min(1), SerializeField] private int maximumMiningNpcs = 3;
        [Min(.01f), SerializeField] private float npcDamageMultiplier = 1f;
        [Min(0f), SerializeField] private float npcMaxVerticalTargetDistance = 8f;
        [SerializeField] private Vector3 lidOpenEuler = new Vector3(-95f, 0f, 0f);
        [Min(0.01f), SerializeField] private float lockBreakSeconds = .2f;
        [Min(0.01f), SerializeField] private float lidOpenSeconds = .55f;
        [Min(0f), SerializeField] private float rewardDisplaySeconds = 3f;
        [Min(0f), SerializeField] private float fadeOutSeconds = .7f;

        [Header("Wood Chest: percent of current money")]
        [Range(0f, 100f), SerializeField] private float minimumMoneyPercent = 1f;
        [Range(0f, 100f), SerializeField] private float maximumMoneyPercent = 100f;

        [Header("Item Chest: this table does not use item/ore/lucky drop chances")]
        [SerializeField] private List<MiningChestItemChance> itemChances = new();
        [Tooltip("Relative weights for quantities 1 through 10. Default 10,9,...,1 makes larger amounts rarer.")]
        [SerializeField] private float[] quantityWeights = { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
        [Min(.2f), SerializeField] private float iconRollSeconds = 2.8f;
        [Min(.2f), SerializeField] private float quantityRollSeconds = 1.25f;

        private PlayerWallet wallet;
        private MiningItemSystem itemSystem;
        private Action<MiningChest> release;
        private Quaternion closedRotation;
        private Vector3 initialLockScale;
        private Vector3 initialLockPosition;
        private Quaternion initialLockRotation;
        private Collider[] chestColliders;
        private bool[] initialColliderStates;
        private Vector3 lastChestTop;
        private MeshRenderer chestBodyRenderer;
        private MeshRenderer chestLidRenderer;
        private MiningNavMeshObstacle navigationObstacle;
        private bool capturedModelState;
        private int remainingHits;
        private bool opening;
        private bool rewardPending;
        private MiningItemData selectedItem;
        private int selectedAmount;
        private MicroBar healthBar;
        private TextMeshPro healthText;
        private MiningHitPunch hitPunch;
        private MiningAudioManager audioManager;
        private float nextRollSfxTime;
        private static AudioSource sharedChestAudioSource;
        private static readonly List<MiningChest> ActiveChests = new();
        private readonly HashSet<MiningNpc> reservedMiners = new();
        private readonly List<(MeshRenderer renderer, Material[] originals, Material[] copies,
            Color[] colors)> fadeMaterials = new();
        private float npcDamageRemainder;
        private Color fadeTextColor;
        private Color fadeIconColor;

        public ChestKind Kind => kind;
        public bool CanMine => isActiveAndEnabled && !opening && !rewardPending && remainingHits > 0;
        public int RemainingHits => remainingHits;

        public static bool TryReserveClosest(MiningNpc miner, Vector3 position, int miningPower,
            MiningChest excluded, out MiningChest selected)
        {
            selected = null;
            float closest = float.PositiveInfinity;
            foreach (MiningChest chest in ActiveChests)
            {
                if (chest == null || chest == excluded || !chest.CanAcceptMiner(miner, miningPower) ||
                    Mathf.Abs(chest.transform.position.y - position.y) > chest.npcMaxVerticalTargetDistance)
                    continue;
                float distance = chest.SqrDistanceToSurface(position);
                if (distance >= closest) continue;
                selected = chest;
                closest = distance;
            }
            return selected != null && selected.TryReserveMiner(miner, miningPower);
        }

        public bool CanAcceptMiner(MiningNpc miner, int miningPower)
        {
            reservedMiners.RemoveWhere(candidate => candidate == null);
            return miner != null && CanMine && miningPower >= npcMiningPowerRequired &&
                (reservedMiners.Contains(miner) || reservedMiners.Count < maximumMiningNpcs);
        }

        public bool TryReserveMiner(MiningNpc miner, int miningPower)
        {
            if (!CanAcceptMiner(miner, miningPower)) return false;
            reservedMiners.Add(miner);
            return true;
        }

        public void ReleaseMiner(MiningNpc miner)
        {
            if (miner != null) reservedMiners.Remove(miner);
        }

        public Vector3 GetWorldTopCenter() => GetChestTop();

        public Vector3 GetClosestSurfacePoint(Vector3 position)
        {
            Vector3 closest = hitCollider != null && hitCollider.enabled
                ? hitCollider.ClosestPoint(position)
                : chestBodyRenderer != null ? chestBodyRenderer.bounds.ClosestPoint(position) : transform.position;
            closest.y = position.y;
            return closest;
        }

        public bool TryGetWorldBounds(out Bounds bounds)
        {
            if (!CanMine) { bounds = default; return false; }
            if (chestBodyRenderer != null) { bounds = chestBodyRenderer.bounds; return true; }
            if (hitCollider != null && hitCollider.enabled) { bounds = hitCollider.bounds; return true; }
            bounds = default;
            return false;
        }

        public float SqrDistanceToSurface(Vector3 position)
        {
            Vector3 closest = GetClosestSurfacePoint(position);
            Vector3 delta = position - closest;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }

        private void OnEnable()
        {
            if (Application.isPlaying && gameObject.scene.IsValid() && !ActiveChests.Contains(this))
                ActiveChests.Add(this);
        }

        private void Awake() => CaptureModelState();

        private void Start()
        {
            // A chest placed by hand in the scene remains usable without the spawner.
            if (remainingHits > 0) return;
            Initialize(wallet != null ? wallet : FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include),
                itemSystem != null ? itemSystem : FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include),
                null);
        }

        private void CaptureModelState()
        {
            if (capturedModelState) return;
            chestBodyRenderer = GetComponent<MeshRenderer>();
            AlignLidHingeWithBody();
            // Reward visuals must not inherit the rotation of the lid while it opens.
            if (lidRewardIcon != null && lidRewardIcon.transform.parent != transform)
                lidRewardIcon.transform.SetParent(transform, true);
            closedRotation = lidHinge != null ? lidHinge.localRotation : Quaternion.identity;
            initialLockScale = lockModel != null ? lockModel.localScale : Vector3.one;
            initialLockPosition = lockModel != null ? lockModel.localPosition : Vector3.zero;
            initialLockRotation = lockModel != null ? lockModel.localRotation : Quaternion.identity;
            chestColliders = GetComponentsInChildren<Collider>(true);
            initialColliderStates = new bool[chestColliders.Length];
            for (int index = 0; index < chestColliders.Length; index++)
                initialColliderStates[index] = chestColliders[index].enabled;
            capturedModelState = true;
        }

        private void AlignLidHingeWithBody()
        {
            if (lidHinge == null) return;
            MeshRenderer lid = lidHinge.GetComponentInChildren<MeshRenderer>(true);
            chestLidRenderer = lid;
            MeshFilter lidFilter = lid != null ? lid.GetComponent<MeshFilter>() : null;
            if (lidFilter == null || lidFilter.sharedMesh == null) return;

            // Rotate around the lid's own rear lower edge. Using the body's
            // rear edge offsets the hinge on models with different lid depth.
            // Leave the authored hinge (and any MeshCollider on it) in place.
            Transform lidModel = lid.transform;
            Vector3 lidPosition = lidModel.position;
            Quaternion lidRotation = lidModel.rotation;
            Bounds lidBounds = lidFilter.sharedMesh.bounds;
            Vector3 lidRearBottom = transform.InverseTransformPoint(lidModel.TransformPoint(
                new Vector3(lidBounds.center.x, lidBounds.min.y, lidBounds.min.z)));
            var pivotObject = new GameObject("Chest Lid Rotation Pivot");
            Transform pivot = pivotObject.transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = lidRearBottom;
            lidModel.SetParent(pivot, true);
            lidModel.SetPositionAndRotation(lidPosition, lidRotation);
            lidHinge = pivot;
        }

        public void Initialize(PlayerWallet targetWallet, MiningItemSystem targetItemSystem,
            Action<MiningChest> returnToSpawner)
        {
            CaptureModelState();
            StopAllCoroutines();
            ResetFadeVisuals();
            reservedMiners.Clear();
            npcDamageRemainder = 0f;
            wallet = targetWallet;
            itemSystem = targetItemSystem;
            release = returnToSpawner;
            remainingHits = Mathf.Max(1, hitsToBreak);
            opening = rewardPending = false;
            selectedItem = null;
            selectedAmount = 0;
            nextRollSfxTime = 0f;
            hitPunch ??= GetComponent<MiningHitPunch>();
            hitPunch ??= gameObject.AddComponent<MiningHitPunch>();
            hitPunch.Configure(transform, .075f, .075f, .2f);
            audioManager ??= FindFirstObjectByType<MiningAudioManager>(FindObjectsInactive.Include);
            if (healthBar == null && healthBarPrefab != null)
            {
                healthBar = Instantiate(healthBarPrefab, transform);
                healthBar.name = "Chest Health Bar";
                healthBar.Initialize(Mathf.Max(1, hitsToBreak));
                healthText = healthBar.GetComponentInChildren<TextMeshPro>(true);
            }
            if (healthBar != null)
            {
                healthText ??= healthBar.GetComponentInChildren<TextMeshPro>(true);
                healthBar.gameObject.SetActive(true);
                if (healthText != null) healthText.gameObject.SetActive(true);
                healthBar.SetNewMaxHP(Mathf.Max(1, hitsToBreak), true);
                healthBar.UpdateBar(remainingHits, true);
                UpdateHealthText();
            }
            if (lidHinge != null) lidHinge.localRotation = closedRotation;
            if (lockModel != null)
            {
                lockModel.localScale = initialLockScale;
                lockModel.localPosition = initialLockPosition;
                lockModel.localRotation = initialLockRotation;
                lockModel.gameObject.SetActive(true);
            }
            if (lootModel != null) lootModel.gameObject.SetActive(true);
            if (lidRewardIcon != null)
            {
                lidRewardIcon.sprite = null;
                lidRewardIcon.gameObject.SetActive(false);
            }
            if (rewardText != null) rewardText.gameObject.SetActive(false);
            for (int index = 0; index < chestColliders.Length; index++)
                if (chestColliders[index] != null)
                    chestColliders[index].enabled = initialColliderStates[index];
            navigationObstacle ??= GetComponent<MiningNavMeshObstacle>();
            navigationObstacle ??= gameObject.AddComponent<MiningNavMeshObstacle>();
            navigationObstacle.enabled = true;
            navigationObstacle.EnableCircularCarving();
            lastChestTop = GetChestTop();
        }

        public bool MineOnce()
        {
            if (rewardPending) return ClaimPendingItem();
            if (!CanMine) return false;
            return ApplyHit(1);
        }

        public bool ApplyNpcDamage(float damage)
        {
            if (!CanMine || damage <= 0f) return false;
            float accumulated = damage * npcDamageMultiplier + npcDamageRemainder;
            int hits = Mathf.FloorToInt(Mathf.Min(accumulated, remainingHits));
            npcDamageRemainder = hits >= remainingHits ? 0f : accumulated - hits;
            return hits <= 0 || ApplyHit(hits);
        }

        private bool ApplyHit(int hits)
        {
            remainingHits = Mathf.Max(0, remainingHits - hits);
            if (healthBar != null) healthBar.UpdateBar(remainingHits);
            UpdateHealthText();
            hitPunch?.Play();
            if (remainingHits <= 0)
            {
                reservedMiners.Clear();
                opening = true;
                lastChestTop = GetChestTop();
                foreach (Collider chestCollider in chestColliders)
                    if (chestCollider != null) chestCollider.enabled = false;
                if (navigationObstacle != null) navigationObstacle.enabled = false;
                if (healthBar != null) healthBar.gameObject.SetActive(false);
                PlayChestSfx(lockBreakSfx);
                StartCoroutine(OpenAndReward());
            }
            else PlayChestSfx(chestHitSfx);
            return true;
        }

        private IEnumerator OpenAndReward()
        {
            if (lockModel != null)
            {
                Vector3 start = initialLockScale;
                Vector3 startPosition = initialLockPosition;
                float elapsed = 0f;
                while (elapsed < lockBreakSeconds)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / lockBreakSeconds));
                    lockModel.localScale = Vector3.Lerp(start, Vector3.zero, t);
                    lockModel.localPosition = startPosition +
                        (Vector3.down * .85f + Vector3.forward * .2f) * (t * t);
                    lockModel.localRotation = initialLockRotation * Quaternion.Euler(0f, 0f, 110f * t);
                    yield return null;
                }
                lockModel.gameObject.SetActive(false);
            }

            PlayChestSfx(chestOpenSfx);
            if (lidHinge != null)
            {
                float elapsed = 0f;
                Quaternion open = closedRotation * Quaternion.Euler(lidOpenEuler);
                while (elapsed < lidOpenSeconds)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / lidOpenSeconds);
                    float t = progress * progress * (3f - 2f * progress);
                    lidHinge.localRotation = Quaternion.Slerp(closedRotation, open, t);
                    yield return null;
                }
                lidHinge.localRotation = open;
            }

            if (kind == ChestKind.WoodMoney)
            {
                float balance = wallet != null ? wallet.CurrentMoney : 0f;
                float percent = UnityEngine.Random.Range(
                    Mathf.Min(minimumMoneyPercent, maximumMoneyPercent),
                    Mathf.Max(minimumMoneyPercent, maximumMoneyPercent));
                float amount = balance * percent * .01f;
                if (wallet != null)
                {
                    wallet.AddMoney(amount);
                    amount = wallet.CurrentMoney - balance;
                }
                if (moneyPopupPrefab != null && moneyPopupUiData != null)
                {
                    Vector3 top = GetChestTop();
                    OreRewardPopup popup = Instantiate(moneyPopupPrefab, top, Quaternion.identity);
                    popup.Initialize(amount, top, moneyPopupUiData);
                }
                else ShowText($"+{MiningMoneyFormatter.Format(amount)}", true);
            }
            else
            {
                selectedItem = RollItem();
                if (selectedItem != null)
                {
                    selectedAmount = RollQuantity();
                    yield return RollIcon();
                    yield return RollQuantityVisual();
                    rewardPending = true;
                    ClaimPendingItem();
                }
                else
                {
                    Debug.LogWarning("Chest has no positive-weight item rewards configured.", this);
                    ShowText(MiningLocalization.Text("No chest rewards configured", "Chưa cài phần thưởng rương"), true);
                }
            }

            opening = false;
            if (!rewardPending)
            {
                if (rewardDisplaySeconds > 0f) yield return new WaitForSeconds(rewardDisplaySeconds);
                yield return FadeAndRelease();
            }
        }

        private MiningItemData RollItem()
        {
            float total = 0f;
            foreach (MiningChestItemChance entry in itemChances)
                if (entry?.Item != null) total += entry.Weight;
            if (total <= 0f) return null;
            float roll = UnityEngine.Random.value * total;
            MiningItemData last = null;
            foreach (MiningChestItemChance entry in itemChances)
            {
                if (entry?.Item == null || entry.Weight <= 0f) continue;
                last = entry.Item;
                roll -= entry.Weight;
                if (roll <= 0f) return entry.Item;
            }
            return last;
        }

        private int RollQuantity()
        {
            float total = 0f;
            for (int i = 0; i < 10; i++)
                total += Mathf.Max(0f, quantityWeights != null && i < quantityWeights.Length ? quantityWeights[i] : 0f);
            if (total <= 0f) return 1;
            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < 10; i++)
            {
                float weight = Mathf.Max(0f, quantityWeights != null && i < quantityWeights.Length ? quantityWeights[i] : 0f);
                if (weight <= 0f) continue;
                roll -= weight;
                if (roll <= 0f) return i + 1;
            }
            return 10;
        }

        private IEnumerator RollIcon()
        {
            float elapsed = 0f;
            float nextTick = 0f;
            while (elapsed < iconRollSeconds)
            {
                float progress = Mathf.Clamp01(elapsed / iconRollSeconds);
                if (elapsed >= nextTick)
                {
                    MiningItemData preview = RollItem();
                    DisplayIcon(preview);
                    ShowText(preview != null ? preview.DisplayName : "?", false);
                    PlayRollTick();
                    nextTick += Mathf.Lerp(.045f, .32f, progress * progress);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            DisplayIcon(selectedItem);
            ShowText(selectedItem.DisplayName, false);
        }

        private IEnumerator RollQuantityVisual()
        {
            float elapsed = 0f;
            float nextTick = 0f;
            while (elapsed < quantityRollSeconds)
            {
                float progress = Mathf.Clamp01(elapsed / quantityRollSeconds);
                if (elapsed >= nextTick)
                {
                    ShowText($"{selectedItem.DisplayName}  x{UnityEngine.Random.Range(1, 11)}", false);
                    PlayRollTick();
                    nextTick += Mathf.Lerp(.05f, .22f, progress);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            ShowText($"{selectedItem.DisplayName}  x{selectedAmount}", true);
        }

        private bool ClaimPendingItem()
        {
            if (!rewardPending || selectedItem == null) return false;
            if (itemSystem == null || !itemSystem.TryAddItem(selectedItem, selectedAmount))
            {
                ShowText($"{selectedItem.DisplayName} x{selectedAmount}\n" +
                    MiningLocalization.Text("Inventory full: click chest again", "Túi đầy: bấm lại rương để nhận"), true);
                return false;
            }
            rewardPending = false;
            ShowText($"+{selectedAmount} {selectedItem.DisplayName}", true);
            if (!opening) StartCoroutine(ReleaseAfterDisplay());
            return true;
        }

        private IEnumerator ReleaseAfterDisplay()
        {
            if (rewardDisplaySeconds > 0f) yield return new WaitForSeconds(rewardDisplaySeconds);
            yield return FadeAndRelease();
        }

        private IEnumerator FadeAndRelease()
        {
            if (fadeOutSeconds > 0f)
            {
                PrepareFadeMaterials();
                fadeTextColor = rewardText != null ? rewardText.color : Color.white;
                fadeIconColor = lidRewardIcon != null ? lidRewardIcon.color : Color.white;
                float elapsed = 0f;
                while (elapsed < fadeOutSeconds)
                {
                    elapsed += Time.deltaTime;
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.Clamp01(elapsed / fadeOutSeconds));
                    SetFadeAlpha(alpha);
                    if (rewardText != null)
                    {
                        Color color = fadeTextColor;
                        color.a *= alpha;
                        rewardText.color = color;
                    }
                    if (lidRewardIcon != null)
                    {
                        Color color = fadeIconColor;
                        color.a *= alpha;
                        lidRewardIcon.color = color;
                    }
                    yield return null;
                }
            }
            if (release != null) release(this);
            else Destroy(gameObject);
        }

        private void PrepareFadeMaterials()
        {
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.GetComponent<TMP_Text>() != null ||
                    renderer.GetComponentInParent<MicroBar>() != null) continue;
                Material[] originals = renderer.sharedMaterials;
                var copies = new Material[originals.Length];
                var colors = new Color[originals.Length];
                for (int index = 0; index < originals.Length; index++)
                {
                    if (originals[index] == null) continue;
                    Material copy = new Material(originals[index]);
                    if (copy.HasProperty("_Surface"))
                    {
                        copy.SetFloat("_Surface", 1f);
                        copy.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                        copy.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                        copy.SetFloat("_ZWrite", 0f);
                        copy.SetOverrideTag("RenderType", "Transparent");
                        copy.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        copy.renderQueue = (int)RenderQueue.Transparent;
                    }
                    colors[index] = copy.HasProperty("_BaseColor")
                        ? copy.GetColor("_BaseColor")
                        : copy.HasProperty("_Color") ? copy.GetColor("_Color") : Color.white;
                    copies[index] = copy;
                }
                renderer.sharedMaterials = copies;
                fadeMaterials.Add((renderer, originals, copies, colors));
            }
        }

        private void SetFadeAlpha(float alpha)
        {
            foreach (var entry in fadeMaterials)
            {
                for (int index = 0; index < entry.copies.Length; index++)
                {
                    Material material = entry.copies[index];
                    if (material == null) continue;
                    Color color = entry.colors[index];
                    color.a *= alpha;
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                }
            }
        }

        private void ResetFadeVisuals()
        {
            bool faded = fadeMaterials.Count > 0;
            foreach (var entry in fadeMaterials)
            {
                if (entry.renderer != null) entry.renderer.sharedMaterials = entry.originals;
                foreach (Material material in entry.copies)
                    if (material != null) Destroy(material);
            }
            fadeMaterials.Clear();
            if (faded && rewardText != null) rewardText.color = fadeTextColor;
            if (faded && lidRewardIcon != null) lidRewardIcon.color = fadeIconColor;
        }

        private void DisplayIcon(MiningItemData item)
        {
            if (lidRewardIcon == null) return;
            lidRewardIcon.sprite = item != null ? item.InventoryIcon : null;
            lidRewardIcon.gameObject.SetActive(lidRewardIcon.sprite != null);
        }

        private void ShowText(string value, bool final)
        {
            if (rewardText == null) return;
            rewardText.gameObject.SetActive(true);
            rewardText.text = value;
            rewardText.color = final ? new Color(1f, .82f, .28f) : Color.white;
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            Vector3 top = GetChestTop();
            Vector3 barPosition = top + overheadOffset;
            if (healthBar != null && healthBar.gameObject.activeSelf)
            {
                Transform bar = healthBar.transform;
                // World-space SpriteRenderers must be in front of the chest mesh to
                // keep both the fill and its TMP hit counter readable.
                Vector3 towardCamera = camera != null
                    ? (camera.transform.position - barPosition).normalized : transform.forward;
                float frontClearance = chestBodyRenderer != null
                    ? chestBodyRenderer.bounds.extents.magnitude + .1f : .45f;
                bar.position = barPosition + Vector3.up * .35f + towardCamera * frontClearance;
                SetWorldScale(bar, healthBarWorldScale);
                if (camera != null) bar.rotation = Quaternion.LookRotation(
                    bar.position - camera.transform.position, camera.transform.up);
            }
            // Keep the reward above the closed chest, independent of lid rotation.
            if (lidRewardIcon != null)
            {
                lidRewardIcon.transform.position = barPosition + Vector3.up * rewardHeightAboveBar;
                SetWorldScale(lidRewardIcon.transform, .5f);
            }
            if (rewardText != null)
            {
                rewardText.transform.position = barPosition + Vector3.up *
                    (rewardHeightAboveBar + (lidRewardIcon != null && lidRewardIcon.gameObject.activeSelf ? .55f : .1f));
                SetWorldScale(rewardText.transform, .2f);
            }
            if (camera == null) return;
            if (lidRewardIcon != null && lidRewardIcon.gameObject.activeSelf)
                lidRewardIcon.transform.rotation = Quaternion.LookRotation(
                    lidRewardIcon.transform.position - camera.transform.position, camera.transform.up);
            if (rewardText != null && rewardText.gameObject.activeSelf)
                rewardText.transform.rotation = Quaternion.LookRotation(
                    rewardText.transform.position - camera.transform.position, camera.transform.up);
        }

        private Vector3 GetChestTop()
        {
            // MeshCollider bounds can become empty when mining disables collision.
            // Use visible meshes so a custom MeshCollider cannot move the HUD anchor.
            if (chestBodyRenderer != null)
            {
                Bounds bounds = chestBodyRenderer.bounds;
                if (chestLidRenderer != null && chestLidRenderer.enabled)
                    bounds.Encapsulate(chestLidRenderer.bounds);
                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }
            return hitCollider != null && hitCollider.enabled
                ? new Vector3(hitCollider.bounds.center.x, hitCollider.bounds.max.y, hitCollider.bounds.center.z)
                : lastChestTop;
        }

        private void UpdateHealthText()
        {
            if (healthText != null)
                healthText.text = $"{remainingHits} / {Mathf.Max(1, hitsToBreak)}";
        }

        private static void SetWorldScale(Transform target, float scale)
        {
            Vector3 parentScale = target.parent != null ? target.parent.lossyScale : Vector3.one;
            target.localScale = new Vector3(
                scale / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
                scale / Mathf.Max(.001f, Mathf.Abs(parentScale.y)),
                scale / Mathf.Max(.001f, Mathf.Abs(parentScale.z)));
        }

        private void PlayRollTick()
        {
            if (Time.unscaledTime < nextRollSfxTime) return;
            nextRollSfxTime = Time.unscaledTime + .11f;
            PlayChestSfx(itemRollTickSfx);
        }

        private void PlayChestSfx(AudioClip clip)
        {
            if (clip == null || audioManager == null || audioManager.AudioData == null ||
                audioManager.SfxMuted) return;
            if (sharedChestAudioSource == null)
            {
                var channel = new GameObject("Chest SFX Channel");
                channel.transform.SetParent(audioManager.transform, false);
                sharedChestAudioSource = channel.AddComponent<AudioSource>();
                sharedChestAudioSource.playOnAwake = false;
                sharedChestAudioSource.spatialBlend = 0f;
            }
            sharedChestAudioSource.Stop();
            sharedChestAudioSource.outputAudioMixerGroup = audioManager.AudioData.SfxMixerGroup;
            sharedChestAudioSource.volume = audioManager.AudioData.SfxVolume *
                audioManager.MasterVolume * audioManager.SfxVolume;
            sharedChestAudioSource.pitch = UnityEngine.Random.Range(
                audioManager.AudioData.MinimumPitch, audioManager.AudioData.MaximumPitch);
            sharedChestAudioSource.clip = clip;
            sharedChestAudioSource.Play();
        }

        private void OnDisable()
        {
            ActiveChests.Remove(this);
            reservedMiners.Clear();
            StopAllCoroutines();
            ResetFadeVisuals();
            opening = rewardPending = false;
        }

        private void OnValidate()
        {
            hitsToBreak = Mathf.Max(1, hitsToBreak);
            npcMiningPowerRequired = Mathf.Max(1, npcMiningPowerRequired);
            maximumMiningNpcs = Mathf.Max(1, maximumMiningNpcs);
            npcDamageMultiplier = Mathf.Max(.01f, npcDamageMultiplier);
            npcMaxVerticalTargetDistance = Mathf.Max(0f, npcMaxVerticalTargetDistance);
            fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);
            minimumMoneyPercent = Mathf.Clamp(minimumMoneyPercent, 0f, 100f);
            maximumMoneyPercent = Mathf.Clamp(maximumMoneyPercent, 0f, 100f);
        }
    }
}
