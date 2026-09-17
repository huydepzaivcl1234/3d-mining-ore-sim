using UnityEngine;
using UnityEngine.AI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Reports, once shortly after startup, which navigation backend the miners actually ended up
    /// using and why.
    ///
    /// This exists because every layer in the navigation stack fails *silently* by design - a
    /// missing NavMesh quietly falls back to the A* grid, and a missing grid quietly falls back to
    /// the old reactive steering. That is the right runtime behaviour (miners keep working), but
    /// it makes a misconfigured scene look like "pathfinding is just bad" rather than "pathfinding
    /// never turned on". Drop this on the same object as the builder while setting things up.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class MiningNavigationValidator : MonoBehaviour
    {
        [Tooltip("Seconds to wait before checking, so surfaces and grids have finished their first build.")]
        [Min(0f)] [SerializeField] private float checkDelay = 1f;
        [Tooltip("Logs the result even when everything is configured correctly.")]
        [SerializeField] private bool logOnSuccess = true;

        private void Start()
        {
            Invoke(nameof(Validate), checkDelay);
        }

        private void Validate()
        {
            bool navMeshReady = MiningNavigation.NavMeshAvailable;
            bool gridReady = MiningNavGrid.Instance != null && MiningNavGrid.Instance.HasBaked;

            if (navMeshReady)
            {
                int obstacleCount = FindObjectsByType<NavMeshObstacle>(FindObjectsSortMode.None).Length;
                int carvingCount = 0;
                foreach (NavMeshObstacle obstacle in
                         FindObjectsByType<NavMeshObstacle>(FindObjectsSortMode.None))
                {
                    if (obstacle != null && obstacle.carving)
                    {
                        carvingCount++;
                    }
                }

                if (obstacleCount == 0)
                {
                    Debug.LogWarning(
                        "[MiningNavigation] NavMesh is built, but no NavMeshObstacle exists in the " +
                        "scene. Miners will path over ores as if they weren't there. Add " +
                        nameof(MiningNavMeshObstacle) + " to the ore prefab.", this);
                    return;
                }

                if (carvingCount == 0)
                {
                    Debug.LogWarning(
                        "[MiningNavigation] NavMeshObstacles exist but none have Carve enabled, so " +
                        "they only nudge agents locally instead of blocking paths. Enable carving.",
                        this);
                    return;
                }

                if (logOnSuccess)
                {
                    Debug.Log(
                        $"[MiningNavigation] Using Unity NavMesh. {carvingCount} carving obstacle(s) active.",
                        this);
                }

                return;
            }

            if (gridReady)
            {
                Debug.LogWarning(
                    "[MiningNavigation] No NavMesh found - falling back to the MiningNavGrid A* " +
                    "grid. This works, but NavMesh is preferred. Check that a " +
                    nameof(MiningNavMeshBuilder) + " with a baked NavMeshSurface exists and that " +
                    "the floor geometry is marked Navigation Static / included in the surface.",
                    this);
                return;
            }

            Debug.LogError(
                "[MiningNavigation] Neither a NavMesh nor a MiningNavGrid is available. Miners are " +
                "running on direct-line reactive steering only and will NOT find shortest paths.",
                this);
        }
    }
}
