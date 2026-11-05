using UnityEngine;
using TMPro;
namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Survivor/UI/Upgrade Sanctuary")]
    public sealed class UpgradeSanctuarySkin : ScriptableObject
    {
        [System.Serializable] public class Artwork {public Sprite icon,glow,ring,medallion;}
        public TMP_FontAsset heading,body;
        public Material vectorMaterial;
        public Sprite ambientLeft,ambientRight,spark;
        public Artwork sword,speed,heart,fire,ricochet,cube;
    }
}
