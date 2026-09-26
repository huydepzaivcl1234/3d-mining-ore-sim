using Microlight.MicroBar;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent]
    public sealed class MiningCharacterHealth : MonoBehaviour
    {
        [Min(1f), SerializeField] private float maxHealth = 100f;
        [SerializeField] private Transform healthBar;
        [SerializeField] private MicroBar microBar;
        [SerializeField] private TMP_Text healthLabel;
        private Camera healthCamera;
        private float health;
        public float Health => health;
        public float MaxHealth => maxHealth;
        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || health <= 0f) return;
            health = Mathf.Max(0f, health - amount);
            Refresh();
        }

        private void Awake()
        {
            health = Mathf.Max(1f, maxHealth);
            if (microBar == null && healthBar != null)
                microBar = healthBar.GetComponent<MicroBar>();
            if (microBar != null) microBar.Initialize(maxHealth);
            Refresh();
        }

        private void LateUpdate()
        {
            if (healthBar == null) return;
            if (healthCamera == null) healthCamera = Camera.main;
            if (healthCamera != null)
                healthBar.rotation = Quaternion.LookRotation(
                    healthBar.position - healthCamera.transform.position,
                    healthCamera.transform.up);
        }

        private void Refresh()
        {
            if (microBar != null) microBar.UpdateBar(health, true);
            if (healthLabel != null)
                healthLabel.text = $"{Mathf.CeilToInt(health)} / {Mathf.CeilToInt(maxHealth)}";
        }

        private void OnValidate() => maxHealth = Mathf.Max(1f, maxHealth);
    }
}
