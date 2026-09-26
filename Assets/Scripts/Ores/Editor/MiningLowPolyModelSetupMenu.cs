#if UNITY_EDITOR
using System.Collections.Generic;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>Creates reusable mesh assets and plug-in replacement prefabs without editing a scene.</summary>
    public static class MiningLowPolyModelSetupMenu
    {
        private const string Folder = "Assets/Prefabs/Ores/LavaModels";

        [MenuItem("Mining Simulator/Models/Create Lava Ore Models")]
        private static void CreateModels()
        {
            EnsureFolder("Assets/Prefabs/Ores", "LavaModels");
            Mesh rock = SaveMesh(Folder + "/Faceted Lava Rock.asset", BuildRock());
            Mesh spike = SaveMesh(Folder + "/Lava Crystal.asset", BuildSpike());
            Material dry = SaveMaterial(Folder + "/Dry Lava Charcoal.mat", new Color(.13f, .11f, .12f));
            Material dryGlow = SaveMaterial(Folder + "/Dry Lava Ember.mat", new Color(1f, .2f, .025f), true);
            Material core = SaveMaterial(Folder + "/Lava Core Obsidian.mat", new Color(.085f, .035f, .075f));
            Material coreGlow = SaveMaterial(Folder + "/Lava Core Magma.mat", new Color(1f, .28f, .035f), true);

            CreateOre("Dry Lava", "Assets/Prefabs/Ores/Ember_Ore.prefab",
                rock, spike, dry, dryGlow, .88f, false);
            CreateOre("Lava Core", "Assets/Prefabs/Ores/Molten_Core.prefab",
                rock, spike, core, coreGlow, 1.05f, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Low Poly Models",
                "Dry Lava and Lava Core prefabs are assigned to their OreData assets.", "OK");
        }

        private static void CreateOre(string name, string template, Mesh rock, Mesh spike,
            Material stoneMaterial, Material glowMaterial, float size, bool core)
        {
            string path = Folder + "/" + name + ".prefab";
            OreData data = AssetDatabase.LoadAssetAtPath<OreData>(
                "Assets/GameData/Ores/Lava/" + name + ".asset");
            if (data == null) { Debug.LogError("Missing OreData: " + name); return; }
            GameObject root = PrefabUtility.LoadPrefabContents(template);
            try
            {
                root.name = name;
                // Keep existing gameplay, colliders, health bar and effects. Hide only the
                // old imported mesh. New visual uses the same root, so Ore.Initialize works.
                Bounds oldBounds = default;
                bool hasBounds = false;
                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.GetComponentInParent<Microlight.MicroBar.MicroBar>() != null ||
                        renderer.GetComponent<TMPro.TMP_Text>() != null) continue;
                    if (renderer.GetComponent<MeshFilter>() == null) continue;
                    if (!hasBounds) { oldBounds = renderer.bounds; hasBounds = true; }
                    else oldBounds.Encapsulate(renderer.bounds);
                    renderer.enabled = false;
                }

                Transform visual = new GameObject(name + " Low Poly Model").transform;
                visual.SetParent(root.transform, false);
                // Keep the prefab's authored collider and spawn orientation. Rotate the
                // new upright mesh back so it stays upright after the existing spawn offset.
                visual.localRotation = Quaternion.Inverse(Quaternion.Euler(data.SpawnRotationOffset));
                visual.localPosition = hasBounds
                    ? root.transform.InverseTransformPoint(oldBounds.center) : Vector3.zero;
                float originalSize = hasBounds ? Mathf.Max(oldBounds.size.x, oldBounds.size.y,
                    oldBounds.size.z) : 1f;
                float scale = size * originalSize /
                    Mathf.Max(.001f, Mathf.Max(Mathf.Abs(root.transform.lossyScale.x),
                        Mathf.Abs(root.transform.lossyScale.y), Mathf.Abs(root.transform.lossyScale.z)));
                visual.localScale = Vector3.one * Mathf.Max(.1f, scale);
                AddMesh(visual, "Faceted Basalt Shell", rock, stoneMaterial,
                    Vector3.zero, Vector3.one);
                int count = core ? 7 : 4;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * Mathf.PI * 2f / count;
                    float radius = core ? .23f : .29f;
                    Vector3 position = new Vector3(Mathf.Cos(angle) * radius,
                        core ? .25f : .15f, Mathf.Sin(angle) * radius);
                    Vector3 dimension = core ? new Vector3(.12f, .5f + .12f * (i % 3), .12f)
                        : new Vector3(.05f, .27f, .05f);
                    AddMesh(visual, core ? "Glowing Magma Spine" : "Ember Crack",
                        spike, glowMaterial, position, dimension);
                }
                if (core)
                    AddMesh(visual, "Molten Core", spike, glowMaterial,
                        new Vector3(0f, .32f, 0f), new Vector3(.32f, .76f, .32f));
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                    throw new System.InvalidOperationException("Could not save " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            SerializedObject serialized = new(data);
            serialized.FindProperty("prefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        private static void AddMesh(Transform parent, string name, Mesh mesh, Material material,
            Vector3 position, Vector3 size)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Mesh SaveMesh(string path, Mesh created)
        {
            Mesh old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null) { Object.DestroyImmediate(created); return old; }
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static Material SaveMaterial(string path, Color color, bool emissive = false)
        {
            Material old = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (old != null) return old;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.5f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Mesh BuildRock()
        {
            float phi = (1f + Mathf.Sqrt(5f)) * .5f;
            Vector3[] points = {
                new(-1, phi, 0), new(1, phi, 0), new(-1, -phi, 0), new(1, -phi, 0),
                new(0, -1, phi), new(0, 1, phi), new(0, -1, -phi), new(0, 1, -phi),
                new(phi, 0, -1), new(phi, 0, 1), new(-phi, 0, -1), new(-phi, 0, 1)
            };
            int[] faces = {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };
            Vector3[] vertices = new Vector3[faces.Length];
            for (int i = 0; i < faces.Length; i++)
            {
                Vector3 point = points[faces[i]].normalized * .5f;
                point.y *= .75f;
                vertices[i] = point;
            }
            Mesh mesh = new() { name = "Faceted Lava Rock", vertices = vertices };
            int[] indices = new int[vertices.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildSpike()
        {
            List<Vector3> vertices = new();
            List<int> triangles = new();
            const int sides = 5;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float b = (i + 1) * Mathf.PI * 2f / sides;
                vertices.Add(new Vector3(Mathf.Cos(a) * .5f, -.5f, Mathf.Sin(a) * .5f));
                vertices.Add(new Vector3(Mathf.Cos(b) * .5f, -.5f, Mathf.Sin(b) * .5f));
                vertices.Add(new Vector3(0f, .5f, 0f));
                int offset = vertices.Count - 3;
                triangles.Add(offset); triangles.Add(offset + 2); triangles.Add(offset + 1);
            }
            Mesh mesh = new() { name = "Lava Crystal", vertices = vertices.ToArray(), triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
