using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    /// <summary>Finds the world object under the pointer and executes it with the configured key.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningInteractionSystem : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private MiningInteractionData data;
        [SerializeField] private MiningInteractionPrompt prompt;

        private IMiningInteractable focusedTarget;
        private string focusedPromptText;
        private bool promptTextDirty = true;

        public MiningInteractionData Data => data;
        public MiningInteractionPrompt Prompt => prompt;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
            prompt?.HideImmediate();
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            MiningLocalization.LanguageChanged += HandleLanguageChanged;
            promptTextDirty = true;
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            ClearFocus();
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || targetCamera == null || data == null || prompt == null ||
                IsPointerOverUi())
            {
                ClearFocus();
                return;
            }

            Vector2 pointerPosition = pointer.position.ReadValue();
            Ray ray = targetCamera.ScreenPointToRay(pointerPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, data.MaximumDistance,
                    data.InteractionLayers, QueryTriggerInteraction.Ignore))
            {
                ClearFocus();
                return;
            }

            IMiningInteractable target = hit.collider.GetComponentInParent<IMiningInteractable>();
            if (target == null || !target.CanInteract)
            {
                ClearFocus();
                return;
            }

            SetFocus(target);
            if (promptTextDirty)
            {
                focusedPromptText = data.GetPrompt(target.InteractionLabel);
                promptTextDirty = false;
            }
            prompt.Show(focusedPromptText, pointerPosition, data.CursorOffset);

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[data.InteractionKey].wasPressedThisFrame)
            {
                target.Interact();
            }
        }

        public void ConfigureIfMissing(MiningInteractionData interactionData,
            MiningInteractionPrompt interactionPrompt)
        {
            if (data == null)
            {
                data = interactionData;
            }
            if (prompt == null)
            {
                prompt = interactionPrompt;
            }
        }

        private void SetFocus(IMiningInteractable target)
        {
            if (ReferenceEquals(focusedTarget, target))
            {
                return;
            }

            if (IsAlive(focusedTarget))
            {
                focusedTarget.SetInteractionFocused(false);
            }
            focusedTarget = target;
            focusedTarget.SetInteractionFocused(true);
            promptTextDirty = true;
        }

        private void ClearFocus()
        {
            if (IsAlive(focusedTarget))
            {
                focusedTarget.SetInteractionFocused(false);
            }
            focusedTarget = null;
            focusedPromptText = null;
            promptTextDirty = true;
            prompt?.HideImmediate();
        }

        private void HandleLanguageChanged()
        {
            promptTextDirty = true;
        }

        private static bool IsAlive(IMiningInteractable target)
        {
            return target != null && (!(target is Object unityObject) || unityObject != null);
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
