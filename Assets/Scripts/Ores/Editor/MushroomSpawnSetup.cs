#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Microlight.MicroBar;
using TMPro;

namespace MiningSimulator.Editor
{
    public static class MushroomSpawnSetup
    {
        private const string Folder = "Assets/Prefabs/Monsters";
        [MenuItem("Mining Simulator/Setup/Create Mushroom Spawn Zone %&#6")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MonsterPack3D/Prefabs/Mushroom.prefab");
            if (source == null) { Debug.LogError("Mushroom pack prefab missing."); return; }
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Monsters");
            string controllerPath = Folder + "/Mushroom.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                foreach (string name in new[] { "Idle", "Walk", "Headbutt", "Damage", "Down" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        "Assets/MonsterPack3D/Animations/Mushroom/SA_Mushroom_" + name + ".anim");
                    if (clip == null) { Debug.LogError("Missing Mushroom clip: " + name); return; }
                    var state = controller.layers[0].stateMachine.AddState(name);
                    state.motion = clip;
                    if (name == "Idle") controller.layers[0].stateMachine.defaultState = state;
                }
            }
            string prefabPath = Folder + "/MushroomMonster.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var playerInput = Object.FindFirstObjectByType<PlayerCombatInput>(FindObjectsInactive.Include);
            var player = playerInput != null ? playerInput.GetComponent<MiningCharacterHealth>() : null;
            if (prefab == null)
            {
                var root = new GameObject("MushroomMonster");
                try
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                    visual.name = "Mushroom Visual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    var animator = visual.GetComponentInChildren<Animator>();
                    if (animator == null) { Debug.LogError("Pack Mushroom has no Animator."); return; }
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
                    bool first = true;
                    foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                    {
                        if (first) { bounds = renderer.bounds; first = false; }
                        else bounds.Encapsulate(renderer.bounds);
                    }
                    visual.transform.localPosition += Vector3.up * -bounds.min.y;
                    var monster = root.AddComponent<MushroomMonster>();
                    var motor = root.GetComponent<CharacterController>();
                    motor.height = Mathf.Max(0.5f, bounds.size.y);
                    motor.radius = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.3f, 0.2f, 0.6f);
                    motor.center = Vector3.up * motor.height / 2f;
                    motor.stepOffset = Mathf.Min(0.25f, motor.height * 0.3f);
                    var settings = new SerializedObject(monster);
                    settings.FindProperty("animator").objectReferenceValue = animator;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    var health = root.GetComponent<MiningCharacterHealth>();
                    string barPath = "Assets/Microlight/MicroBar/Prefabs/SimpleBars/Sprite_SimpleMicroBarSRP.prefab";
                    var barPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(barPath);
                    if (barPrefab != null)
                    {
                        var bar = (GameObject)PrefabUtility.InstantiatePrefab(barPrefab, root.transform);
                        bar.name = "Monster Health Bar";
                        bar.transform.localPosition = Vector3.up * (motor.height + 0.35f);
                        bar.transform.localScale = Vector3.one * 0.4f;
                        var healthSettings = new SerializedObject(health);
                        healthSettings.FindProperty("healthBar").objectReferenceValue = bar.transform;
                        healthSettings.FindProperty("microBar").objectReferenceValue = bar.GetComponent<MicroBar>();
                        healthSettings.FindProperty("healthLabel").objectReferenceValue = bar.GetComponentInChildren<TMP_Text>();
                        healthSettings.ApplyModifiedPropertiesWithoutUndo();
                    }
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally { Object.DestroyImmediate(root); }
            }
            // Give the game prefab a textured material without touching the user's pack materials.
            string materialPath = Folder + "/Mushroom.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.name = "Mushroom";
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/MonsterPack3D/Textures/T_Mushroom_C.png"));
                material.SetFloat("_Smoothness", 0.1f);
                AssetDatabase.CreateAsset(material, materialPath);
                var contents = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    foreach (var renderer in contents.GetComponentsInChildren<SkinnedMeshRenderer>())
                        renderer.sharedMaterial = material;
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            var zone = Object.FindFirstObjectByType<MonsterSpawnZone>(FindObjectsInactive.Include);
            if (zone == null)
            {
                var go = new GameObject("Mushroom Spawn Zone");
                Undo.RegisterCreatedObjectUndo(go, "Create Mushroom Spawn Zone");
                zone = Undo.AddComponent<MonsterSpawnZone>(go);
                go.transform.position = player != null ? player.transform.position + Vector3.forward * 8f : Vector3.zero;
                var settings = new SerializedObject(zone);
                settings.FindProperty("player").objectReferenceValue = player;
                var entries = settings.FindProperty("monsters");
                entries.arraySize = 1;
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue = prefab.GetComponent<MushroomMonster>();
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("chance").floatValue = 100f;
                settings.ApplyModifiedProperties();
                var preview = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
                Undo.RegisterCreatedObjectUndo(preview, "Create editable Mushroom preview");
                preview.name = "Mushroom Preview (disabled during play)";
                preview.AddComponent<MonsterScenePreview>();
                EditorSceneManager.MarkSceneDirty(go.scene);
            }
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = zone.gameObject;
            Debug.Log("Mushroom zone ready: move its Transform, edit Area Size / spawn table / timing. HP regeneration: Mining Character Health on Player and MushroomMonster prefab.", zone);
        }
    }
}
#endif
