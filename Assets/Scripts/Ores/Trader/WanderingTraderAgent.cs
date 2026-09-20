using UnityEngine;
using UnityEngine.EventSystems;

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

        public string InteractionLabel => MiningLocalization.Text("WANDERING TRADER", "THƯƠNG NHÂN LANG THANG");
        public bool CanInteract => owner != null && !isTrading;

        public void Initialize(WanderingTraderSystem traderSystem)
        {
            owner = traderSystem;
            ChooseDestination();
        }

        private void Update()
        {
            if (owner == null || isTrading)
            {
                return;
            }

            if (!hasDestination || Time.time >= nextDestinationTime)
            {
                ChooseDestination();
            }
            if (!hasDestination)
            {
                return;
            }

            Vector3 offset = destination - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= 0.04f)
            {
                hasDestination = false;
                nextDestinationTime = Time.time + UnityEngine.Random.Range(owner.WaitSeconds.x,
                    owner.WaitSeconds.y);
                return;
            }

            Vector3 direction = offset.normalized;
            transform.position += direction * owner.WanderSpeed * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up), 360f * Time.deltaTime);
        }

        private void OnMouseDown()
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
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
    }
}
