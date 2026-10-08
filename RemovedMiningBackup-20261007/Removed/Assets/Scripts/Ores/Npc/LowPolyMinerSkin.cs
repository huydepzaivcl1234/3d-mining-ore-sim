using UnityEngine;

namespace MiningSimulator.Ores
{
    // Kept with the same script GUID so existing NPC prefabs don't acquire a Missing Script.
    // The temporary Unity cube outfit has been retired. A Blender-made skinned mesh can
    // replace the original model without modifying the mining controller or its animations.
    [DisallowMultipleComponent]
    public sealed class LowPolyMinerSkin : MonoBehaviour
    {
        [SerializeField, HideInInspector] private Material jacket;
        [SerializeField, HideInInspector] private Material trousers;
        [SerializeField, HideInInspector] private Material skin;
        [SerializeField, HideInInspector] private Material helmet;
        [SerializeField, HideInInspector] private Material boots;
        [SerializeField, HideInInspector] private Material lamp;
    }
}
