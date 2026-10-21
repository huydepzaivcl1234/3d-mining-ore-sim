using UnityEngine;
namespace GameData.Player.Script
{
    /// <summary>Samples a retargeted action clip, independent of the player's locomotion Animator.</summary>
    public sealed class PistolReloadVisual
    {
        private readonly PistolReloadProfile profile;
        private readonly Transform gun, magazine, slide, root, weaponOffset, magazineOffset, control;
        private readonly Vector3 magazinePosition, magazineScale, slidePosition;
        private readonly Quaternion magazineRotation;
        private readonly Vector3 magazineGripCenter;
        private bool active;
        private float progress, duration;
        private Transform droppedMagazine;
        private Renderer[] magazineRenderers;
        private bool[] originalRendererState;
        private bool ejected;
        private Vector3 dropOrigin, dropVelocity;
        private Quaternion dropRotation;
        public bool MagazineInHand => active && progress >= NewMagazineTime;
        public float PickupBlend => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(NewMagazineTime - .1f, NewMagazineTime, progress));
        private float EjectTime => Mathf.Clamp(profile.ejectTime, .05f, .35f);
        private float NewMagazineTime => Mathf.Clamp(profile.newMagazineTime, EjectTime + .1f, .65f);
        private float InsertTime => Mathf.Clamp(profile.insertTime, NewMagazineTime + .05f, .85f);
        public Vector3 PickupHandPoint => gun.TransformPoint(profile.pickupHandOffset);
        public bool Valid => root != null;
        public float HandReleaseWeight => Valid && active ? Mathf.Clamp01(control.localPosition.x) : 0f;
        public Vector3 WeaponPosition => Valid && active ? weaponOffset.localPosition + profile.weaponPositionOffset * control.localPosition.y : Vector3.zero;
        public Quaternion WeaponRotation => Valid && active
            ? weaponOffset.localRotation * Quaternion.Euler(profile.weaponTiltEuler * control.localPosition.y) : Quaternion.identity;
        public float MagazineTravel => magazine ? Vector3.Distance(magazine.localPosition, magazinePosition) : 0f;
        public float SlideTravel => slide ? Vector3.Distance(slide.localPosition, slidePosition) : 0f;
        public Vector3 LeftGripOffset => profile ? profile.leftGripOffset : Vector3.zero;
        public Vector3 LeftGripEuler => profile ? profile.leftGripEuler : Vector3.zero;
        public Transform Magazine => magazine;
        public Vector3 MagazineGripPoint => magazine.TransformPoint(magazineGripCenter) + gun.TransformVector(LeftGripOffset);

