using System;
using UnityEngine;
namespace MiningSimulator.Ores
{
    public enum GroundSurface { Grass, Stone, Iron, Wood }
    public static class GroundSurfaceLayers
    {
        public static int Mask => LayerMask.GetMask("Ground","Grass","Stone","Iron","Wood");
        public static bool Contains(int layer)=>(Mask&(1<<layer))!=0;
    }
    [CreateAssetMenu(menuName="Survivor/Audio/Surface footsteps",fileName="SurfaceAudio")]
    public sealed class SurfaceAudioData:ScriptableObject
    {
        [Serializable] public sealed class Surface
        {
            public GroundSurface surface;
            public LayerMask layers;
            public AudioClip[] footsteps=Array.Empty<AudioClip>();
            public AudioClip jump,landing;
        }
        public Surface[] surfaces=Array.Empty<Surface>();
        [Tooltip("Used on unknown layers or when that surface has no assigned steps.")]
        public AudioClip[] fallbackSteps=Array.Empty<AudioClip>();
        public Surface ForLayer(int layer){foreach(var s in surfaces)if(s!=null&&(s.layers.value&(1<<layer))!=0)return s;return null;}
    }
}
