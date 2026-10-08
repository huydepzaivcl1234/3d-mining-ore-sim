using UnityEngine;

namespace MiningSimulator.Ores
{
    // Scene-authored reference model, not an extra member of the runtime population.
    [DefaultExecutionOrder(-100)]
    public sealed class MonsterScenePreview : MonoBehaviour
    {
        private void Awake() => gameObject.SetActive(false);
    }
}
