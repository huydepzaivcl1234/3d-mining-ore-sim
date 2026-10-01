#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Microlight.MicroBar;
using TMPro;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Reuses the authored Golem visual, shared monster logic and existing mining spawn table.</summary>
    public static class GolemBossSetupMenu
    {
        private const string Folder = "Assets/Prefabs/Monsters/";
        [MenuItem("Mining Simulator/Setup/Add Golem and Boss Variants")]
        public static void Apply()
        {
            if (Application.isPlaying) return;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Golem.prefab");
            var mushroom = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "MushroomMonster.prefab");
            if (source == null || mushroom == null) { Debug.LogError("Golem or Mushroom source is missing."); return; }
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "GolemCombat.controller");
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "GolemCombat.controller");
                int i = 0;
                foreach (string name in new[] { "Idle", "Walk", "Hammer", "Damage", "Down", "Hit" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/MonsterPack3D/Animations/Golem/SA_Golem_" + name + ".anim");
                    if (clip == null) { Debug.LogError("Missing Golem animation: " + name); return; }
                    var state = controller.layers[0].stateMachine.AddState(name, new Vector3(250, i++ * 70));
                    state.motion = clip;
                    if (name == "Idle") controller.layers[0].stateMachine.defaultState = state;
                }
                EditorUtility.SetDirty(controller.layers[0].stateMachine);
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssetIfDirty(controller);
            }
            var originalRewards = mushroom.GetComponent<MushroomMonster>().RewardData;
            const string rewardPath = "Assets/GameData/Monsters/GolemRewards.asset";
            var rewards = AssetDatabase.LoadAssetAtPath<MonsterRewardData>(rewardPath);
            if (rewards == null)
            {
                rewards = ScriptableObject.CreateInstance<MonsterRewardData>();
                if (originalRewards != null) EditorUtility.CopySerialized(originalRewards, rewards);
                rewards.name = "GolemRewards";
                rewards.gold = 75f; rewards.experience = 35f;
                AssetDatabase.CreateAsset(rewards, rewardPath);
            }
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/MonsterDissolve.shader");
            foreach (var data in new[] { originalRewards, rewards })
            {
                if (data == null) continue;
                Undo.RecordObject(data, "Monster Dissolve Shader");
                data.dissolveShader = shader;
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
            }
            string path = Folder + "GolemMonster.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                var root = new GameObject("GolemMonster");
                try
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                    visual.name = "Golem Visual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    var animator = visual.GetComponentInChildren<Animator>();
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    Bounds bounds = new Bounds(); bool first = true;
                    foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                    { if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
                    visual.transform.localPosition += Vector3.up * -bounds.min.y;
                    var monster = root.AddComponent<MushroomMonster>();
                    var motor = root.GetComponent<CharacterController>();
                    motor.height = Mathf.Max(.5f, bounds.size.y);
                    motor.radius = Mathf.Clamp(bounds.size.z * .4f, .3f, .65f);
                    motor.center = Vector3.up * motor.height * .5f;
                    motor.skinWidth = .03f; motor.stepOffset = .2f; motor.minMoveDistance = 0f;
                    var so = new SerializedObject(monster);
                    so.FindProperty("animator").objectReferenceValue = animator;
                    so.FindProperty("attackState").stringValue = "Hammer";
                    so.FindProperty("rewards").objectReferenceValue = rewards;
                    so.FindProperty("damage").floatValue = 8f;
                    so.FindProperty("attackRange").floatValue = 2f;
                    so.FindProperty("moveSpeed").floatValue = 1.3f;
                    so.FindProperty("deathDelay").floatValue = 3f;
                    so.FindProperty("hitMoment").floatValue = .5f;
                    so.FindProperty("hitShape").enumValueIndex = 1;
                    so.FindProperty("warningShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/MonsterAttackWarning.shader");
                    so.ApplyModifiedPropertiesWithoutUndo();
                    var health = root.GetComponent<MiningCharacterHealth>();
                    var healthSettings = new SerializedObject(health);
                    healthSettings.FindProperty("maxHealth").floatValue = 80f;
                    var template = mushroom.GetComponent<MiningCharacterHealth>().HealthBar;
                    if (template != null)
                    {
                        var bar = Object.Instantiate(template.gameObject, root.transform);
                        bar.name = "Monster Health Bar";
                        bar.transform.localPosition = Vector3.up * (motor.height + .35f);
                        healthSettings.FindProperty("healthBar").objectReferenceValue = bar.transform;
                        healthSettings.FindProperty("microBar").objectReferenceValue = bar.GetComponent<MicroBar>();
                        healthSettings.FindProperty("healthLabel").objectReferenceValue = bar.GetComponentInChildren<TMP_Text>(true);
                    }
                    healthSettings.ApplyModifiedPropertiesWithoutUndo();
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { Object.DestroyImmediate(root); }
            }
            var zone = Object.FindAnyObjectByType<MonsterSpawnZone>(FindObjectsInactive.Include);
            if (zone != null)
            {
                var fields = new SerializedObject(zone);
                var entries = fields.FindProperty("monsters");
                bool found = false;
                for (int i = 0; i < entries.arraySize; i++)
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue == prefab.GetComponent<MushroomMonster>())
                    {
                        found = true;
                        var icon = entries.GetArrayElementAtIndex(i).FindPropertyRelative("icon");
                        if (icon.objectReferenceValue == null)
                        {
                            icon.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/MiningMonsterIcons/GolemMonster.png");
                            fields.ApplyModifiedProperties();
                            EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);
                        }
                    }
                if (!found)
                {
                    Undo.RecordObject(zone, "Add Golem to Mining Encounters");
                    int index = entries.arraySize;
                    entries.InsertArrayElementAtIndex(index);
                    var item = entries.GetArrayElementAtIndex(index);
                    item.FindPropertyRelative("prefab").objectReferenceValue = prefab.GetComponent<MushroomMonster>();
                    item.FindPropertyRelative("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/MiningMonsterIcons/GolemMonster.png");
                    item.FindPropertyRelative("chance").floatValue = 25f;
                    // First integration only: 75/25 for the user's original single Mushroom table.
                    if (index == 1 && entries.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue == mushroom.GetComponent<MushroomMonster>())
                        entries.GetArrayElementAtIndex(0).FindPropertyRelative("chance").floatValue = 75f;
                    fields.ApplyModifiedProperties();
                    EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);
                }
            }
            Debug.Log("Golem and boss variants ready. Edit species Rewards assets and spawner Chances; save the scene yourself.");
        }
    }
}
#endif
