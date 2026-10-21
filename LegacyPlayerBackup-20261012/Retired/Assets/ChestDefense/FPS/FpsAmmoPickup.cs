using MiningSimulator.Ores;
using UnityEngine;

namespace ChestDefense.Fps
{
    // Attach to an authored ammo pickup with a non-trigger collider; uses the existing interaction prompt/key.
    [DisallowMultipleComponent]
    public sealed class FpsAmmoPickup : MonoBehaviour, IMiningInteractable
    {
        public FpsWeaponData weapon;
        public int amount = 30;
        public FpsWeaponController player;
        public GameObject focusVisual;
        public AudioClip pickupSound;
        MiningAudioManager audioOwner;
        public string InteractionLabel => string.Format(MiningLocalization.TextKey("FPS_AMMO_PICKUP", "{0} ammo +{1}"),
            weapon != null ? weapon.LocalizedName : "", amount);
        public bool CanInteract => isActiveAndEnabled && player != null && weapon != null && amount > 0 && player.AmmoSpace(weapon.weaponId) > 0;
        void Awake()
        {
            if (player == null) player = FindFirstObjectByType<FpsWeaponController>();
            audioOwner = FindFirstObjectByType<MiningAudioManager>();
            if (focusVisual != null) focusVisual.SetActive(false);
        }
        public void SetInteractionFocused(bool focused) { if (focusVisual != null) focusVisual.SetActive(focused); }
        public void Interact()
        {
            if (!CanInteract) return;
            int accepted = player.AddAmmo(weapon.weaponId, amount);
            if (accepted <= 0) return; // A full bag never consumes the pickup.
            amount -= accepted;
            if (pickupSound != null) audioOwner?.PlayWorldSfx(pickupSound, transform.position);
            if (amount == 0) { foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false; Destroy(gameObject); }
        }
    }
}
