using System.Collections;
using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed class MiningNavigationValidator : MonoBehaviour
    {
        [Min(0)] [SerializeField] private float checkDelay = 1;
        [SerializeField] private bool logOnSuccess = true;
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(checkDelay);
            var grid = MiningNavGrid.Instance;
            if (grid == null) { Debug.LogError("[MiningNavigation] Missing Terrain A* grid.", this); yield break; }
            while (!grid.HasBaked && grid.CellCount > 0) yield return null;
            if (grid.HasBaked && logOnSuccess) Debug.Log($"[MiningNavigation] Terrain A* ready: {grid.CellCount} cells. Dirty-region updates; queued miner searches.", this);
        }
    }
}
