using System.Collections.Generic;
using UnityEngine;

namespace GameData.Player.Script
{
    /// <summary>Authoring preparation only. Does not switch the active gun or replace its Animator.</summary>
    [CreateAssetMenu(menuName = "Chest Defense/Weapons/Setup Profile")]
    public sealed class WeaponSetupProfile : ScriptableObject
    {
        public GameObject sourceModel;
        public PistolWeaponData weaponData;
        [Header("Source clips - not automatically applied to the player")]
        public AnimationClip sourceIdle, sourceFire, sourceReload;
        [Tooltip("Generic source arms need an explicit target rig/clip conversion; a model swap is insufficient.")]
        public bool separateGenericArmRig;
        [Header("Prepared player presentation")]
        public GameObject gunOnlyPrefab;
        public PistolHandPose playerHandPose;
        public string animatorLayer = "Rifle Presentation";
        public string aimPointPath, muzzlePath, leftHandTargetPath;
        public string magazinePath, boltPath;
        public Vector3 boltBackward = Vector3.back;
        [TextArea(3, 8)] public string preparationNotes;

        public string[] GetSetupIssues()
        {
            var issues = new List<string>();
            if (!weaponData) issues.Add("Missing weapon data.");
            if (!gunOnlyPrefab) issues.Add("Gun-only prefab not prepared; source model contains a separate arm rig.");
            if (!playerHandPose || !playerHandPose.valid) issues.Add("Player Humanoid rifle grip is not authored.");
            if (separateGenericArmRig) issues.Add("Generic source hand animation must be converted to player hand targets or replaced with compatible clips.");
            if (gunOnlyPrefab) foreach (var marker in new[] { aimPointPath, muzzlePath, leftHandTargetPath })
                if (string.IsNullOrWhiteSpace(marker) || !gunOnlyPrefab.transform.Find(marker)) issues.Add("Missing or uncalibrated sight/muzzle/support-hand marker: " + marker);
            return issues.ToArray();
        }
    }
}
