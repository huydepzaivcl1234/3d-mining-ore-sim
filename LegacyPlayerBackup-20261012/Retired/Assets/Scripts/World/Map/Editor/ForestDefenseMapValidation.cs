using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    public static class ForestDefenseMapValidation
    {
        public static string Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Validation is isolated Edit Mode only.");
            var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(ForestDefenseMapBuilder.Output + "/MosswoodDefenseMap.prefab"), scene);
                var obstacles = root.transform.Find("Forest - LOD and distance culling").GetComponentsInChildren<Collider>();
                int checks = 0;
                // Test actual enabled collider bounds, not object pivots.
                foreach (var collider in obstacles)
                {
                    if (!collider.enabled || collider.isTrigger) continue;
                    if (collider.bounds.SqrDistance(Vector3.zero) < 13 * 13)
                        throw new Exception("Obstacle enters 13m combat clearance: " + collider.name);
                    for (int direction = 0; direction < 4; direction++)
                    {
                        Vector3 heading = Quaternion.Euler(0, direction * 90, 0) * Vector3.forward;
                        for (int metre = 7; metre <= 30; metre++)
                        {
                            Vector3 point = heading * metre + Vector3.up;
                            if (collider.bounds.SqrDistance(point) < .8f * .8f)
                                throw new Exception("Collider blocks approach " + direction + ": " + collider.name);
                            checks++;
                        }
                    }
                }
                var renderers = root.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || material.shader.name.Contains("Error"))
                            throw new Exception("Invalid render material on " + renderer.name);
                if (root.GetComponentsInChildren<ParticleSystem>().Length != 0)
                    throw new Exception("Unexpected particles in map.");
                TestCameraProxy(scene);
                TestVisibility(root,scene);
                TestNavigation(root,scene);
                return "PASS: " + checks + " approach/collider checks; 13m combat clearance; 4 real A* routes; culling far/near/restore; valid materials; no particles; respawn camera reused 10 times.";
            }
            finally
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene,true);
            }
        }

        private static void TestVisibility(GameObject root,UnityEngine.SceneManagement.Scene scene)
        {
            var culling = root.GetComponent<ForestMapVisibility>();
            var owner = new GameObject("Culling Fixture Camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner,scene);
            var camera = owner.AddComponent<Camera>();
            const BindingFlags flags = BindingFlags.Instance|BindingFlags.NonPublic;
            var type = typeof(ForestMapVisibility);
            type.GetMethod("OnEnable",flags).Invoke(culling,null);
            type.GetField("view",flags).SetValue(culling,camera);
            owner.transform.position = Vector3.right*1000;
            type.GetMethod("LateUpdate",flags).Invoke(culling,null);
            var renderers = culling.decoration.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                if (!renderer.forceRenderingOff) throw new Exception("Far decoration not culled.");
            owner.transform.position = Vector3.zero;
            type.GetField("next",flags).SetValue(culling,0f);
            type.GetMethod("LateUpdate",flags).Invoke(culling,null);
            foreach (var renderer in renderers)
                if (renderer.forceRenderingOff) throw new Exception("Near decoration not restored.");
            type.GetMethod("OnDisable",flags).Invoke(culling,null);
            camera.enabled = false;
        }

        private static void TestNavigation(GameObject root,UnityEngine.SceneManagement.Scene scene)
        {
            Vector3 offset = new Vector3(10000,0,10000);
            root.transform.position = offset;
            var owner = new GameObject("Isolated Navigation Grid");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner,scene);
            var grid = owner.AddComponent<WorldNavigationGrid>();
            grid.ConfigureArea(offset,new Vector2(80,80));
            grid.Configure(1,.55f,1.8f,45,.5f,~0,512,256);
            Physics.SyncTransforms(); grid.Rebake();
            for (int i=0;i<4;i++)
            {
                Vector3 heading = Quaternion.Euler(0,i*90,0)*Vector3.forward;
                var route = new List<Vector3>();
                if (!grid.TryFindPath(offset+heading*30,offset+heading*4,route) || route.Count==0)
                    throw new Exception("A* cannot traverse approach " + i);
            }
        }

        private static void TestCameraProxy(UnityEngine.SceneManagement.Scene scene)
        {
            var player = new GameObject("Isolated Respawn Fixture");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(player, scene);
            player.transform.position = Vector3.right * 10000;
            player.AddComponent<Animator>(); player.AddComponent<CharacterController>();
            player.AddComponent<MiningCharacterHealth>();
            var ragdoll = player.AddComponent<PlayerKnockbackRagdoll>();
            var respawn = player.AddComponent<PlayerDeathRespawn>();
            var view = new GameObject("Fixture Camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(view, scene);
            var camera = view.AddComponent<Camera>(); camera.enabled = false; view.transform.position = player.transform.position + new Vector3(0,3,-5);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(PlayerDeathRespawn);
            type.GetField("spectatorCamera",flags).SetValue(respawn,camera);
            type.GetField("deathRagdoll",flags).SetValue(respawn,ragdoll);
            type.GetField("playerBody",flags).SetValue(respawn,player.GetComponent<CharacterController>());
            GameObject originalProxy = null;
            var timing = System.Diagnostics.Stopwatch.StartNew();
            for (int i=0;i<10;i++)
            {
                type.GetMethod("BeginCamera",flags).Invoke(respawn,null);
                var proxy = (GameObject)type.GetField("cameraProxy",flags).GetValue(respawn);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(proxy,scene);
                if (i==0) originalProxy=proxy;
                if (originalProxy!=proxy) throw new Exception("Camera proxy recreated.");
                type.GetMethod("RestoreControls",flags).Invoke(respawn,null);
                if (proxy.GetComponent<CharacterController>().enabled) throw new Exception("Camera collision remains active after respawn.");
            }
            timing.Stop();
            Debug.Log("Isolated camera reset 10 cycles: " + timing.Elapsed.TotalMilliseconds.ToString("F2") + "ms. This is not full gameplay frame timing.");
        }
    }
}
