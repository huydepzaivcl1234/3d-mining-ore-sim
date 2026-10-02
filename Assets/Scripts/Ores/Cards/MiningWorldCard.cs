using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed class MiningWorldCard : MonoBehaviour
    {
        private MiningCardSystem owner;
        private MiningCardData data;
        private MiningCardData.Tier tier;
        private float ready;
        private bool collected;
        private LineRenderer beam;
        private bool landed;
        private Vector3 groundAnchor, landingPosition;
        private float landedAt, halfHeight;
        private void OnCollisionEnter(Collision collision) => TryLand(collision);
        private void OnCollisionStay(Collision collision) => TryLand(collision);
        private void TryLand(Collision collision)
        {
            if (landed || data == null || (data.landingLayers.value & (1 << collision.gameObject.layer)) == 0) return;
            // Enemies, players and ore are not a landing surface.
            if (collision.collider.GetComponentInParent<MiningCharacterHealth>() != null ||
                collision.collider.GetComponentInParent<Ore>() != null ||
                collision.collider.GetComponentInParent<MiningWorldCard>() != null) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                if (contact.normal.y < data.groundNormalMinimum) continue;
                landed = true;
                landedAt = Time.time;
                groundAnchor = new Vector3(transform.position.x, contact.point.y, transform.position.z);
                landingPosition = transform.position;
                var box = GetComponent<BoxCollider>();
                halfHeight = box != null ? box.size.y * .5f : 0f;
                var body = GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = true;
                if (box != null) box.enabled = false;
                transform.rotation = Quaternion.identity;
                break;
            }
        }
        public void Configure(MiningCardSystem system, MiningCardData settings, MiningCardData.Tier definition)
        {
            owner=system; data=settings; tier=definition;
            ready=Time.time + data.pickupDelay;
            var beamObject=new GameObject("Card Light Pillar");
            beamObject.transform.SetParent(transform,false);
            beam=beamObject.AddComponent<LineRenderer>();
            beam.useWorldSpace=true; beam.positionCount=2;
            beam.sharedMaterial=data.beamMaterial;
            beam.startWidth=data.beamWidth; beam.endWidth=data.beamWidth;
            beam.startColor=tier.color; beam.endColor=new Color(tier.color.r,tier.color.g,tier.color.b,0f);
        }
        private void LateUpdate()
        {
            if (owner == null || data == null) return;
            if (landed)
            {
                float elapsed = Time.time - landedAt;
                float lift = data.hoverLiftDuration > 0f ? Mathf.Clamp01(elapsed / data.hoverLiftDuration) : 1f;
                lift = lift * lift * (3f - 2f * lift);
                // Pivot is centred on the card mesh; keep its bottom above the floor.
                float bob = Mathf.Sin(elapsed * data.bobFrequency * Mathf.PI * 2f) *
                    Mathf.Min(data.bobAmplitude, data.hoverHeight);
                transform.position = Vector3.Lerp(landingPosition,
                    groundAnchor + Vector3.up * (halfHeight + data.hoverHeight + bob), lift);
                transform.rotation = Quaternion.AngleAxis(elapsed * data.rotationDegreesPerSecond, Vector3.up);
            }
            // The pillar stays vertical in world space even while the card tumbles.
            Vector3 anchor = landed ? groundAnchor : transform.position;
            if (beam != null) { beam.SetPosition(0,anchor); beam.SetPosition(1,anchor+Vector3.up*data.beamHeight); }
            if (collected || !landed || Time.time < ready || !owner.CanCollect) return;
            Vector3 delta=owner.Player.transform.position-groundAnchor;
            if (delta.sqrMagnitude > data.pickupRadius*data.pickupRadius) return;
            if (!owner.Collect(tier)) return;
            collected=true;
            Destroy(gameObject);
        }
    }
}
