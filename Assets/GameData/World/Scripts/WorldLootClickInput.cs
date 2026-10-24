using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    /// <summary>Handles player clicks on retained chests and Lucky Blocks.</summary>
    [DisallowMultipleComponent]
    public sealed class WorldLootClickInput : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private MiningGameData gameData;
        private MiningItemSystem itemSystem;

        private void Awake()
        {
            itemSystem = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
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

            itemSystem ??= FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);

            Ray ray = targetCamera.ScreenPointToRay(pointer.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, gameData.ClickMaximumDistance,
                gameData.ClickableLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            MiningChest chest = hit.collider.GetComponentInParent<MiningChest>();
            if (chest != null)
            {
                chest.MineOnce();
                return;
            }

            LuckyBlock luckyBlock = hit.collider.GetComponentInParent<LuckyBlock>();
            if (luckyBlock != null)
            {
                int baseDamage = luckyBlock.Variant != null ? luckyBlock.Variant.ClickDamage : 1;
                luckyBlock.ApplyPlayerDamage(itemSystem != null
                    ? itemSystem.RollOreLuckyDamage(baseDamage) : baseDamage);
                return;
            }

        }
    }
}