        public PistolReloadVisual(PistolReloadProfile profile, Transform gun)
        {
            this.profile = profile; this.gun = gun;
            if (!profile || !profile.targetClip || !gun) return;
            magazine = gun.Find(profile.magazinePath);
            slide = string.IsNullOrEmpty(profile.slidePath) ? null : gun.Find(profile.slidePath);
            if (!magazine || (!string.IsNullOrEmpty(profile.slidePath) && !slide)) { Debug.LogWarning("Reload target paths do not match the equipped weapon.", gun); return; }
            magazinePosition = magazine.localPosition; magazineRotation = magazine.localRotation; magazineScale = magazine.localScale;
            var magazineRenderer = magazine.GetComponentInChildren<MeshRenderer>();
            if (magazineRenderer)
                magazineGripCenter = magazine.InverseTransformPoint(magazineRenderer.transform.TransformPoint(magazineRenderer.localBounds.center));
            slidePosition = slide ? slide.localPosition : Vector3.zero;
            magazineRenderers = magazine.GetComponentsInChildren<Renderer>(true);
            originalRendererState = new bool[magazineRenderers.Length];
            for (int i = 0; i < magazineRenderers.Length; i++) originalRendererState[i] = magazineRenderers[i].enabled;
            droppedMagazine = Object.Instantiate(magazine.gameObject).transform;
            droppedMagazine.name = "Pistol Ejected Magazine (Reusable)";
            droppedMagazine.gameObject.hideFlags = HideFlags.DontSave;
            foreach (var collider in droppedMagazine.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in droppedMagazine.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            droppedMagazine.gameObject.SetActive(false);
            root = new GameObject("Pistol Reload Targets (clip)").transform;
            root.SetParent(gun, false);
            root.gameObject.hideFlags = HideFlags.DontSave;
            weaponOffset = Child("WeaponOffset"); magazineOffset = Child("MagazineOffset"); control = Child("Control");
        }

        private Transform Child(string name)
        {
            var child = new GameObject(name).transform; child.SetParent(root, false); return child;
        }

        public void Sample(float progress, float duration = 2f)
        {
            if (!Valid) return;
            active = true;
            this.progress = Mathf.Clamp01(progress); this.duration = Mathf.Max(.1f, duration);
            profile.targetClip.SampleAnimation(root.gameObject, Mathf.Clamp01(progress) * profile.targetClip.length);
        }

        public void ApplyMechanism(bool emptyReload, float slideTravel)
        {
            if (!Valid || !active) return;
            Vector3 offset;
            if (progress < EjectTime)
                offset = Vector3.down * (.045f * Mathf.SmoothStep(0f, 1f, progress / EjectTime));
            else if (progress < NewMagazineTime) offset = profile.freshMagazineOffset;
            else
            {
                float insertion = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(NewMagazineTime, InsertTime, progress));
                offset = Vector3.Lerp(profile.freshMagazineOffset, Vector3.zero, insertion) + profile.magazineInsertionArc * Mathf.Sin(insertion * Mathf.PI);
            }
            magazine.localPosition = magazinePosition + magazine.parent.InverseTransformVector(gun.TransformVector(offset));
            Quaternion worldDelta = gun.rotation * magazineOffset.localRotation * Quaternion.Inverse(gun.rotation);
            float tilt = progress >= NewMagazineTime ? 1f - Mathf.InverseLerp(NewMagazineTime, InsertTime, progress) : 0f;
            magazine.localRotation = Quaternion.Slerp(magazineRotation, Quaternion.Inverse(magazine.parent.rotation) * worldDelta * magazine.parent.rotation * magazineRotation, tilt);
            magazine.localScale = Vector3.Scale(magazineScale, magazineOffset.localScale);
            if (progress >= EjectTime && !ejected)
            {
                ejected = true;
                Vector3 exitPosition = magazine.parent.TransformPoint(magazinePosition) - gun.up * .045f;
                Quaternion exitRotation = magazine.parent.rotation * magazineRotation;
                droppedMagazine.SetPositionAndRotation(exitPosition, exitRotation);
                droppedMagazine.localScale = magazine.lossyScale;
                dropOrigin = exitPosition; dropRotation = exitRotation;
                dropVelocity = -gun.up * .65f - gun.right * .12f;
                droppedMagazine.gameObject.SetActive(true);
            }
            float dropAge = Mathf.Max(0f, (progress - EjectTime) * duration);
            if (ejected && droppedMagazine.gameObject.activeSelf)
            {
                droppedMagazine.position = dropOrigin + dropVelocity * dropAge + Vector3.down * (4.905f * dropAge * dropAge);
                droppedMagazine.rotation = dropRotation * Quaternion.Euler(dropAge * 140f, 0f, dropAge * 35f);
                if (dropAge >= profile.droppedMagazineLifetime) droppedMagazine.gameObject.SetActive(false);
            }
            bool visible = progress < EjectTime || progress >= NewMagazineTime;
            for (int i = 0; i < magazineRenderers.Length; i++) magazineRenderers[i].enabled = visible && originalRendererState[i];
            float release = Mathf.Clamp(profile.slideReleaseTime, InsertTime + .02f, .98f);
            float heldOpen = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(release, Mathf.Min(1f, release + .06f), progress));
            if (slide) slide.localPosition = slidePosition + Vector3.back * (emptyReload ? Mathf.Max(0f, slideTravel) * heldOpen : 0f);
        }

        public void Reset()
        {
            if (!active) return;
            active = false;
            ejected = false;
            if (droppedMagazine) droppedMagazine.gameObject.SetActive(false);
            if (magazineRenderers != null) for (int i = 0; i < magazineRenderers.Length; i++) magazineRenderers[i].enabled = originalRendererState[i];
            if (magazine) { magazine.localPosition = magazinePosition; magazine.localRotation = magazineRotation; magazine.localScale = magazineScale; }
            if (slide) slide.localPosition = slidePosition;
        }

        public void Dispose() { Reset(); if (root) Object.Destroy(root.gameObject); if (droppedMagazine) Object.Destroy(droppedMagazine.gameObject); }
    }
}
