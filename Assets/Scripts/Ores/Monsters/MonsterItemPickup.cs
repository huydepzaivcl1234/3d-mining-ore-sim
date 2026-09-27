using UnityEngine;

namespace MiningSimulator.Ores
{
    // Independent of the corpse so destroying the monster cannot destroy its loot.
    public sealed class MonsterItemPickup : MonoBehaviour
    {
        private MonsterRewardData settings;
        private MiningItemData item;
        private int amount;
        private MiningItemSystem inventory;
        private MiningCharacterHealth player;
        private Vector3 velocity;
        private float groundY, landedTime, pullSpeed;
        private bool landed;
        private Camera view;
        public static void Spawn(MonsterRewardData settings, MonsterItemDrop drop, int count,
            Vector3 origin, MiningCharacterHealth player, MiningItemSystem inventory, LayerMask groundLayers)
        {
            var obj = new GameObject("Monster Drop " + drop.item.name);
            obj.transform.position = origin;
            var pickup = obj.AddComponent<MonsterItemPickup>();
            pickup.settings = settings; pickup.item = drop.item; pickup.amount = count;
            pickup.player = player; pickup.inventory = inventory;
            Vector2 direction = Random.insideUnitCircle.normalized;
            pickup.velocity = new Vector3(direction.x * settings.launchOutSpeed, settings.launchUpSpeed, direction.y * settings.launchOutSpeed);
            pickup.groundY = origin.y - 0.6f;
            float closest = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(origin + Vector3.up * 2, Vector3.down, 100, groundLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y < 0.5f || hit.collider.GetComponentInParent<MiningCharacterHealth>() != null || hit.distance >= closest) continue;
                closest = hit.distance; pickup.groundY = hit.point.y;
            }
            var canvasObject = new GameObject("Icon", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(obj.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = new Vector2(100, 100);
            rect.localScale = Vector3.one * Mathf.Max(0.01f, settings.iconSize) / 100;
            var iconObject = new GameObject("Item Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            iconObject.transform.SetParent(canvasObject.transform, false);
            var image = iconObject.GetComponent<UnityEngine.UI.Image>();
            image.sprite = drop.icon != null ? drop.icon : drop.item.InventoryIcon;
            image.preserveAspect = true; image.raycastTarget = false;
            image.color = image.sprite != null ? Color.white : drop.item.FallbackColor;
            var iconRect = (RectTransform)iconObject.transform;
            iconRect.anchorMin = Vector2.zero; iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
        }
        private void LateUpdate()
        {
            if (view == null) view = Camera.main;
            if (view != null) transform.rotation = view.transform.rotation;
        }
        private void Update()
        {
            if (settings == null) return;
            float floor = groundY + Mathf.Max(0.01f, settings.iconSize) * 0.5f;
            if (!landed)
            {
                velocity += Physics.gravity * Time.deltaTime;
                Vector3 step = velocity * Time.deltaTime;
                float distance = step.magnitude;
                if (distance > 0 && Physics.SphereCast(transform.position, 0.05f, step / distance, out var hit, distance,
                    ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<MiningCharacterHealth>() == null)
                {
                    transform.position = hit.point + hit.normal * 0.06f;
                    if (hit.normal.y > 0.5f) Land();
                    else velocity = Vector3.Reflect(velocity, hit.normal) * 0.4f;
                }
                else transform.position += step;
                if (transform.position.y <= floor)
                {
                    transform.position = new Vector3(transform.position.x, floor, transform.position.z);
                    Land();
                }
                return;
            }
            if (Time.time - landedTime < settings.groundWaitSeconds || player == null || player.Health <= 0 || inventory == null) return;
            if (!inventory.CanAddItem(item, amount)) return;
            Vector3 target = player.transform.position + Vector3.up * 0.8f;
            if ((target - transform.position).sqrMagnitude > settings.attractionRadius * settings.attractionRadius) return;
            pullSpeed += Mathf.Max(0, settings.attractionAcceleration) * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, pullSpeed * Time.deltaTime);
            if ((target - transform.position).sqrMagnitude < 0.15f * 0.15f && inventory.TryAddItem(item, amount)) Destroy(gameObject);
        }
        private void Land()
        {
            if (landed) return;
            landed = true; landedTime = Time.time;
            pullSpeed = Mathf.Max(0.1f, settings.attractionSpeed);
        }
    }
}
