using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName = "Chest Defense/Weapons/Pistol Reload", fileName = "PistolReloadProfile")]
    public sealed class PistolReloadProfile : ScriptableObject
    {
        public AnimationClip sourceClip;
        [Tooltip("Retargeted clip animating WeaponOffset, MagazineOffset and Control on the reload target rig.")]
        public AnimationClip targetClip;
        public string magazinePath = "Magazine_Location/M1911 Magazine_Black";
        public string slidePath = "M1911 Handgun_Model/Slider";
        [Tooltip("Weapon-local offset from magazine mesh center to the finger grasp point; tune the grip without editing your idle pose.")]
        public Vector3 leftGripOffset;
        public Vector3 leftGripEuler = new Vector3(0f, 0f, -35f);
        public Vector3 weaponTiltEuler = new Vector3(5f, -5f, -20f);
        public Vector3 weaponPositionOffset = new Vector3(-.045f, .09f, .02f);
        [Header("Magazine change - normalized reload time")]
        [Range(0f, 1f)] public float ejectTime = .18f;
        [Range(0f, 1f)] public float newMagazineTime = .46f;
        [Range(0f, 1f)] public float insertTime = .73f;
        [Range(0f, 1f)] public float slideReleaseTime = .86f;
        public Vector3 freshMagazineOffset = new Vector3(-.08f, -.22f, -.015f);
        [Tooltip("Curved insertion path, in weapon-local meters; zero uses a straight path.")]
        public Vector3 magazineInsertionArc = new Vector3(-.025f, 0f, -.025f);
        public Vector3 pickupHandOffset = new Vector3(-.16f, -.24f, -.04f);
        [Min(0f)] public float droppedMagazineLifetime = .85f;
        public AudioClip reloadSound;
        [Range(0f, 1f)] public float soundVolume = .35f;
    }
}
