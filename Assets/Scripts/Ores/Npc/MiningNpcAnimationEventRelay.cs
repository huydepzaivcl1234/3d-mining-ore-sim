using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed class MiningNpcAnimationEventRelay : MonoBehaviour
    {
        public void OnMiningImpact()
        {
            transform.parent?.SendMessage(nameof(OnMiningImpact), SendMessageOptions.DontRequireReceiver);
        }
    }
}
