using UnityEngine;
namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Chest Defense/Towers/Catalog")]
    public sealed class TowerCatalog : ScriptableObject
    {
        public TowerData[] towers = System.Array.Empty<TowerData>();
    }
}
