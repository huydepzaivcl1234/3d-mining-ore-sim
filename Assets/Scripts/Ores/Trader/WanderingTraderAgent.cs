using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    /// <summary>World interaction and low-cost wandering movement for the trader's humanoid.</summary>
    [DisallowMultipleComponent]
    public sealed class WanderingTraderAgent : MonoBehaviour, IMiningInteractable
    {
        private WanderingTraderSystem owner;
        private Vector3 destination;
        private float nextDestinationTime;
        private bool hasDestination;
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
            ChooseDestination();
        }

        private void Update()
        {
            HandlePointerInteraction();

            if (owner == null || isTrading)
            {
                owner?.ApplyMovementAnimation(animator, false);
                return;
            }

            if (!owner.IsOutsideMiningArea(transform.position))
            {
                transform.position = owner.MoveOutsideMiningArea(transform.position);
                hasDestination = false;
                ChooseDestination();
            }

            if (!hasDestination || Time.time >= nextDestinationTime)
            {
                ChooseDestination();
            }
            if (!hasDestination)
            {
                owner.ApplyMovementAnimation(animator, false);
                return;
            }

            Vector3 offset = destination - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= 0.04f)
            {
                hasDestination = false;
                nextDestinationTime = Time.time + UnityEngine.Random.Range(owner.WaitSeconds.x,
                    owner.WaitSeconds.y);
                owner.ApplyMovementAnimation(animator, false);
                return;
            }

            Vector3 direction = offset.normalized;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up), 360f * Time.deltaTime);
            owner.ApplyMovementAnimation(animator, true);
        }

        private void FixedUpdate()
        {
            if (owner == null || isTrading || !hasDestination)
            {
                return;
            }

            Vector3 currentPosition = body != null ? body.position : transform.position;
            Vector3 offset = destination - currentPosition;
            offset.y = 0f;
            if (offset.sqrMagnitude <= 0.04f)
            {
                return;
            }

            Vector3 nextPosition = currentPosition + offset.normalized * owner.WanderSpeed *
                Time.fixedDeltaTime;
            if (!owner.IsOutsideMiningArea(nextPosition))
            {
                hasDestination = false;
                nextDestinationTime = Time.time;
                owner.ApplyMovementAnimation(animator, false);
                return;
            }

            if (body != null && body.isKinematic)
            {
                body.MovePosition(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }
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
            if (!trading)
            {
                nextDestinationTime = Time.time + 0.5f;
            }
        }

        private void ChooseDestination()
        {
            hasDestination = owner != null && owner.TryGetDestination(transform.position,
                out destination);
            nextDestinationTime = Time.time + 0.25f;
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
