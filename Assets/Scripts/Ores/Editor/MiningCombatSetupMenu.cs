#if UNITY_EDITOR
using Microlight.MicroBar;
using MiningSimulator.Ores;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Editor
{
    public static class MiningCombatSetupMenu
    {
        private const string MenuPath = "Mining Simulator/Setup/Health Bar (Selected Character)";
        private const string MicroBarPrefabPath =
            "Assets/Microlight/MicroBar/Prefabs/SimpleBars/Sprite_SimpleMicroBarSRP.prefab";

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid())
            {
                Debug.LogWarning("Open the gameplay scene and leave Play Mode before setting up combat.");
                return;
            }

            GameObject actor = Selection.activeGameObject;
            if (actor != null && actor.scene == scene)
                actor = actor.transform.root.gameObject;
            else
            {
                MiningOrbitCamera camera = Object.FindFirstObjectByType<MiningOrbitCamera>();
                actor = camera != null && camera.FollowTarget != null
                    ? camera.FollowTarget.root.gameObject : null;
            }

            if (actor == null || actor.scene != scene)
            {
                Debug.LogWarning("Select your PlayerArmature (or another character) in the Hierarchy first.");
                return;
            }

            GameObject microBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MicroBarPrefabPath);
            if (microBarPrefab == null || microBarPrefab.GetComponent<MicroBar>() == null)
            {
                Debug.LogError("The existing Sprite_SimpleMicroBarSRP prefab is missing. No combat setup was changed.");
                return;
            }

            MiningCharacterHealth combat = actor.GetComponent<MiningCharacterHealth>();
            if (combat == null) combat = Undo.AddComponent<MiningCharacterHealth>(actor);
            SerializedObject settings = new(combat);

            Transform existing = actor.transform.Find("Combat Health Bar");
            MicroBar microBar = existing != null ? existing.GetComponent<MicroBar>() : null;
            if (existing != null && microBar == null)
            {
                if (existing.GetComponent<Canvas>() == null)
                {
                    Debug.LogError("'Combat Health Bar' is not the old Canvas or a MicroBar. Rename it or inspect it before running setup.", existing);
                    return;
                }
                // Replace only the old combat Canvas made by this menu. This also
                // removes a Screen Space Canvas if its render mode was changed.
                Undo.DestroyObjectImmediate(existing.gameObject);
                existing = null;
            }
            if (microBar == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    microBarPrefab, actor.transform);
                Undo.RegisterCreatedObjectUndo(instance, "Add combat MicroBar");
                instance.name = "Combat Health Bar";
                instance.transform.localPosition = new Vector3(0f, 2.25f, 0f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one * 0.5f;
                microBar = instance.GetComponent<MicroBar>();
            }
            TextMeshPro label = microBar.GetComponentInChildren<TextMeshPro>(true);
            if (label == null) label = CreateHealthText(microBar.transform);
            settings.FindProperty("healthBar").objectReferenceValue = microBar.transform;
            settings.FindProperty("microBar").objectReferenceValue = microBar;
            settings.FindProperty("healthLabel").objectReferenceValue = label;
            settings.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = actor;
            Debug.Log("Health bar is ready. Character movement and animations are unchanged.", actor);
        }

        private static TextMeshPro CreateHealthText(Transform parent)
        {
            GameObject textObject = new("Health Value", typeof(RectTransform), typeof(TextMeshPro));
            Undo.RegisterCreatedObjectUndo(textObject, "Create health text");
            Undo.SetTransformParent(textObject.transform, parent, "Parent health text");
            RectTransform rect = (RectTransform)textObject.transform;
            rect.localPosition = new Vector3(0f, 0f, -0.03f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(1.85f, 0.24f);
            TextMeshPro label = textObject.GetComponent<TextMeshPro>();
            label.text = "100 / 100";
            label.fontSize = 1.2f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.enableAutoSizing = true;
            label.fontSizeMin = 0.35f;
            label.fontSizeMax = 1.5f;
            return label;
        }
    }
}
#endif
