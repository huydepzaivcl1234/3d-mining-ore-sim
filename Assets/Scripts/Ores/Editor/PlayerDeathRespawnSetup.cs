#if UNITY_EDITOR
using System.Linq;
using MiningSimulator.Ores;
using StarterAssets;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Editor
{
    public static class PlayerDeathRespawnSetup
    {
        [MenuItem("Mining Simulator/Setup/Player Death And Respawn")]
        private static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Leave Play Mode before setting up respawn.");
                return;
            }
            ThirdPersonController player = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<ThirdPersonController>() : null;
            if (player == null) player = Object.FindAnyObjectByType<ThirdPersonController>();
            if (player == null)
            {
                Debug.LogError("Select your Player with ThirdPersonController first.");
                return;
            }
            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController is not AnimatorController source)
            {
                Debug.LogError("Player needs an Animator Controller (not an override) before setup.", player);
                return;
            }
            var scene = player.gameObject.scene;
            Canvas canvas = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                .FirstOrDefault(item => item.name == "Mining HUD Canvas");
            if (canvas == null)
            {
                Debug.LogError("Mining HUD Canvas was not found. No scene changes were made.");
                return;
            }
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx")
                .OfType<AnimationClip>().FirstOrDefault(item => !item.name.StartsWith("__preview"));
            if (clip == null)
            {
                Debug.LogError("HumanM@Death01 animation was not found. No scene changes were made.");
                return;
            }
            // Copy the assigned controller so imported/demo assets remain unchanged.
            string sourcePath = AssetDatabase.GetAssetPath(source);
            AnimatorController controller = source;
            if (!sourcePath.EndsWith(" Respawn.controller"))
            {
                string path = sourcePath.Substring(0, sourcePath.Length - ".controller".Length) + " Respawn.controller";
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (controller == null)
                {
                    if (!AssetDatabase.CopyAsset(sourcePath, path))
                    {
                        Debug.LogError("Could not copy the Player controller.");
                        return;
                    }
                    controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                }
            }
            var machine = controller.layers[0].stateMachine;
            if (!machine.states.Any(item => item.state.name == "Death"))
            {
                Undo.RegisterCompleteObjectUndo(machine, "Add Player Death state");
                AnimatorState state = machine.AddState("Death", new Vector3(600f, 300f));
                state.motion = clip;
                state.writeDefaultValues = false;
                EditorUtility.SetDirty(controller);
            }
            Undo.RecordObject(animator, "Assign respawn controller");
            animator.runtimeAnimatorController = controller;
            var health = player.GetComponent<MiningCharacterHealth>();
            if (health == null) health = Undo.AddComponent<MiningCharacterHealth>(player.gameObject);
            var respawn = player.GetComponent<PlayerDeathRespawn>();
            if (respawn == null) respawn = Undo.AddComponent<PlayerDeathRespawn>(player.gameObject);
            var settings = new SerializedObject(respawn);

            Transform panel = canvas.transform.Find("Player Respawn Panel");
            if (panel == null)
            {
                var panelObject = new GameObject("Player Respawn Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                Undo.RegisterCreatedObjectUndo(panelObject, "Create respawn panel");
                Undo.SetTransformParent(panelObject.transform, canvas.transform, "Parent respawn panel");
                panel = panelObject.transform;
                var rect = (RectTransform)panel;
                rect.localScale = Vector3.one;
                rect.anchorMin = new Vector2(0.2f, 0.8f);
                rect.anchorMax = new Vector2(0.8f, 0.96f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var background = panelObject.GetComponent<UnityEngine.UI.Image>();
                background.color = new Color(0.08f, 0.03f, 0.03f, 0.9f);
                background.raycastTarget = false;
            }
            TMP_Text text = panel.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                var label = new GameObject("Respawn Countdown", typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(label, "Create countdown");
                Undo.SetTransformParent(label.transform, panel, "Parent countdown");
                var rect = (RectTransform)label.transform;
                rect.localScale = Vector3.one;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(12f, 8f);
                rect.offsetMax = new Vector2(-12f, -8f);
                text = label.GetComponent<TextMeshProUGUI>();
                text.text = "Respawn in 10s\nWASD: move | Space/Ctrl: up/down | Hold RMB: look";
                text.fontSize = 26f;
                text.enableAutoSizing = true;
                text.fontSizeMin = 14f;
                text.fontSizeMax = 26f;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
            }
            Transform marker = settings.FindProperty("respawnPoint").objectReferenceValue as Transform;
            if (marker == null)
                marker = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(item => item.name == "Player Respawn Point");
            if (marker == null)
            {
                var markerObject = new GameObject("Player Respawn Point");
                Undo.RegisterCreatedObjectUndo(markerObject, "Create respawn point");
                marker = markerObject.transform;
                marker.SetPositionAndRotation(player.transform.position, player.transform.rotation);
            }
            settings.FindProperty("respawnPoint").objectReferenceValue = marker;
            Camera camera = Camera.main;
            if (camera == null)
                camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                    .FirstOrDefault();
            Behaviour[] drivers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Behaviour>(true))
                // GetComponentsInChildren returns null slots for missing scripts.
                // A broken unrelated Behaviour must not abort wiring the Player.
                .Where(item => item != null && (item is MiningOrbitCamera || item.GetType().Name == "CinemachineBrain")).ToArray();
            var driverArray = settings.FindProperty("cameraDrivers");
            // Keep hand-authored references when setup is run again.
            if (driverArray.arraySize == 0)
            {
                driverArray.arraySize = drivers.Length;
                for (int i = 0; i < drivers.Length; i++) driverArray.GetArrayElementAtIndex(i).objectReferenceValue = drivers[i];
            }
            settings.FindProperty("animator").objectReferenceValue = animator;
            settings.FindProperty("respawnPanel").objectReferenceValue = panel.gameObject;
            settings.FindProperty("countdownLabel").objectReferenceValue = text;
            if (settings.FindProperty("spectatorCamera").objectReferenceValue == null)
                settings.FindProperty("spectatorCamera").objectReferenceValue = camera;
            settings.ApplyModifiedProperties();
            panel.SetAsLastSibling();
            // Visible in edit mode for authoring; Awake hides it during gameplay.
            Undo.RecordObject(panel.gameObject, "Show respawn preview");
            panel.gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Player respawn is configured. Edit Player Death Respawn, Player Respawn Point and Mining HUD Canvas/Player Respawn Panel. Save the scene when ready.", player);
        }
    }
}
#endif
