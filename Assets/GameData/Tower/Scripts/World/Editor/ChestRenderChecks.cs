using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Isolated render workload. Never enters the authored scene or writes a save.</summary>
    public static class ChestRenderChecks
    {
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        static void Invoke(TreasureChest chest, string name, params object[] args) =>
            typeof(TreasureChest).GetMethod(name, Private).Invoke(chest, args);

        public static string Run(bool cannonLod = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var original = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            RenderTexture target = null;
            Texture2D pixels = null;
            TreasureChestData definition = null;
            var previousTarget = RenderTexture.active;
            try
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Base/BaseTreasureChest.prefab");
                var go = UnityEngine.Object.Instantiate(source);
                SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.position = Vector3.zero;
                var chest = go.GetComponent<TreasureChest>();
                var serialized = new SerializedObject(chest);
                definition = UnityEngine.Object.Instantiate((TreasureChestData)serialized.FindProperty("data").objectReferenceValue);
                serialized.FindProperty("data").objectReferenceValue = definition;
                serialized.FindProperty("persistProgress").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                Invoke(chest, "Awake");
                chest.Health.ConfigureSpawnHealth(definition.baseHealth);
                Invoke(chest, "BuildVisualPivot");
                Invoke(chest, "BuildBreakVisuals");
                int baselineVertices = Vertices(go);
                definition.hideClosedContents = true;
                Invoke(chest, "SampleLid", false, 1f);
                int closedVertices = Vertices(go);
                Require(closedVertices < baselineVertices, "Closed contents not hidden");
                Invoke(chest, "SampleLid", true, 0f);
                Require(Vertices(go) == baselineVertices, "Opening failed to restore contents");
                Invoke(chest, "SampleLid", false, 1f);
                Invoke(chest, "SetBrokenVisual", false);
                Require(Vertices(go) == closedVertices, "Repair restored hidden interior");
                definition.hideClosedContents = false;
                Invoke(chest, "SampleLid", false, 1f);
                Require(Vertices(go) == baselineVertices, "Designer opt-out failed");

                var cannon = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Tower/Cannon/CannonTower.prefab");
                var groups = new List<LODGroup>();
                for (int i = 0; i < 30; i++)
                {
                    var tower = UnityEngine.Object.Instantiate(cannon);
                    SceneManager.MoveGameObjectToScene(tower, scene);
                    tower.transform.position = new Vector3((i % 6 - 2.5f) * 1.7f, 0, 4 + i / 6 * 1.7f);
                    foreach (var behaviour in tower.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
                    var group=tower.GetComponent<LODGroup>();
                    if(group!=null){groups.Add(group);if(cannonLod)group.ForceLOD(0);}
                }
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
                var cameraObject = new GameObject("Render check camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false; camera.cullingMask = 1 << 30;
                cameraObject.transform.position = new Vector3(0, 6, -8);
                cameraObject.transform.LookAt(new Vector3(0, 1, 4));
                var lightObject = new GameObject("Render check light", typeof(Light));
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                var light = lightObject.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1;
                light.cullingMask = 1 << 30; lightObject.transform.rotation = Quaternion.Euler(40, -30, 0);
                target = new RenderTexture(960, 540, 24); camera.targetTexture = target;
                pixels = new Texture2D(1, 1, TextureFormat.RGB24, false);
                if(cannonLod)
                {
                    definition.hideClosedContents=true; Invoke(chest,"SampleLid",false,1f);
                    double full=Measure(camera,pixels,target);
                    Capture(camera,target,"CannonFull");
                    foreach(var group in groups)group.ForceLOD(1);
                    double reduced=Measure(camera,pixels,target);
                    Capture(camera,target,"CannonReduced");
                    foreach(var group in groups)group.ForceLOD(0);
                    double restored=Measure(camera,pixels,target);
                    return $"PASS: 30-cannon render-only median full {full:F3} ms, LOD1 {reduced:F3} ms, restored {restored:F3} ms. Not gameplay FPS. Captured full/reduced views.";
                }
                double baseline = Measure(camera, pixels, target);
                definition.hideClosedContents = true;
                Invoke(chest, "SampleLid", false, 1f);
                double candidate = Measure(camera, pixels, target);
                definition.hideClosedContents = false;
                Invoke(chest, "SampleLid", false, 1f);
                double restoredBaseline = Measure(camera, pixels, target);
                return $"PASS: open/close, repair visibility, opt-out. Chest enabled vertices {baselineVertices} -> {closedVertices}. " +
                    $"30-cannon offscreen Render + GPU readback median: baseline {baseline:F3} ms, candidate {candidate:F3} ms, restored baseline {restoredBaseline:F3} ms. " +
                    "Editor microbenchmark only, not gameplay FPS; animation/AI/target scans excluded. Scene/save unchanged.";
            }
            finally
            {
                RenderTexture.active = previousTarget;
                EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(original);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        static void Capture(Camera camera,RenderTexture target,string name)
        {
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                System.IO.File.WriteAllBytes("Docs/AI/"+name+".png",image.EncodeToPNG());
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }

        static double Measure(Camera camera, Texture2D pixels, RenderTexture target)
        {
            var samples = new List<double>();
            for (int i = 0; i < 13; i++)
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
                clock.Stop(); if (i >= 3) samples.Add(clock.Elapsed.TotalMilliseconds);
            }
            samples.Sort(); return (samples[4] + samples[5]) * .5;
        }
        static int Vertices(GameObject go)
        {
            int total = 0;
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<Renderer>();
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy && filter.sharedMesh != null)
                    total += filter.sharedMesh.vertexCount;
            }
            return total;
        }
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
