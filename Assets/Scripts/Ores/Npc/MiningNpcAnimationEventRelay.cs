using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Forwards animation events from the model Animator to its parent MiningNpc.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningNpcAnimationEventRelay : MonoBehaviour
    {
        public void OnMiningImpact()
        {
            Transform current = transform.parent;
            while (current != null)
            {
                current.gameObject.SendMessage(nameof(OnMiningImpact), SendMessageOptions.DontRequireReceiver);
                current = current.parent;
            }
        }
    }
}
