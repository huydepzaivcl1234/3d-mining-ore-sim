using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.EditorTools
{
    /// <summary>Renders prefab geometry only; never instantiates gameplay in the user's scene.</summary>
    public static class TransparentPrefabIcon
    {
        [MenuItem("Assets/Mining Simulator/Export Transparent Icon", true)]
        private static bool CanExport() => Selection.activeObject is GameObject &&
            AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".prefab") && !EditorApplication.isPlaying;

        [MenuItem("Assets/Mining Simulator/Export Transparent Icon")]
        private static void ExportSelected() => ExportSelected(1.3f);

        [MenuItem("Assets/Mining Simulator/Export Transparent Icon (Bright)", true)]
        private static bool CanExportBright() => CanExport();

        [MenuItem("Assets/Mining Simulator/Export Transparent Icon (Bright)")]
        private static void ExportBright() => ExportSelected(5f);

        private static void ExportSelected(float lighting)
        {
            var prefab = (GameObject)Selection.activeObject;
            string path = EditorUtility.SaveFilePanelInProject("Export transparent icon",
                prefab.name, "png", "Choose the PNG destination.");
            if (string.IsNullOrEmpty(path)) return;
            if (File.Exists(path) && !EditorUtility.DisplayDialog("Replace icon?",
                path + " already exists. Its GUID will be preserved.", "Replace", "Cancel")) return;
            Export(prefab, path, 256, lighting);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Sprite>(path));
        }

        public static void Export(GameObject prefab, string path, int size = 256, float lighting = 1.3f)
        {
            if (prefab == null || !AssetDatabase.GetAssetPath(prefab).EndsWith(".prefab"))
                throw new ArgumentException("Select a prefab asset.");
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..") ||
                !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Output must be a PNG inside Assets.");
            size = Mathf.Clamp(size, 64, 2048);
            var preview = new PreviewRenderUtility();
            var bakedMeshes = new List<Mesh>();
            Texture2D image = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                // Copy renderers and their current posed meshes rather than scripts, UI or colliders.
                var source = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
                var geometry = new GameObject("Icon geometry");
                try
                {
                    var animator = source.GetComponentInChildren<Animator>();
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                        var idle = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c =>
                            c.name.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0);
                        if (idle != null) idle.SampleAnimation(animator.gameObject, 0);
                    }
                    foreach (var renderer in source.GetComponentsInChildren<Renderer>())
                    {
                        if (!renderer.enabled || renderer is SpriteRenderer ||
                            renderer.GetComponentInParent<Canvas>() != null ||
                            renderer.GetComponentInParent<TMPro.TMP_Text>() != null) continue;
                        Mesh mesh = null;
                        if (renderer is SkinnedMeshRenderer skin)
                        {
                            mesh = new Mesh();
                            bakedMeshes.Add(mesh);
                            skin.BakeMesh(mesh);
                        }
                        else if (renderer is MeshRenderer)
                            mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                        if (mesh == null) continue;
                        var copy = new GameObject(renderer.name, typeof(MeshFilter), typeof(MeshRenderer));
                        copy.transform.SetParent(geometry.transform, false);
                        copy.transform.SetPositionAndRotation(source.transform.InverseTransformPoint(renderer.transform.position),
                            Quaternion.Inverse(source.transform.rotation) * renderer.transform.rotation);
                        copy.transform.localScale = renderer.transform.lossyScale;
                        copy.GetComponent<MeshFilter>().sharedMesh = mesh;
                        copy.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                        if (renderer is SkinnedMeshRenderer) mesh.hideFlags = HideFlags.HideAndDontSave;
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(source); }
                preview.AddSingleGO(geometry);
                geometry.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var renderers = geometry.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length == 0) throw new InvalidOperationException("No visible model mesh.");
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                preview.BeginPreview(new Rect(0, 0, size, size), GUIStyle.none);
                var camera = preview.camera;
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.magnitude * 1.12f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.nearClipPlane = .01f;
                camera.farClipPlane = Mathf.Max(100f, bounds.size.magnitude * 10);
                Vector3 direction = new Vector3(.4f, .25f, 1).normalized;
                camera.transform.position = bounds.center + direction * Mathf.Max(10, bounds.size.magnitude * 3);
                camera.transform.LookAt(bounds.center);
                preview.lights[0].intensity = Mathf.Clamp(lighting, .1f, 10f);
                preview.lights[0].transform.rotation = Quaternion.Euler(35, 205, 0);
                preview.lights[1].intensity = preview.lights[0].intensity * .6f;
                preview.lights[1].transform.rotation = Quaternion.Euler(10, 135, 0);
                preview.ambientColor = Color.white;
                preview.Render(true);
                var texture = (RenderTexture)preview.EndPreview();
                RenderTexture.active = texture;
                // Preview targets use physical pixels on high-DPI monitors.
                image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                image.Apply();
                if (image.GetPixel(0, 0).a > .01f)
                    throw new InvalidOperationException("Renderer did not preserve alpha; PNG not written.");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                // Preview cleanup owns the isolated geometry and camera; destroy baked meshes too.
                foreach (var mesh in bakedMeshes) UnityEngine.Object.DestroyImmediate(mesh);
                preview.Cleanup();
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = size;
            importer.SaveAndReimport();
        }
    }
}
