using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Middle-clicks an ore or Lucky Block to make every compatible NPC prioritize it.
    /// The marker is scene-authored so the project can use its own icon and styling.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningNpcTargetCommand : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private MiningGameData gameData;

        [Header("Target Marker")]
        [Tooltip("Assign your own project icon here. The setup menu never replaces it.")]
        [SerializeField] private Sprite markerIcon;
        [SerializeField] private SpriteRenderer markerRenderer;
        [SerializeField] private Color markerColor = Color.white;
        [SerializeField, Min(0.01f)] private float markerWorldHeight = 0.8f;
        [SerializeField] private Vector3 markerOffset = new(0f, 0.55f, 0f);
        [SerializeField, Min(0f)] private float bobHeight = 0.08f;
        [SerializeField, Min(0f)] private float bobSpeed = 3f;

        private readonly RaycastHit[] raycastHits = new RaycastHit[32];
        private Ore selectedOre;
        private LuckyBlock selectedLuckyBlock;

        private void Awake()
        {
            ResolveCamera();
            ApplyMarkerStyle();
            SetMarkerVisible(false);
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.middleButton.wasPressedThisFrame)
            {
                return;
            }

            ResolveCamera();
            if (targetCamera == null || gameData == null ||
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TryCommandTarget(mouse.position.ReadValue());
        }

        private void LateUpdate()
        {
            if (!TryGetSelectedTopPosition(out Vector3 topPosition))
            {
                ClearSelection();
                return;
            }

            ApplyMarkerStyle();
            if (markerRenderer == null || markerIcon == null)
            {
                SetMarkerVisible(false);
                return;
            }

            float bob = bobHeight > 0f
                ? Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight
                : 0f;
            Transform markerTransform = markerRenderer.transform;
            markerTransform.position = topPosition + markerOffset + Vector3.up * bob;
            if (targetCamera != null)
            {
                markerTransform.rotation = targetCamera.transform.rotation;
            }

            SetMarkerVisible(true);
        }

        public void ConfigureIfMissing(MiningGameData targetGameData,
            SpriteRenderer targetMarkerRenderer)
        {
            gameData ??= targetGameData;
            markerRenderer ??= targetMarkerRenderer;
            ResolveCamera();
            ApplyMarkerStyle();
            SetMarkerVisible(false);
        }

        private void TryCommandTarget(Vector2 screenPosition)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            int hitCount = Physics.RaycastNonAlloc(ray, raycastHits,
                gameData.ClickMaximumDistance, gameData.ClickableLayers,
                QueryTriggerInteraction.Ignore);

            Ore nearestOre = null;
            LuckyBlock nearestBlock = null;
            float nearestDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = raycastHits[index];
                if (hit.collider == null || hit.distance >= nearestDistance)
                {
                    continue;
                }

                LuckyBlock block = hit.collider.GetComponentInParent<LuckyBlock>();
                if (block != null && block.isActiveAndEnabled && !block.IsResolved)
                {
                    nearestBlock = block;
                    nearestOre = null;
                    nearestDistance = hit.distance;
                    continue;
                }

                Ore ore = hit.collider.GetComponentInParent<Ore>();
                if (ore != null && ore.isActiveAndEnabled && !ore.IsDepleted)
                {
                    nearestOre = ore;
                    nearestBlock = null;
                    nearestDistance = hit.distance;
                }
            }

            if (nearestBlock != null)
            {
                CommandAllNpcs(nearestBlock);
                selectedLuckyBlock = nearestBlock;
                selectedOre = null;
            }
            else if (nearestOre != null)
            {
                CommandAllNpcs(nearestOre);
                selectedOre = nearestOre;
                selectedLuckyBlock = null;
            }
        }

        private static void CommandAllNpcs(Ore ore)
        {
            MiningNpc[] npcs = FindObjectsByType<MiningNpc>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (MiningNpc npc in npcs)
            {
                npc?.CommandMine(ore);
            }
        }

        private static void CommandAllNpcs(LuckyBlock block)
        {
            MiningNpc[] npcs = FindObjectsByType<MiningNpc>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (MiningNpc npc in npcs)
            {
                npc?.CommandMine(block);
            }
        }

        private bool TryGetSelectedTopPosition(out Vector3 position)
        {
            if (selectedLuckyBlock != null && selectedLuckyBlock.isActiveAndEnabled &&
                !selectedLuckyBlock.IsResolved)
            {
                position = selectedLuckyBlock.GetWorldTopCenter();
                return true;
            }

            if (selectedOre != null && selectedOre.isActiveAndEnabled && !selectedOre.IsDepleted)
            {
                position = selectedOre.GetWorldTopCenter();
                return true;
            }

            position = default;
            return false;
        }

        private void ClearSelection()
        {
            selectedOre = null;
            selectedLuckyBlock = null;
            SetMarkerVisible(false);
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void ApplyMarkerStyle()
        {
            if (markerRenderer == null)
            {
                return;
            }

            markerRenderer.sprite = markerIcon;
            markerRenderer.color = markerColor;
            if (markerIcon != null && markerIcon.bounds.size.y > Mathf.Epsilon)
            {
                float scale = markerWorldHeight / markerIcon.bounds.size.y;
                markerRenderer.transform.localScale = Vector3.one * scale;
            }
        }

        private void SetMarkerVisible(bool visible)
        {
            if (markerRenderer != null)
            {
                markerRenderer.enabled = visible;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            markerWorldHeight = Mathf.Max(0.01f, markerWorldHeight);
            bobHeight = Mathf.Max(0f, bobHeight);
            bobSpeed = Mathf.Max(0f, bobSpeed);
            ApplyMarkerStyle();
            if (!Application.isPlaying)
            {
                SetMarkerVisible(markerIcon != null);
            }
        }
#endif
    }
}
