#if UNITY_EDITOR
using System.Collections.Generic;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>Author scene references and editable Lava ore entries on the existing spawner.</summary>
    public static class MiningLavaWorldSetupMenu
    {
        private const string Folder = "Assets/GameData/Ores/Lava";
        private static readonly string[] Templates = { "Stone", "Iron", "Netherite" };
        private static readonly string[] Names = { "Basalt", "Ember Ore", "Molten Core" };
        private static readonly OreKind[] Kinds = { OreKind.Basalt, OreKind.EmberOre, OreKind.MoltenCore };
        private static readonly int[] Powers = { 1, 8, 35 };
        private static readonly float[] Chances = { 75f, 20f, 5f };

        [MenuItem("Mining Simulator/Portal/Setup Lava World On Selected Gate")]
        private static void Setup()
        {
            GameObject gate = Selection.activeGameObject;
            if (gate == null || !gate.scene.IsValid())
            {
                EditorUtility.DisplayDialog("Lava World", "Select your existing portal in the Hierarchy.", "OK");
                return;
            }
            var spawners = Object.FindObjectsByType<OreSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (spawners.Length != 1)
            {
                EditorUtility.DisplayDialog("Lava World", "Expected one scene OreSpawner. Keep the existing Ore System.", "OK");
                return;
            }
            var radial = gate.GetComponent<MiningDynamicRadialMaskTransition>();
            if (radial == null || gate.GetComponent<MiningPortalPreviewGate>() == null)
            {
                EditorUtility.DisplayDialog("Lava World", "First run Mining Simulator/Portal/Setup Enter Preview On Selected Gate.", "OK");
                return;
            }
            OreData[] ores = CreateOres();
            if (ores == null) return;
            var serializedSpawner = new SerializedObject(spawners[0]);
            SerializedProperty lavaSettings = serializedSpawner.FindProperty("lavaSpawnData");
            if (lavaSettings != null && lavaSettings.objectReferenceValue == null)
            {
                OreSpawnData groundSettings = spawners[0].SpawnData;
                if (groundSettings == null)
                {
                    EditorUtility.DisplayDialog("Lava World",
                        "Assign Ground Spawn Data on Ore System before creating Lava spawn settings.", "OK");
                    return;
                }
                const string spawnFolder = "Assets/GameData/Spawning";
                const string lavaPath = spawnFolder + "/LavaOreSpawnData.asset";
                if (!AssetDatabase.IsValidFolder(spawnFolder))
                    AssetDatabase.CreateFolder("Assets/GameData", "Spawning");
                OreSpawnData lavaData = AssetDatabase.LoadAssetAtPath<OreSpawnData>(lavaPath);
                if (lavaData == null)
                {
                    lavaData = Object.Instantiate(groundSettings);
                    lavaData.name = "LavaOreSpawnData";
                    // Ore entries remain scene editable on Ore System > Lava World.
                    SerializedObject lavaFields = new(lavaData);
                    lavaFields.FindProperty("oreSpawnTable").ClearArray();
                    lavaFields.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.CreateAsset(lavaData, lavaPath);
                    AssetDatabase.SaveAssets();
                }
                lavaSettings.objectReferenceValue = lavaData;
            }
            SerializedProperty table = serializedSpawner.FindProperty("lavaOreSpawnTable");
            if (table.arraySize == 0)
            {
                table.arraySize = ores.Length;
                for (int i = 0; i < ores.Length; i++)
                {
                    table.GetArrayElementAtIndex(i).FindPropertyRelative("ore").objectReferenceValue = ores[i];
                    table.GetArrayElementAtIndex(i).FindPropertyRelative("spawnChancePercent").floatValue = Chances[i];
                }
            }
            serializedSpawner.ApplyModifiedProperties();
            var world = gate.GetComponent<MiningLavaWorldController>();
            if (world == null) world = Undo.AddComponent<MiningLavaWorldController>(gate);
            var serializedWorld = new SerializedObject(world);
            serializedWorld.FindProperty("oreSpawner").objectReferenceValue = spawners[0];
            serializedWorld.FindProperty("transition").objectReferenceValue = radial;
            SerializedProperty progression = serializedWorld.FindProperty("minerProgression");
            SerializedProperty wallet = serializedWorld.FindProperty("wallet");
            SerializedProperty managerRef = serializedWorld.FindProperty("audioManager");
            MiningAudioManager audioManager = null;
            foreach (Component component in Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component == null || component.gameObject.scene != gate.scene) continue;
                if (component is NpcProgressionSystem level && progression.objectReferenceValue == null)
                    progression.objectReferenceValue = level;
                if (component is PlayerWallet sceneWallet && wallet.objectReferenceValue == null)
                    wallet.objectReferenceValue = sceneWallet;
                if (component is MiningAudioManager sceneManager) audioManager = sceneManager;
            }
            if (managerRef.objectReferenceValue == null)
                managerRef.objectReferenceValue = audioManager;
            audioManager ??= managerRef.objectReferenceValue as MiningAudioManager;
            if (audioManager != null)
            {
                SerializedObject serializedTransition = new(radial);
                SerializedProperty transitionManager = serializedTransition.FindProperty("audioManager");
                transitionManager.objectReferenceValue = audioManager;
                MiningAudioData data = audioManager.AudioData;
                if (data != null)
                {
                    SerializedObject audioSettings = new(data);
                    SerializedProperty lavaClip = audioSettings.FindProperty("lavaWorldAmbience");
                    if (lavaClip.objectReferenceValue == null)
                        lavaClip.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(
                            "Assets/Audio/Lava/LavaAmbience.wav");
                    MigratePortalClip(serializedTransition, audioSettings,
                        "whooshClip", "lavaPortalWhooshSfx");
                    MigratePortalClip(serializedTransition, audioSettings,
                        "impactClip", "lavaPortalImpactSfx");
                    SerializedProperty whoosh = audioSettings.FindProperty("lavaPortalWhooshSfx");
                    if (whoosh.objectReferenceValue == null)
                        whoosh.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(
                            "Assets/Audio/Lava/PortalWhoosh.wav");
                    SerializedProperty impact = audioSettings.FindProperty("lavaPortalImpactSfx");
                    if (impact.objectReferenceValue == null)
                        impact.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(
                            "Assets/Audio/Lava/PortalImpact.wav");
                    audioSettings.ApplyModifiedProperties();
                    AssetDatabase.SaveAssetIfDirty(data);
                }
                serializedTransition.ApplyModifiedProperties();
            }
            // Reuse the authored Ground. The sample LavaWorldBuilder creates a second box,
            // so only import its shader and make a material asset for this scene surface.
            Transform ground = GameObject.Find("Ground")?.transform;
            if (ground != null)
            {
                SerializedProperty mesh = serializedWorld.FindProperty("groundRenderer");
                SerializedProperty terrain = serializedWorld.FindProperty("groundTerrain");
                Terrain groundTerrain = ground.GetComponent<Terrain>() ??
                    ground.GetComponentInChildren<Terrain>(true);
                if (groundTerrain != null)
                {
                    if (terrain.objectReferenceValue == null)
                        terrain.objectReferenceValue = groundTerrain;
                }
                else if (mesh.objectReferenceValue == null)
                    mesh.objectReferenceValue = ground.GetComponent<Renderer>() ??
                        ground.GetComponentInChildren<Renderer>(true);
            }
            SerializedProperty lavaMaterial = serializedWorld.FindProperty("lavaGroundMaterial");
            if (lavaMaterial.objectReferenceValue == null)
                lavaMaterial.objectReferenceValue = EnsureLavaMaterial();
            SerializedProperty treeRoots = serializedWorld.FindProperty("surroundingTrees");
            SerializedProperty terrains = serializedWorld.FindProperty("groundTerrains");
            if (terrains.arraySize == 0)
            {
                var sceneTerrains = new List<Terrain>();
                foreach (Terrain terrain in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (terrain != null && terrain.gameObject.scene == gate.scene) sceneTerrains.Add(terrain);
                terrains.arraySize = sceneTerrains.Count;
                for (int i = 0; i < sceneTerrains.Count; i++)
                    terrains.GetArrayElementAtIndex(i).objectReferenceValue = sceneTerrains[i];
            }
            if (treeRoots.arraySize == 0)
            {
                var found = new List<GameObject>();
                foreach (Transform candidate in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (candidate == null || candidate == gate.transform || !candidate.gameObject.scene.IsValid() || !IsTreeName(candidate.name)) continue;
                    bool namedAncestor = false;
                    for (Transform parent = candidate.parent; parent != null; parent = parent.parent)
                        if (IsTreeName(parent.name)) { namedAncestor = true; break; }
                    if (!namedAncestor) found.Add(candidate.gameObject);
                }
                treeRoots.arraySize = found.Count;
                for (int i = 0; i < found.Count; i++)
                    treeRoots.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }
            serializedWorld.ApplyModifiedProperties();
            // Remove only the unused source created by the previous Lava setup, never a user source.
            Transform obsoleteSource = gate.transform.Find("Lava World Ambience");
            if (obsoleteSource != null && obsoleteSource.GetComponents<Component>().Length == 2 &&
                obsoleteSource.GetComponent<AudioSource>() is AudioSource oldSource &&
                oldSource.clip == null)
                Undo.DestroyObjectImmediate(obsoleteSource.gameObject);
            EditorSceneManager.MarkSceneDirty(gate.scene);
            Selection.activeGameObject = gate;
            EditorUtility.DisplayDialog("Lava World", "Inspect the portal's Level, Power and Money requirements; Audio > MiningAudioData now owns all Lava ambience and portal SFX. Save your scene and audio asset.", "OK");
        }

        private static void MigratePortalClip(SerializedObject transition,
            SerializedObject audioSettings, string oldField, string newField)
        {
            SerializedProperty oldClip = transition.FindProperty(oldField);
            SerializedProperty newClip = audioSettings.FindProperty(newField);
            if (oldClip == null || newClip == null || oldClip.objectReferenceValue == null) return;
            if (newClip.objectReferenceValue == null)
                newClip.objectReferenceValue = oldClip.objectReferenceValue;
            oldClip.objectReferenceValue = null;
        }

        private static bool IsTreeName(string value)
        {
            string name = value.ToLowerInvariant();
            return name.StartsWith("tree") || name.StartsWith("pine") || name.Contains(" trees") || name.Contains("forest");
        }

        private static Material EnsureLavaMaterial()
        {
            const string path = "Assets/GameData/Materials/LavaFloor.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/Shaders/StylizedLavaFloor_URP.shader");
            if (shader == null)
            {
                Debug.LogWarning("Lava shader is missing. Import StylizedLavaFloor_URP.shader first.");
                return null;
            }
            const string folder = "Assets/GameData/Materials";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/GameData", "Materials");
            Material material = new(shader) { name = "LavaFloor" };
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static OreData[] CreateOres()
        {
            OreData[] ores = new OreData[Names.Length];
            for (int i = 0; i < ores.Length; i++)
            {
                string path = Folder + "/" + Names[i] + ".asset";
                ores[i] = AssetDatabase.LoadAssetAtPath<OreData>(path);
                if (ores[i] != null) continue;
                OreData template = AssetDatabase.LoadAssetAtPath<OreData>("Assets/GameData/Ores/" + Templates[i] + ".asset");
                if (template == null || template.Prefab == null)
                {
                    EditorUtility.DisplayDialog("Lava World", "Missing template ore or prefab: " + Templates[i], "OK");
                    return null;
                }
            }
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/GameData/Ores", "Lava");
            for (int i = 0; i < ores.Length; i++)
            {
                if (ores[i] != null) continue;
                string path = Folder + "/" + Names[i] + ".asset";
                OreData template = AssetDatabase.LoadAssetAtPath<OreData>("Assets/GameData/Ores/" + Templates[i] + ".asset");
                OreData data = Object.Instantiate(template);
                data.name = Names[i];
                var serialized = new SerializedObject(data);
                serialized.FindProperty("kind").enumValueIndex = (int)Kinds[i];
                serialized.FindProperty("displayName").stringValue = Names[i];
                serialized.FindProperty("description").stringValue = "Lava World ore. Assign a unique prefab to change its appearance.";
                serialized.FindProperty("miningPowerRequired").intValue = Powers[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(data, path);
                ores[i] = data;
            }
            AssetDatabase.SaveAssets();
            return ores;
        }
    }
}
#endif
