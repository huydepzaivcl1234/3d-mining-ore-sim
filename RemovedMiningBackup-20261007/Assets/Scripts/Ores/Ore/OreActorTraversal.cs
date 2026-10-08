using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    // Per-instance pairs preserve player/ore collisions and rebind after pooling/spawning.
    internal static class OreActorTraversal
    {
        private static readonly Dictionary<GameObject, Collider[]> Actors = new();
        private static readonly Dictionary<Ore, Collider[]> Ores = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Actors.Clear(); Ores.Clear(); }

        public static void RegisterActor(GameObject actor)
        {
            var colliders = actor.GetComponentsInChildren<Collider>(true);
            Actors[actor] = colliders;
            foreach (var ore in Ores.Values) Pair(colliders, ore);
        }
        public static void UnregisterActor(GameObject actor) => Actors.Remove(actor);
        public static void RegisterOre(Ore ore)
        {
            var colliders = ore.GetComponentsInChildren<Collider>(true);
            Ores[ore] = colliders;
            foreach (var actor in Actors.Values) Pair(actor, colliders);
            foreach (var obstacle in ore.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true))
                obstacle.enabled = false;
        }
        public static void UnregisterOre(Ore ore) => Ores.Remove(ore);
        private static void Pair(Collider[] actor, Collider[] ore)
        {
            foreach (var a in actor)
                foreach (var o in ore)
                    if (a != null && o != null && a.enabled && o.enabled && a.gameObject.activeInHierarchy && o.gameObject.activeInHierarchy)
                        Physics.IgnoreCollision(a, o, true);
        }
    }
}
