using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
        [Min(1), SerializeField] private int hitsToBreak = 5;
        [SerializeField] private Vector3 lidOpenEuler = new Vector3(-95f, 0f, 0f);
        [Min(0.01f), SerializeField] private float lockBreakSeconds = .2f;
        [Min(0.01f), SerializeField] private float lidOpenSeconds = .55f;
        [Min(0f), SerializeField] private float rewardDisplaySeconds = 3f;

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
        private bool capturedModelState;
        private int remainingHits;
        private bool opening;
        private bool rewardPending;
        private MiningItemData selectedItem;
        private int selectedAmount;

        public ChestKind Kind => kind;
        public bool CanMine => isActiveAndEnabled && !opening && !rewardPending && remainingHits > 0;
        public int RemainingHits => remainingHits;

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
            closedRotation = lidHinge != null ? lidHinge.localRotation : Quaternion.identity;
            initialLockScale = lockModel != null ? lockModel.localScale : Vector3.one;
            initialLockPosition = lockModel != null ? lockModel.localPosition : Vector3.zero;
            initialLockRotation = lockModel != null ? lockModel.localRotation : Quaternion.identity;
            capturedModelState = true;
        }

        public void Initialize(PlayerWallet targetWallet, MiningItemSystem targetItemSystem,
            Action<MiningChest> returnToSpawner)
        {
            CaptureModelState();
            StopAllCoroutines();
            wallet = targetWallet;
            itemSystem = targetItemSystem;
            release = returnToSpawner;
            remainingHits = Mathf.Max(1, hitsToBreak);
            opening = rewardPending = false;
            selectedItem = null;
            selectedAmount = 0;
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
            if (hitCollider != null) hitCollider.enabled = true;
        }

        public bool MineOnce()
        {
            if (rewardPending) return ClaimPendingItem();
            if (!CanMine) return false;
            if (--remainingHits <= 0)
            {
                opening = true;
                StartCoroutine(OpenAndReward());
            }
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
                    float t = Mathf.Clamp01(elapsed / lockBreakSeconds);
                    lockModel.localScale = Vector3.Lerp(start, start * .65f, t);
                    lockModel.localPosition = startPosition +
                        (Vector3.down * .85f + Vector3.forward * .2f) * (t * t);
                    lockModel.localRotation = initialLockRotation * Quaternion.Euler(0f, 0f, 110f * t);
                    yield return null;
                }
                lockModel.gameObject.SetActive(false);
            }

            if (lidHinge != null)
            {
                float elapsed = 0f;
                Quaternion open = closedRotation * Quaternion.Euler(lidOpenEuler);
                while (elapsed < lidOpenSeconds)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / lidOpenSeconds));
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
                ShowText($"+{MiningMoneyFormatter.Format(amount)}", true);
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
                release?.Invoke(this);
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
            release?.Invoke(this);
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
            if (camera == null) return;
            if (lidRewardIcon != null && lidRewardIcon.gameObject.activeSelf)
                lidRewardIcon.transform.rotation = Quaternion.LookRotation(
                    lidRewardIcon.transform.position - camera.transform.position, camera.transform.up);
            if (rewardText != null && rewardText.gameObject.activeSelf)
                rewardText.transform.rotation = Quaternion.LookRotation(
                    rewardText.transform.position - camera.transform.position, camera.transform.up);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            opening = rewardPending = false;
        }

        private void OnValidate()
        {
            hitsToBreak = Mathf.Max(1, hitsToBreak);
            minimumMoneyPercent = Mathf.Clamp(minimumMoneyPercent, 0f, 100f);
            maximumMoneyPercent = Mathf.Clamp(maximumMoneyPercent, 0f, 100f);
        }
    }
}
