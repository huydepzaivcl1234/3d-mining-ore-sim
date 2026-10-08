using UnityEngine;
using TMPro;
using Microlight.MicroBar;

namespace MiningSimulator.Ores
{
    public sealed class MiningNpcHealthBar : MonoBehaviour
    {
        [SerializeField] private MiningNpc miner;
        [SerializeField] private NpcData data;
        [SerializeField] private MicroBar bar;
        [SerializeField] private TextMeshPro label;
        private Camera view;
        private float maximum;

        public static void Ensure(MiningNpc miner, NpcData data)
        {
            if (miner == null || data == null || data.MinerHealthBarPrefab == null) return;
            var existing = miner.GetComponentInChildren<MiningNpcHealthBar>(true);
            if (existing == null)
            {
                var obj = Instantiate(data.MinerHealthBarPrefab, miner.transform);
                obj.name = "Miner Health Bar";
                existing = obj.GetComponent<MiningNpcHealthBar>() ?? obj.AddComponent<MiningNpcHealthBar>();
            }
            existing.Bind(miner, data);
        }

        private void Bind(MiningNpc target, NpcData config)
        {
            if (miner != null) miner.HealthChanged -= Refresh;
            miner = target; data = config;
            bar ??= GetComponentInChildren<MicroBar>(true);
            label ??= GetComponentInChildren<TextMeshPro>(true);
            if (bar != null) { maximum = miner.MaxHealth; bar.Initialize(maximum); }
            miner.HealthChanged += Refresh;
            Refresh(miner.Health, miner.MaxHealth);
        }
        private void OnEnable() { if (miner != null) { miner.HealthChanged -= Refresh; miner.HealthChanged += Refresh; } }
        private void OnDisable() { if (miner != null) miner.HealthChanged -= Refresh; }
        private void Refresh(float hp, float max)
        {
            if (bar != null)
            {
                if (!Mathf.Approximately(maximum, max)) { maximum = max; bar.SetNewMaxHP(max, true); }
                bar.UpdateBar(hp);
            }
            if (label != null) label.text = $"{hp:0} / {max:0}";
        }
        private void LateUpdate()
        {
            if (miner == null || data == null) return;
            view ??= Camera.main;
            transform.position = miner.transform.position + data.MinerHealthBarOffset;
            if (view != null) transform.rotation = Quaternion.LookRotation(transform.position - view.transform.position, view.transform.up);
            var scale = miner.transform.lossyScale;
            float size = data.MinerHealthBarScale;
            transform.localScale = new Vector3(size / Mathf.Max(.001f, Mathf.Abs(scale.x)), size / Mathf.Max(.001f, Mathf.Abs(scale.y)), size / Mathf.Max(.001f, Mathf.Abs(scale.z)));
        }
    }
}
