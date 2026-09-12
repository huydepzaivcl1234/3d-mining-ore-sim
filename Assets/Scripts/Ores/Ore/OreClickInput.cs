using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    /// <summary>Converts a mouse or touch press into one mining hit through a physics raycast.</summary>
    [DisallowMultipleComponent]
    public sealed class OreClickInput : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private MiningGameData gameData;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame || targetCamera == null || gameData == null)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = targetCamera.ScreenPointToRay(pointer.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, gameData.ClickMaximumDistance,
                gameData.ClickableLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            LuckyBlock luckyBlock = hit.collider.GetComponentInParent<LuckyBlock>();
            if (luckyBlock != null)
            {
                luckyBlock.MineOnce();
                return;
            }

            MiningPortalGate portalGate = hit.collider.GetComponentInParent<MiningPortalGate>();
            if (portalGate != null)
            {
                portalGate.Interact();
                return;
            }

            Ore ore = hit.collider.GetComponentInParent<Ore>();
            ore?.MineOnce();
        }
    }
}
