#if UNITY_EDITOR
using System;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>Builds authored Lava ore prefabs from imported meshes and existing mineable prefabs.</summary>
    public static class MiningLavaOreModelsSetupMenu
    {
        private const string MeshFolder = "Assets/Ores/Models/Lava";
        private const string MaterialFolder = "Assets/GameData/Materials/LavaOres";
        private const string PrefabFolder = "Assets/Prefabs/Ores/Lava";
        private const string DataFolder = "Assets/GameData/Ores/Lava";
        private static readonly string[] Names = { "Basalt", "Ember Ore", "Molten Core" };
        private static readonly string[] Stems = { "Basalt", "Ember_Ore", "Molten_Core" };
        private static readonly string[] SurfaceNames = { "Crust", "Slate", "Ash", "Lava", "Glow" };
        private static readonly Color[,] Colors =
        {
            { new(.11f, .105f, .12f), new(.19f, .16f, .16f), new(.30f, .24f, .21f), new(.52f, .14f, .045f), new(.93f, .43f, .09f) },
            { new(.12f, .075f, .075f), new(.24f, .12f, .09f), new(.38f, .21f, .12f), new(.90f, .17f, .025f), new(1f, .55f, .12f) },
            { new(.105f, .06f, .065f), new(.24f, .105f, .06f), new(.36f, .17f, .08f), new(.93f, .28f, .035f), new(1f, .83f, .23f) }
        };

        [MenuItem("Mining Simulator/Portal/Build Lava Ore Models and Prefabs")]
        private static void Build()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                EditorUtility.DisplayDialog("Lava ores", "URP/Lit shader was not found.", "OK");
                return;
            }
            for (int i = 0; i < Names.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(
                        MeshFolder + "/" + Stems[i] + ".obj") == null ||
                    AssetDatabase.LoadAssetAtPath<OreData>(DataFolder + "/" + Names[i] + ".asset") == null)
                {
                    EditorUtility.DisplayDialog("Lava ores", "Missing OBJ or OreData: " + Names[i] +
                        ". Import the full ZIP and run Setup Lava World on the portal first.", "OK");
                    return;
                }
            }

            EnsureFolder("Assets/GameData", "Materials");
            EnsureFolder("Assets/GameData/Materials", "LavaOres");
            EnsureFolder("Assets/Prefabs/Ores", "Lava");

            int created = 0;
            for (int i = 0; i < Names.Length; i++)
            {
                OreData data = AssetDatabase.LoadAssetAtPath<OreData>(
                    DataFolder + "/" + Names[i] + ".asset");
                string output = PrefabFolder + "/" + Stems[i] + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(output);
                if (prefab == null)
                {
                    string sourcePath = AssetDatabase.GetAssetPath(data.Prefab);
                    GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                    GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                        MeshFolder + "/" + Stems[i] + ".obj");
                    if (source == null || !sourcePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        sourcePath = "Assets/Prefabs/Ores/Stone.prefab";
                        source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                    }
                    if (source == null || model == null)
                    {
                        Debug.LogError("Lava ore template or imported mesh is missing for " + Names[i]);
                        continue;
                    }
                    GameObject contents = PrefabUtility.LoadPrefabContents(sourcePath);
                    try
                    {
                        if (PrefabUtility.IsPartOfPrefabInstance(contents))
                            PrefabUtility.UnpackPrefabInstance(contents,
                                PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        contents.name = Names[i];
                        Ore ore = contents.GetComponentInChildren<Ore>(true);
                        if (ore == null)
                        {
                            Debug.LogError("Template has no Ore component: " + sourcePath);
                            continue;
                        }
                        foreach (MeshRenderer renderer in contents.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            // The template's world-space health bar uses a MeshRenderer too.
                            if (renderer.GetComponentInParent<OreHealthBar>() != null ||
                                renderer.GetComponentInParent<Canvas>() != null ||
                                renderer.GetComponent<TMPro.TMP_Text>() != null) continue;
                            renderer.enabled = false; // Preserve collider, health UI and mining scripts.
                        }

                        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, contents.scene);
                        visual.name = Names[i] + " Visual";
                        visual.transform.SetParent(contents.transform, false);
                        visual.transform.localPosition = new Vector3(0f, -.25f, 0f);
                        visual.transform.localRotation = Quaternion.identity;
                        visual.transform.localScale = Vector3.one;
                        foreach (MeshRenderer renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
                            AssignMaterials(renderer, i, lit);

                        SerializedObject oreFields = new(ore);
                        oreFields.FindProperty("data").objectReferenceValue = data;
                        oreFields.ApplyModifiedPropertiesWithoutUndo();
                        prefab = PrefabUtility.SaveAsPrefabAsset(contents, output);
                        if (prefab != null) created++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                }
                if (prefab == null) continue;
                Undo.RecordObject(data, "Assign Lava Ore Prefab");
                SerializedObject fields = new(data);
                fields.FindProperty("prefab").objectReferenceValue = prefab;
                // The delivered OBJs are Y-up, while the template FBXs use -90 degrees.
                fields.FindProperty("spawnRotationOffset").vector3Value = Vector3.zero;
                fields.FindProperty("spawnHeightOffset").floatValue = 0f;
                fields.ApplyModifiedProperties();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Lava ores", "Created " + created +
                " new mineable prefabs. Existing generated prefabs and edited materials were kept. " +
                "Inspect OreData prefab references and the generated Lava materials; save your project.", "OK");
        }

        private static void AssignMaterials(MeshRenderer renderer, int oreIndex, Shader lit)
        {
            int count = renderer.GetComponent<MeshFilter>()?.sharedMesh?.subMeshCount ?? 0;
            if (count == 0) return;
            Material[] imported = renderer.sharedMaterials;
            Material[] materials = new Material[count];
            for (int slot = 0; slot < count; slot++)
            {
                int surface = Mathf.Min(slot, SurfaceNames.Length - 1);
                if (slot < imported.Length && imported[slot] != null)
                {
                    for (int s = 0; s < SurfaceNames.Length; s++)
                        if (imported[slot].name.IndexOf(SurfaceNames[s],
                            StringComparison.OrdinalIgnoreCase) >= 0) { surface = s; break; }
                }
                materials[slot] = GetMaterial(oreIndex, surface, lit);
            }
            renderer.sharedMaterials = materials;
        }

        private static Material GetMaterial(int ore, int surface, Shader shader)
        {
            string path = MaterialFolder + "/" + Stems[ore] + "_" + SurfaceNames[surface] + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Material material = new(shader)
            {
                name = Stems[ore] + "_" + SurfaceNames[surface]
            };
            material.SetColor("_BaseColor", Colors[ore, surface]);
            material.SetFloat("_Smoothness", surface >= 3 ? .45f : .13f);
            if (surface >= 3)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Colors[ore, surface] *
                    (surface == 4 ? 2.8f : 1.6f));
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
