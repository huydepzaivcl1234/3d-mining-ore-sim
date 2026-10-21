using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Ores
{
    public interface IPooledProjectile
    {
        void ResetForPool();
    }
    /// <summary>Scene-owned, bounded projectile reuse. No saves and no DontDestroyOnLoad objects.</summary>
    public sealed class SceneProjectilePool : MonoBehaviour
    {
        static readonly Dictionary<Scene, SceneProjectilePool> Scenes = new();
        readonly Dictionary<(GameObject, Type), Bucket> buckets = new();
        [SerializeField, Min(0)] int maximumStored = 256;
        Transform storage;
        int stored;
        public int StoredCount => stored;
        public int CreatedCount { get; private set; }
        public int ReusedCount { get; private set; }
        public int DiscardedCount { get; private set; }

        sealed class Bucket
        {
            public int capacity;
            public readonly Stack<ProjectilePoolMember> free = new();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Scenes.Clear();

        public static T Rent<T>(GameObject prefab, Vector3 position, Quaternion rotation, Scene scene, int capacity) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("A loaded owning scene is required.");
            if (!Scenes.TryGetValue(scene, out var pool) || pool == null)
            {
                var root = new GameObject("Combat projectile pools");
                SceneManager.MoveGameObjectToScene(root, scene);
                pool = root.AddComponent<SceneProjectilePool>();
                var inactive = new GameObject("Stored projectiles (inactive)");
                inactive.transform.SetParent(root.transform, false); inactive.SetActive(false);
                pool.storage = inactive.transform;
                Scenes[scene] = pool;
            }
            var key = (prefab, typeof(T));
            if (!pool.buckets.TryGetValue(key, out var bucket))
            { bucket = new Bucket(); pool.buckets.Add(key, bucket); }
            bucket.capacity = Mathf.Max(0, capacity);
            ProjectilePoolMember member = null;
            while (bucket.free.Count > 0 && member == null)
            { member = bucket.free.Pop(); pool.stored--; }
            if (member == null)
            {
                var item = prefab != null ? Instantiate(prefab, pool.storage) : new GameObject(typeof(T).Name);
                item.SetActive(false); item.transform.SetParent(pool.storage, false);
                member = item.AddComponent<ProjectilePoolMember>();
                member.pool = pool; member.key = key;
                member.originalScale = prefab != null ? prefab.transform.localScale : Vector3.one;
                member.projectile = item.GetComponent<T>() ?? item.AddComponent<T>();
                member.trails = item.GetComponentsInChildren<TrailRenderer>(true);
                member.particles = item.GetComponentsInChildren<ParticleSystem>(true);
                pool.CreatedCount++;
            }
            else pool.ReusedCount++;
            member.rented = true;
            member.transform.SetParent(pool.transform, false);
            member.transform.SetPositionAndRotation(position, rotation);
            member.transform.localScale = member.originalScale;
            foreach (var trail in member.trails) if(trail!=null) trail.Clear();
            foreach (var particles in member.particles) if(particles!=null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // Caller initializes the inactive projectile, then activates it. No old target can tick.
            return (T)member.projectile;
        }

        public static void Release(GameObject projectile)
        {
            if (projectile == null) return;
            var member = projectile.GetComponent<ProjectilePoolMember>();
            if (member == null || member.pool == null)
            { projectile.SetActive(false); Discard(projectile); return; }
            if (!member.rented) return;
            member.rented = false;
            var pool = member.pool;
            projectile.SetActive(false); // Projectile OnDisable releases target/owner references.
            if (member.projectile is IPooledProjectile resettable) resettable.ResetForPool();
            foreach (var trail in member.trails) if(trail!=null) trail.Clear();
            foreach (var particles in member.particles) if(particles!=null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (!pool.buckets.TryGetValue(member.key, out var bucket) ||
                bucket.free.Count >= bucket.capacity || pool.stored >= pool.maximumStored)
            { pool.DiscardedCount++; Discard(projectile); return; }
            member.transform.SetParent(pool.storage, false);
            bucket.free.Push(member); pool.stored++;
        }

        static void Discard(GameObject projectile)
        {
            // Runtime destruction stays deferred; isolated Editor checks own scene instances only.
            if (Application.isPlaying) Destroy(projectile);
            else DestroyImmediate(projectile);
        }

        void OnDestroy()
        {
            if (Scenes.TryGetValue(gameObject.scene, out var owner) && owner == this)
                Scenes.Remove(gameObject.scene);
            buckets.Clear();
        }
    }

    // Runtime token only; not authored on prefabs, not saved to inventory/world-save data.
    public sealed class ProjectilePoolMember : MonoBehaviour
    {
        internal SceneProjectilePool pool;
        internal (GameObject, Type) key;
        internal Component projectile;
        internal bool rented;
        internal Vector3 originalScale;
        internal TrailRenderer[] trails;
        internal ParticleSystem[] particles;
    }
}
