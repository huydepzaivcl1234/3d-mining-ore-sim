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
        [SerializeField] private LayerMask clickableLayers = ~0;
        [Min(0.1f), SerializeField] private float maximumDistance = 500f;

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
            if (pointer == null || !pointer.press.wasPressedThisFrame || targetCamera == null)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = targetCamera.ScreenPointToRay(pointer.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, maximumDistance,
                clickableLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            Ore ore = hit.collider.GetComponentInParent<Ore>();
            ore?.MineOnce();
        }

        private void OnValidate()
        {
            maximumDistance = Mathf.Max(0.1f, maximumDistance);
        }
    }
}
