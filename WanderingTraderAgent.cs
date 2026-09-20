using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    /// <summary>World interaction for the stationary trader's humanoid.</summary>
    [DisallowMultipleComponent]
    public sealed class WanderingTraderAgent : MonoBehaviour, IMiningInteractable
    {
        private WanderingTraderSystem owner;
        private bool isTrading;
        private Animator animator;
        private Rigidbody body;

        public string InteractionLabel => MiningLocalization.Text("WANDERING TRADER", "THƯƠNG NHÂN LANG THANG");
        public bool CanInteract => owner != null && !isTrading;

        public void Initialize(WanderingTraderSystem traderSystem)
        {
            owner = traderSystem;
            animator = GetComponentInChildren<Animator>(true);
            body = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            HandlePointerInteraction();
            owner?.ApplyMovementAnimation(animator, false);
        }

        private void FixedUpdate()
        {
            // The merchant is intentionally stationary. This prevents route jitter and makes
            // the interaction target reliable while an offer is being viewed.
        }

        private void OnMouseDown()
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                Interact();
            }
        }

        private void HandlePointerInteraction()
        {
            if (!CanInteract || IsPointerOverUi())
            {
                return;
            }

            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool pressedF = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
            if ((clicked || pressedF) && IsPointerOverThisTrader())
            {
                Interact();
            }
        }

        private void OnDisable()
        {
            owner?.HandleTradeClosed(this);
        }

        public void Interact()
        {
            if (CanInteract && owner.TryOpenTrade(this))
            {
                isTrading = true;
            }
        }

        public void SetInteractionFocused(bool focused)
        {
            // The existing prompt provides the visual focus indication.
        }

        public void SetTrading(bool trading)
        {
            isTrading = trading;
            owner?.ApplyMovementAnimation(animator, false);
        }

        private bool IsPointerOverThisTrader()
        {
            Camera camera = Camera.main;
            Pointer pointer = Pointer.current;
            if (camera == null || pointer == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(pointer.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return hit.collider.GetComponentInParent<WanderingTraderAgent>() == this;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
