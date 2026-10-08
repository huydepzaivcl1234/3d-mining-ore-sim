using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Designer-owned persistent cosmetic sold by the Gem Shop.</summary>
    [CreateAssetMenu(fileName = "MiningCosmetic", menuName = "Mining Simulator/Game Data/Cosmetic")]
    public sealed class MiningCosmeticData : ScriptableObject
    {
        [SerializeField] private string cosmeticId = "golden_pickaxe";
        [SerializeField] private string displayName = "Golden Pickaxe";
        [TextArea, SerializeField] private string description = "A brilliant miner pickaxe skin with a golden sparkle trail.";
        [SerializeField] private Sprite icon;
        [SerializeField] private string iconFallback = "P";
        [SerializeField] private Color fallbackColor = new(1f, 0.76f, 0.08f, 1f);
        [Header("Optional authored visuals")]
        [Tooltip("Assign your Golden Pickaxe prefab here when it is imported. Leave empty to tint each miner's current pickaxe gold.")]
        [SerializeField] private GameObject toolPrefab;
        [Tooltip("Assign the particle prefab from the Golden Pickaxe here. A small gold sparkle is used if empty.")]
        [SerializeField] private ParticleSystem particlePrefab;
        [SerializeField] private Vector3 toolLocalPosition;
        [SerializeField] private Vector3 toolLocalEulerAngles;
        [SerializeField] private Vector3 toolLocalScale = Vector3.one;
        [Header("Fallback gold skin")]
        [SerializeField] private Color tint = new(1f, 0.68f, 0.06f, 1f);

        public string CosmeticId => cosmeticId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public string IconFallback => iconFallback;
        public Color FallbackColor => fallbackColor;
        public GameObject ToolPrefab => toolPrefab;
        public ParticleSystem ParticlePrefab => particlePrefab;
        public Vector3 ToolLocalPosition => toolLocalPosition;
        public Vector3 ToolLocalEulerAngles => toolLocalEulerAngles;
        public Vector3 ToolLocalScale => toolLocalScale;
        public Color Tint => tint;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(cosmeticId)) cosmeticId = name;
            if (string.IsNullOrWhiteSpace(displayName)) displayName = name;
            if (toolLocalScale.x <= 0f || toolLocalScale.y <= 0f || toolLocalScale.z <= 0f)
                toolLocalScale = Vector3.one;
        }
    }
}
