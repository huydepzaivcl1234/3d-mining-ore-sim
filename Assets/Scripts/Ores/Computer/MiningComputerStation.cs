using System;
using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Purchaseable Computer 1 station. Before purchase it renders as a ghost and uses
    /// the shared hover/F interaction. After purchase it produces multiplied coin ticks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningComputerStation : MonoBehaviour, IMiningInteractable
    {
        [Header("Identity And Data")]
        [SerializeField] private string stationId = "Computer1";
        [SerializeField] private MiningComputerData data;

        [Header("Existing Project References")]
        [SerializeField] private OreRewardPopup rewardPopupPrefab;
        [SerializeField] private MiningUiData uiData;

        private PlayerWallet wallet;
        private MiningUpgradeSystem upgradeSystem;
        private MiningComputerPanel infoPanel;
        private Renderer[] renderers;
        private Material[][] originalMaterials;
        private Material ghostMaterial;
        private Transform[] assemblyParts;
        private Vector3[] assemblyLocalPositions;
        private Vector3[] assemblyLocalScales;
        private Coroutine productionRoutine;
        private Sequence assemblySequence;
        private Sequence punchSequence;
        private Vector3 restingLocalPosition;
        private Vector3 restingLocalScale;
        private bool purchased;
        private bool initialized;
        private int currentLevel = 1;

        public bool IsPurchased => purchased;
        public int CurrentLevel => Mathf.Clamp(currentLevel, 1, MaximumLevel);
        public int MaximumLevel => data != null ? data.MaximumLevel : 1;
        public int CurrentBaseCoinsPerTick => data != null
            ? data.GetBaseCoinsPerTick(CurrentLevel)
            : 0;
        public int NextBaseCoinsPerTick => data != null
            ? data.GetBaseCoinsPerTick(Mathf.Min(CurrentLevel + 1, MaximumLevel))
            : 0;
        public float CurrentRewardPerTick => CalculateReward(CurrentBaseCoinsPerTick);
        public float NextRewardPerTick => CalculateReward(NextBaseCoinsPerTick);
        public float SecondsPerTick => data != null ? data.SecondsPerTick : 0f;
        public float UpgradeCost => data != null ? data.GetUpgradeCost(CurrentLevel) : float.MaxValue;
        public bool IsMaximumLevel => CurrentLevel >= MaximumLevel;
        public bool CanUpgrade => purchased && !IsMaximumLevel && wallet != null &&
                                  wallet.CurrentMoney >= UpgradeCost;
        public string InteractionLabel => purchased
            ? MiningLocalization.Text("View computer info", "Xem thông tin máy")
            : string.Format(MiningLocalization.Text(
                    "Buy computer ({0})", "Mua máy tính ({0})"),
                MiningMoneyFormatter.Format(PurchaseCost));
        public bool CanInteract => isActiveAndEnabled && data != null;
        public event Action StateChanged;
        private int PurchaseCost => data != null ? data.PurchaseCost : 0;

        public void ConfigureIfMissing(MiningComputerData computerData,
            OreRewardPopup popupPrefab, MiningUiData miningUiData)
        {
            if (data == null)
            {
                data = computerData;
            }
            if (rewardPopupPrefab == null)
            {
                rewardPopupPrefab = popupPrefab;
            }
            if (uiData == null)
            {
                uiData = miningUiData;
            }
        }

        private void Awake()
        {
            InitializeIfNeeded();
            ResolveRuntimeReferences();
            purchased = LoadPurchased();
            currentLevel = purchased ? LoadLevel() : 1;
            if (purchased)
            {
                RestoreSolidMaterials();
            }
            else
            {
                ApplyGhostMaterial(data != null ? data.GhostColor : new Color(0.1f, 0.85f, 1f, 0.28f));
            }
        }

        private void OnEnable()
        {
            if (purchased)
            {
                StartProduction();
            }
        }

        private void OnDisable()
        {
            StopProduction();
            StopAnimationsAndRestore();
        }

        private void OnDestroy()
        {
            if (ghostMaterial != null)
            {
                Destroy(ghostMaterial);
            }
        }

        public void Interact()
        {
            if (purchased)
            {
                OpenInfoPanel();
            }
            else
            {
                TryPurchase();
            }
        }

        public bool TryPurchase()
        {
            if (!CanInteract)
            {
                return false;
            }

            ResolveRuntimeReferences();
            if (wallet == null || !wallet.TrySpend(PurchaseCost))
            {
                return false;
            }

            purchased = true;
            currentLevel = 1;
            SavePurchased();
            SaveLevel();
            RestoreSolidMaterials();
            PlayAssemblyAnimation();
            StateChanged?.Invoke();
            return true;
        }

        public bool TryUpgrade()
        {
            ResolveRuntimeReferences();
            if (!CanUpgrade || !wallet.TrySpend(UpgradeCost))
            {
                return false;
            }

            currentLevel = Mathf.Min(currentLevel + 1, MaximumLevel);
            SaveLevel();
            PlayCoinPunch();
            StateChanged?.Invoke();
            return true;
        }

        public void SetInteractionFocused(bool focused)
        {
            if (!purchased && data != null)
            {
                ApplyGhostMaterial(focused ? data.FocusedGhostColor : data.GhostColor);
            }
        }

        public void ResetAllData()
        {
            InitializeIfNeeded();
            StopProduction();
            StopAnimationsAndRestore();
            purchased = false;
            currentLevel = 1;
            if (data != null)
            {
                PlayerPrefs.DeleteKey(GetSaveKey());
                PlayerPrefs.DeleteKey(GetLevelSaveKey());
                PlayerPrefs.Save();
                ApplyGhostMaterial(data.GhostColor);
            }
            StateChanged?.Invoke();
        }

        public static void ResetAllLoadedStations()
        {
            MiningComputerStation[] stations = FindObjectsByType<MiningComputerStation>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (MiningComputerStation station in stations)
            {
                station?.ResetAllData();
            }
        }

        private void ResolveRuntimeReferences()
        {
            if (wallet == null)
            {
                wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            }
            if (upgradeSystem == null)
            {
                upgradeSystem = FindFirstObjectByType<MiningUpgradeSystem>(FindObjectsInactive.Include);
            }
        }

        private void OpenInfoPanel()
        {
            ResolveRuntimeReferences();
            if (infoPanel == null)
            {
                infoPanel = FindFirstObjectByType<MiningComputerPanel>(
                    FindObjectsInactive.Include);
            }
            if (infoPanel == null)
            {
                Debug.LogWarning("Computer info panel is missing. Run Mining Simulator/" +
                                 "Setup/Create or Update Computer Info Panel in Edit Mode.", this);
                return;
            }

            infoPanel.Show(this);
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            restingLocalPosition = transform.localPosition;
            restingLocalScale = transform.localScale;
            CacheVisuals();
            initialized = true;
        }

        private void CacheVisuals()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originalMaterials = new Material[renderers.Length][];
            HashSet<Transform> rendererTransforms = new();
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                originalMaterials[index] = renderer.sharedMaterials;
                if (renderer.transform != transform)
                {
                    rendererTransforms.Add(renderer.transform);
                }
            }

            List<Transform> roots = new();
            foreach (Transform candidate in rendererTransforms)
            {
                bool hasAnimatedParent = false;
                Transform parent = candidate.parent;
                while (parent != null && parent != transform)
                {
                    if (rendererTransforms.Contains(parent))
                    {
                        hasAnimatedParent = true;
                        break;
                    }
                    parent = parent.parent;
                }
                if (!hasAnimatedParent)
                {
                    roots.Add(candidate);
                }
            }

            roots.Sort((left, right) => GetRendererCenterY(left).CompareTo(GetRendererCenterY(right)));
            assemblyParts = roots.ToArray();
            assemblyLocalPositions = new Vector3[assemblyParts.Length];
            assemblyLocalScales = new Vector3[assemblyParts.Length];
            for (int index = 0; index < assemblyParts.Length; index++)
            {
                assemblyLocalPositions[index] = assemblyParts[index].localPosition;
                assemblyLocalScales[index] = assemblyParts[index].localScale;
            }
        }

        private float GetRendererCenterY(Transform candidate)
        {
            Renderer candidateRenderer = candidate.GetComponent<Renderer>();
            return candidateRenderer != null ? candidateRenderer.bounds.center.y : candidate.position.y;
        }

        private void ApplyGhostMaterial(Color color)
        {
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            EnsureGhostMaterial();
            if (ghostMaterial == null)
            {
                return;
            }
            SetGhostColor(color);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null)
                {
                    continue;
                }

                int materialCount = originalMaterials[rendererIndex].Length;
                Material[] ghostMaterials = new Material[materialCount];
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    ghostMaterials[materialIndex] = ghostMaterial;
                }
                renderer.sharedMaterials = ghostMaterials;
            }
        }

        private void EnsureGhostMaterial()
        {
            if (ghostMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard");
            if (shader == null && originalMaterials.Length > 0 &&
                originalMaterials[0].Length > 0 && originalMaterials[0][0] != null)
            {
                shader = originalMaterials[0][0].shader;
            }
            if (shader == null)
            {
                return;
            }

            ghostMaterial = new Material(shader)
            {
                name = "Computer Ghost (Runtime)",
                hideFlags = HideFlags.DontSave,
                renderQueue = (int)RenderQueue.Transparent
            };
            ghostMaterial.SetOverrideTag("RenderType", "Transparent");
            if (ghostMaterial.HasProperty("_Surface"))
            {
                ghostMaterial.SetFloat("_Surface", 1f);
            }
            if (ghostMaterial.HasProperty("_Blend"))
            {
                ghostMaterial.SetFloat("_Blend", 0f);
            }
            if (ghostMaterial.HasProperty("_SrcBlend"))
            {
                ghostMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }
            if (ghostMaterial.HasProperty("_DstBlend"))
            {
                ghostMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }
            if (ghostMaterial.HasProperty("_ZWrite"))
            {
                ghostMaterial.SetFloat("_ZWrite", 0f);
            }
            if (ghostMaterial.HasProperty("_AlphaClip"))
            {
                ghostMaterial.SetFloat("_AlphaClip", 0f);
            }
            ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            ghostMaterial.EnableKeyword("_EMISSION");
        }

        private void SetGhostColor(Color color)
        {
            if (ghostMaterial == null)
            {
                return;
            }

            if (ghostMaterial.HasProperty("_BaseColor"))
            {
                ghostMaterial.SetColor("_BaseColor", color);
            }
            if (ghostMaterial.HasProperty("_Color"))
            {
                ghostMaterial.SetColor("_Color", color);
            }
            if (ghostMaterial.HasProperty("_EmissionColor"))
            {
                float intensity = data != null ? data.GhostEmissionIntensity : 1f;
                ghostMaterial.SetColor("_EmissionColor", new Color(color.r, color.g, color.b, 1f) * intensity);
            }
        }

        private void RestoreSolidMaterials()
        {
            if (renderers == null || originalMaterials == null)
            {
                return;
            }
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null)
                {
                    renderers[index].sharedMaterials = originalMaterials[index];
                }
            }
        }

        private void PlayAssemblyAnimation()
        {
            StopAnimationsAndRestore();
            if (data == null || assemblyParts == null || assemblyParts.Length == 0)
            {
                StartProduction();
                return;
            }

            assemblySequence = Sequence.Create();
            float delay = 0f;
            for (int index = 0; index < assemblyParts.Length; index++)
            {
                Transform part = assemblyParts[index];
                if (part == null)
                {
                    continue;
                }

                Vector3 finalPosition = assemblyLocalPositions[index];
                Vector3 finalScale = assemblyLocalScales[index];
                Vector3 localDropOffset = part.parent != null
                    ? part.parent.InverseTransformVector(Vector3.up * data.PartDropHeight)
                    : Vector3.up * data.PartDropHeight;
                part.localPosition = finalPosition + localDropOffset;
                part.localScale = Vector3.zero;
                assemblySequence.Insert(delay, Tween.LocalPosition(part, finalPosition,
                    data.PartDropDuration, Ease.OutBounce));
                assemblySequence.Insert(delay, Tween.Scale(part, finalScale,
                    data.PartDropDuration * 0.7f, Ease.OutBack));
                delay += data.DelayBetweenParts;
            }
            assemblySequence.ChainCallback(this, station => station.HandleAssemblyCompleted());
        }

        private void HandleAssemblyCompleted()
        {
            RestoreAssemblyTransforms();
            StartProduction();
        }

        private void StartProduction()
        {
            StopProduction();
            if (purchased && isActiveAndEnabled && data != null)
            {
                productionRoutine = StartCoroutine(ProduceCoins());
            }
        }

        private void StopProduction()
        {
            if (productionRoutine != null)
            {
                StopCoroutine(productionRoutine);
                productionRoutine = null;
            }
        }

        private IEnumerator ProduceCoins()
        {
            WaitForSeconds wait = new(data.SecondsPerTick);
            while (purchased)
            {
                yield return wait;
                GrantCoinTick();
            }
            productionRoutine = null;
        }

        private void GrantCoinTick()
        {
            ResolveRuntimeReferences();
            if (wallet == null || data == null)
            {
                return;
            }

            float reward = CurrentRewardPerTick;
            wallet.AddMoney(reward);
            SpawnRewardPopup(reward);
            PlayCoinPunch();
        }

        private void SpawnRewardPopup(float reward)
        {
            if (rewardPopupPrefab == null || uiData == null)
            {
                return;
            }

            Bounds bounds = CalculateWorldBounds();
            Vector3 position = new(bounds.center.x, bounds.max.y, bounds.center.z);
            OreRewardPopup popup = Instantiate(rewardPopupPrefab, position, Quaternion.identity);
            popup.Initialize(reward, position, uiData);
        }

        private Bounds CalculateWorldBounds()
        {
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(transform.position, Vector3.one);
            }

            bool initialized = false;
            Bounds bounds = new(transform.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }
                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return initialized ? bounds : new Bounds(transform.position, Vector3.one);
        }

        private void PlayCoinPunch()
        {
            if (data == null)
            {
                return;
            }

            if (punchSequence.isAlive)
            {
                punchSequence.Stop();
            }
            transform.localPosition = restingLocalPosition;
            transform.localScale = restingLocalScale;
            float halfDuration = data.PunchDuration * 0.5f;
            Vector3 squashedScale = new(
                restingLocalScale.x * (1f + data.PunchScaleAmount),
                restingLocalScale.y * (1f - data.PunchScaleAmount),
                restingLocalScale.z * (1f + data.PunchScaleAmount));
            Vector3 pressedPosition = restingLocalPosition + Vector3.down * data.PunchDropAmount;
            punchSequence = Sequence.Create(Tween.Scale(transform, squashedScale,
                    halfDuration, Ease.OutQuad))
                .Group(Tween.LocalPosition(transform, pressedPosition, halfDuration, Ease.OutQuad))
                .Chain(Tween.Scale(transform, restingLocalScale, halfDuration, Ease.OutBack))
                .Group(Tween.LocalPosition(transform, restingLocalPosition, halfDuration, Ease.OutBack));
        }

        private void StopAnimationsAndRestore()
        {
            if (assemblySequence.isAlive)
            {
                assemblySequence.Stop();
            }
            if (punchSequence.isAlive)
            {
                punchSequence.Stop();
            }
            RestoreAssemblyTransforms();
            transform.localPosition = restingLocalPosition;
            transform.localScale = restingLocalScale;
        }

        private void RestoreAssemblyTransforms()
        {
            if (assemblyParts == null)
            {
                return;
            }
            for (int index = 0; index < assemblyParts.Length; index++)
            {
                if (assemblyParts[index] != null)
                {
                    assemblyParts[index].localPosition = assemblyLocalPositions[index];
                    assemblyParts[index].localScale = assemblyLocalScales[index];
                }
            }
        }

        private bool LoadPurchased()
        {
            return data != null && PlayerPrefs.GetInt(GetSaveKey(), 0) == 1;
        }

        private int LoadLevel()
        {
            return data == null
                ? 1
                : Mathf.Clamp(PlayerPrefs.GetInt(GetLevelSaveKey(), 1), 1, MaximumLevel);
        }

        private void SavePurchased()
        {
            if (data == null)
            {
                return;
            }
            PlayerPrefs.SetInt(GetSaveKey(), 1);
            PlayerPrefs.Save();
        }

        private void SaveLevel()
        {
            if (data == null)
            {
                return;
            }
            PlayerPrefs.SetInt(GetLevelSaveKey(), CurrentLevel);
            PlayerPrefs.Save();
        }

        private float CalculateReward(int baseCoins)
        {
            if (baseCoins <= 0)
            {
                return 0f;
            }
            return upgradeSystem != null
                ? upgradeSystem.CalculateMiningReward(baseCoins)
                : baseCoins;
        }

        private string GetSaveKey()
        {
            string safeId = string.IsNullOrWhiteSpace(stationId) ? "Computer1" : stationId.Trim();
            return $"{data.PurchaseSaveKey}.{safeId}";
        }

        private string GetLevelSaveKey()
        {
            return GetSaveKey() + ".Level.v1";
        }
    }
}
