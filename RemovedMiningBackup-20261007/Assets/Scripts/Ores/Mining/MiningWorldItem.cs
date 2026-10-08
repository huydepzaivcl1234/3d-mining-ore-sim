using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>One physical item drop that bounces briefly before entering the inventory.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningWorldItem : MonoBehaviour
    {
        private MiningItemSystem itemSystem;
        private MiningItemData item;
        private MiningItemDatabase settings;
        private Rigidbody body;
        private float spawnedAt;
        private float nextPickupAttempt;
        private int remainingBounces;
        private bool hasLanded;
        private bool collected;

        public MiningItemData Item => item;

        public void Initialize(MiningItemSystem targetSystem, MiningItemData targetItem,
            MiningItemDatabase targetSettings)
        {
            itemSystem = targetSystem;
            item = targetItem;
            settings = targetSettings;
            spawnedAt = Time.time;
            nextPickupAttempt = float.PositiveInfinity;
            remainingBounces = settings.BounceCount;
            hasLanded = false;

            CreateVisual();
            SphereCollider itemCollider = gameObject.AddComponent<SphereCollider>();
            itemCollider.radius = settings.ColliderRadius;
            body = gameObject.AddComponent<Rigidbody>();
            body.mass = settings.Mass;
            body.linearDamping = settings.LinearDamping;
            body.angularDamping = settings.AngularDamping;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Vector2 horizontalRange = settings.HorizontalImpulseRange;
            Vector2 upwardRange = settings.UpwardImpulseRange;
            Vector2 angularRange = settings.AngularSpeedRange;
            Vector2 direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = Vector2.right;
            }
            Vector2 horizontal = direction.normalized * Random.Range(
                Mathf.Min(horizontalRange.x, horizontalRange.y),
                Mathf.Max(horizontalRange.x, horizontalRange.y));
            float upward = Random.Range(Mathf.Min(upwardRange.x, upwardRange.y),
                Mathf.Max(upwardRange.x, upwardRange.y));
            body.AddForce(new Vector3(horizontal.x, upward, horizontal.y), ForceMode.Impulse);
            float angularSpeed = Random.Range(Mathf.Min(angularRange.x, angularRange.y),
                Mathf.Max(angularRange.x, angularRange.y));
            body.angularVelocity = Random.onUnitSphere * angularSpeed * Mathf.Deg2Rad;
        }

        private void Update()
        {
            if (collected || settings == null)
            {
                return;
            }
            if (Time.time - spawnedAt >= settings.MaximumWorldLifetime)
            {
                Destroy(gameObject);
                return;
            }
            if (Time.time >= nextPickupAttempt)
            {
                TryCollect();
                nextPickupAttempt = Time.time + 0.5f;
            }
        }

        private void OnMouseDown()
        {
            TryCollect();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (body == null || settings == null || collision.contactCount == 0 ||
                collision.GetContact(0).normal.y < 0.45f)
            {
                return;
            }

            if (!hasLanded)
            {
                hasLanded = true;
                nextPickupAttempt = Time.time + settings.AutoPickupDelay;
            }
            if (remainingBounces <= 0)
            {
                return;
            }
            remainingBounces--;
            Vector3 velocity = body.linearVelocity;
            body.linearVelocity = new Vector3(velocity.x * 0.65f,
                Mathf.Max(settings.BounceVelocity, Mathf.Abs(velocity.y) * 0.35f),
                velocity.z * 0.65f);
        }

        private void TryCollect()
        {
            if (collected || itemSystem == null || item == null ||
                !itemSystem.TryAddItem(item))
            {
                return;
            }
            collected = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void CreateVisual()
        {
            GameObject visual;
            if (item.WorldModel != null)
            {
                visual = Instantiate(item.WorldModel, transform);
                visual.name = "Model";
            }
            else
            {
                PrimitiveType primitive = item.EffectType == MiningItemEffectType.MoneyReward
                    ? PrimitiveType.Capsule
                    : PrimitiveType.Sphere;
                visual = GameObject.CreatePrimitive(primitive);
                visual.name = "Fallback Model";
                visual.transform.SetParent(transform, false);
                Renderer renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = item.FallbackColor;
                }
                if (primitive == PrimitiveType.Capsule)
                {
                    visual.transform.localScale = new Vector3(0.45f, 0.8f, 0.45f);
                    visual.transform.localRotation = Quaternion.Euler(0f, 0f, 70f);
                }
            }

            visual.transform.localPosition = item.ModelLocalPosition;
            visual.transform.localRotation *= Quaternion.Euler(item.ModelLocalEulerAngles);
            visual.transform.localScale *= item.WorldScale;
            foreach (Collider nestedCollider in visual.GetComponentsInChildren<Collider>(true))
            {
                nestedCollider.enabled = false;
            }
            foreach (Rigidbody nestedBody in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                nestedBody.isKinematic = true;
                nestedBody.detectCollisions = false;
            }
        }
    }
}
