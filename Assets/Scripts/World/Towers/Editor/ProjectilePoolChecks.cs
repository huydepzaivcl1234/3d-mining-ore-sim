using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    public static class ProjectilePoolChecks
    {
        public static string Run()
        {
            if (UnityEditor.EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before isolated checks.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var shots = new List<TowerProjectile>();
                for (int i = 0; i < 32; i++)
                {
                    var shot = SceneProjectilePool.Rent<TowerProjectile>(null, Vector3.zero, Quaternion.identity, scene, 32);
                    Require(!shot.gameObject.activeSelf, "Rent must be inactive");
                    shot.gameObject.SetActive(true); shots.Add(shot);
                }
                var pool = shots[0].GetComponentInParent<SceneProjectilePool>();
                for (int batch = 0; batch < 20; batch++)
                {
                    foreach (var shot in shots)
                    {
                        SceneProjectilePool.Release(shot.gameObject);
                        SceneProjectilePool.Release(shot.gameObject);
                    }
                    Require(pool.StoredCount == 32, "Duplicate release duplicated entries");
                    shots.Clear();
                    for (int i = 0; i < 32; i++)
                    {
                        var shot = SceneProjectilePool.Rent<TowerProjectile>(null, Vector3.right, Quaternion.identity, scene, 32);
                        Require(!shot.HasPlayerOwner, "Retained ownership");
                        Require(shot.transform.position == Vector3.right, "World position not reset");
                        shot.gameObject.SetActive(true); shots.Add(shot);
                    }
                }
                Require(pool.CreatedCount == 32 && pool.ReusedCount == 640, "Unexpected creation during reuse");
                var homing = SceneProjectilePool.Rent<HomingMonsterProjectile>(null, Vector3.zero, Quaternion.identity, scene, 4);
                var target = new GameObject("Temporary target");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(target, scene);
                homing.Launch(null, target.transform); homing.gameObject.SetActive(true);
                SceneProjectilePool.Release(homing.gameObject);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Require(typeof(HomingMonsterProjectile).GetField("victim", flags).GetValue(homing) == null, "Retained target");
                Require(!(bool)typeof(HomingMonsterProjectile).GetField("launched", flags).GetValue(homing), "Retained launch state");
                var overflow = new List<HomingMonsterProjectile>();
                for (int i = 0; i < 8; i++) overflow.Add(SceneProjectilePool.Rent<HomingMonsterProjectile>(null, Vector3.zero, Quaternion.identity, scene, 4));
                foreach (var shot in overflow) SceneProjectilePool.Release(shot.gameObject);
                Require(pool.DiscardedCount == 4 && pool.StoredCount == 4, "Per-bucket bound failed");
                foreach (var shot in shots) SceneProjectilePool.Release(shot.gameObject);
                Require(pool.StoredCount == 36, "Unexpected stored total");
                // A new typed bucket cannot grow total retained storage above the scene limit.
                var extra = new List<Transform>();
                for (int i = 0; i < 300; i++) extra.Add(SceneProjectilePool.Rent<Transform>(null, Vector3.zero, Quaternion.identity, scene, 300));
                foreach (var shot in extra) SceneProjectilePool.Release(shot.gameObject);
                Require(pool.StoredCount == 256 && pool.DiscardedCount == 84, "Scene-wide bound failed");
                return "PASS: 32 shots reused 640 times; inactive rent, duplicate release, pose reset, owner/target reset, typed buckets; overflow discarded, total retained <=256. No scene or save written. Not an FPS benchmark.";
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
