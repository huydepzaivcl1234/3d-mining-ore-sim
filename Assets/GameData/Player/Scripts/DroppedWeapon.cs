using UnityEngine;
using UnityEngine.Rendering;

namespace GameData.Player.Script
{
    // A world pickup is a static visual snapshot, never a cloned weapon controller.
    public sealed class DroppedWeapon : MonoBehaviour
    {
        private PlayerWeaponProgression owner;
        private Vector3 start, landing;
        private float age, halfHeight;
        private bool collected;
        public bool Landed => age >= .55f;
        public Vector3 GroundPoint { get; private set; }

        public static DroppedWeapon Create(PlayerWeaponProgression owner, Transform source, Vector3 start, Vector3 ground)
        {
            if (!source) return null;
            var root = new GameObject("Dropped " + source.name);
            var visual = new GameObject("Weapon visual").transform;
            visual.SetParent(root.transform, false);
            bool any = false;
            Bounds bounds = default;
            foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh || !renderer.enabled || !renderer.gameObject.activeInHierarchy
                    || renderer.bounds.size.sqrMagnitude < .000001f) continue;
                var part = new GameObject(renderer.name);
                part.transform.SetParent(visual, false);
                part.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                part.transform.localScale = renderer.transform.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var copy = part.AddComponent<MeshRenderer>();
                copy.sharedMaterials = renderer.sharedMaterials;
                copy.shadowCastingMode = ShadowCastingMode.On;
                copy.receiveShadows = true;
                if (!any) bounds = copy.bounds; else bounds.Encapsulate(copy.bounds);
                any = true;
            }
            if (!any) { Destroy(root); return null; }
            visual.position -= bounds.center;
            var pickup = root.AddComponent<DroppedWeapon>();
            pickup.owner = owner;
            pickup.start = start;
            pickup.GroundPoint = ground;
            pickup.halfHeight = bounds.extents.y;
            pickup.landing = ground + Vector3.up * (pickup.halfHeight + .25f);
            root.transform.position = start;
            return pickup;
        }

        private void Update()
        {
            if (!owner) { Destroy(gameObject); return; }
            if (Time.deltaTime <= 0 || collected) return;
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / .55f);
            transform.position = Landed
                ? landing + Vector3.up * (Mathf.Sin((age - .55f) * 2.4f) * .035f)
                : Vector3.Lerp(start, landing, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .28f);
            transform.Rotate(Vector3.up, 85f * Time.deltaTime, Space.World);
            // Let the throw finish; do not immediately collect at the release point.
            if (age < .85f || !owner.CanCollectDroppedWeapon) return;
            Vector3 delta = owner.transform.position - GroundPoint;
            if (new Vector2(delta.x, delta.z).sqrMagnitude > 1.2f * 1.2f || Mathf.Abs(delta.y) > 2f) return;
            // A nearby pickup behind a wall must not be collected through it.
            Vector3 eye = owner.GetComponent<PistolShooting>().ViewCamera.transform.position;
            foreach (var hit in Physics.RaycastAll(eye, transform.position - eye, Vector3.Distance(eye, transform.position), ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(owner.transform) && !hit.transform.IsChildOf(transform)) return;
            if (owner.PickupDroppedWeapon(this)) { collected = true; Destroy(gameObject); }
        }
    }
}
