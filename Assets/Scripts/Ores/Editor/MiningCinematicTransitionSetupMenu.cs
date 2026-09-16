#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Wires the cinematic Play transition without changing authored HUD layout.</summary>
    public static class MiningCinematicTransitionSetupMenu
    {
        [MenuItem("Mining Simulator/UI/Build Cinematic Menu Transition")]
        public static void Build()
        {
            MiningMainMenu mainMenu = Object.FindFirstObjectByType<MiningMainMenu>(
                FindObjectsInactive.Include);
            if (mainMenu == null)
            {
                Debug.LogWarning("Open the mining scene first: MiningMainMenu was not found. No scene was changed.");
                return;
            }

            SerializedObject menuFields = new(mainMenu);
            CanvasGroup menuGroup = Reference<CanvasGroup>(menuFields, "canvasGroup");
            TextMeshProUGUI title = Reference<TextMeshProUGUI>(menuFields, "titleLabel");
            GameObject mainView = Reference<GameObject>(menuFields, "mainView");
            UnityEngine.UI.Button play = Reference<UnityEngine.UI.Button>(menuFields, "playButton");
            UnityEngine.UI.Button shop = Reference<UnityEngine.UI.Button>(menuFields, "shopButton");
            UnityEngine.UI.Button settings = Reference<UnityEngine.UI.Button>(menuFields, "settingsButton");
            UnityEngine.UI.Button exit = Reference<UnityEngine.UI.Button>(menuFields, "exitButton");
            Canvas canvas = mainMenu.GetComponentInParent<Canvas>(true);
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;

            if (menuGroup == null || play == null || settings == null ||
                exit == null || canvasRect == null)
            {
                Debug.LogWarning("Main Menu cinematic wiring is incomplete. Run 'Create Or Update Main Menu' first; no scene was changed.", mainMenu);
                return;
            }

            RectTransform overlay = EnsureOverlay(canvasRect);
            UnityEngine.UI.Image overlayImage = overlay.GetComponent<UnityEngine.UI.Image>();
            CanvasGroup overlayGroup = overlay.GetComponent<CanvasGroup>();
            MiningCinematicTransition cinematic = overlay.GetComponent<MiningCinematicTransition>() ??
                                                   Undo.AddComponent<MiningCinematicTransition>(overlay.gameObject);

            List<RectTransform> buttons = new()
            {
                play.transform as RectTransform
            };
            if (shop != null)
            {
                buttons.Add(shop.transform as RectTransform);
            }
            buttons.Add(settings.transform as RectTransform);
            buttons.Add(exit.transform as RectTransform);

            List<MiningHudFlyIn> flyIns = new();
            AddFlyIn(canvasRect, flyIns, "NPC Shop", MiningHudFlyIn.SlideDirection.FromLeft,
                230f, 0f);
            AddFlyIn(canvasRect, flyIns, "Rebirth HUD", MiningHudFlyIn.SlideDirection.FromTop,
                180f, 0.02f);
            AddFlyIn(canvasRect, flyIns, "Gem HUD", MiningHudFlyIn.SlideDirection.FromTop,
                180f, 0.07f);
            AddFlyIn(canvasRect, flyIns, "NPC Progress HUD", MiningHudFlyIn.SlideDirection.FromRight,
                230f, 0.04f);
            AddFlyIn(canvasRect, flyIns, "Audio Menu Button", MiningHudFlyIn.SlideDirection.FromRight,
                180f, 0.08f);
            AddFlyIn(canvasRect, flyIns, "Inventory Menu Button", MiningHudFlyIn.SlideDirection.FromRight,
                180f, 0.12f);
            AddFlyIn(canvasRect, flyIns, "Shop Menu Button", MiningHudFlyIn.SlideDirection.FromRight,
                180f, 0.16f);
            AddFlyIn(canvasRect, flyIns, "Quest Menu Button", MiningHudFlyIn.SlideDirection.FromRight,
                180f, 0.20f);

            SerializedObject cinematicFields = new(cinematic);
            Set(cinematicFields, "mainMenuCanvasGroup", menuGroup);
            RectTransform logo = title != null
                ? title.rectTransform
                : mainView != null ? mainView.transform.Find("Title") as RectTransform : null;
            Set(cinematicFields, "logoRect", logo);
            Set(cinematicFields, "mainCamera", Camera.main != null
                ? Camera.main
                : Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include));
            Set(cinematicFields, "flashOverlay", overlayImage);
            Set(cinematicFields, "flashCanvasGroup", overlayGroup);
            SetArray(cinematicFields, "menuButtons", buttons);
            SetArray(cinematicFields, "hudFlyIns", flyIns);
            cinematicFields.ApplyModifiedProperties();

            Set(menuFields, "cinematicTransition", cinematic);
            menuFields.ApplyModifiedProperties();

            overlay.SetAsLastSibling();
            EditorUtility.SetDirty(mainMenu);
            EditorUtility.SetDirty(cinematic);
            EditorSceneManager.MarkSceneDirty(mainMenu.gameObject.scene);
            Selection.activeGameObject = overlay.gameObject;
            EditorGUIUtility.PingObject(overlay.gameObject);
            Debug.Log("Cinematic Main Menu transition built. Play now performs staggered menu exit, unscaled camera FOV zoom, purple flash, gameplay handoff and HUD fly-in. Existing gameplay/menu logic remains authoritative; save the scene.", overlay.gameObject);
        }

        private static RectTransform EnsureOverlay(RectTransform canvas)
        {
            Transform existing = canvas.Find("Cinematic Transition Overlay");
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject("Cinematic Transition Overlay", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
                Undo.RegisterCreatedObjectUndo(obj, "Create Cinematic Transition Overlay");
                obj.transform.SetParent(canvas, false);
            }
            else
            {
                obj = existing.gameObject;
                obj.SetActive(true);
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Stretch Cinematic Transition Overlay");
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            UnityEngine.UI.Image image = obj.GetComponent<UnityEngine.UI.Image>() ??
                                         Undo.AddComponent<UnityEngine.UI.Image>(obj);
            Undo.RecordObject(image, "Style Cinematic Transition Overlay");
            image.sprite = null;
            image.color = new Color(0.18f, 0.035f, 0.24f, 1f);
            image.raycastTarget = true;

            CanvasGroup group = obj.GetComponent<CanvasGroup>() ?? Undo.AddComponent<CanvasGroup>(obj);
            Undo.RecordObject(group, "Configure Cinematic Transition Overlay");
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return rect;
        }

        private static void AddFlyIn(Transform root, ICollection<MiningHudFlyIn> results,
            string objectName, MiningHudFlyIn.SlideDirection direction, float distance,
            float delay)
        {
            Transform target = Find(root, objectName);
            RectTransform rect = target as RectTransform;
            if (rect == null)
            {
                return;
            }
            MiningHudFlyIn flyIn = rect.GetComponent<MiningHudFlyIn>() ??
                                   Undo.AddComponent<MiningHudFlyIn>(rect.gameObject);
            Undo.RecordObject(flyIn, "Configure HUD Fly In");
            flyIn.Configure(rect, direction, distance, delay, 0.45f);
            EditorUtility.SetDirty(flyIn);
            results.Add(flyIn);
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }

        private static T Reference<T>(SerializedObject fields, string name) where T : Object
        {
            return fields.FindProperty(name)?.objectReferenceValue as T;
        }

        private static void Set(SerializedObject fields, string name, Object value)
        {
            SerializedProperty property = fields.FindProperty(name);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetArray<T>(SerializedObject fields, string name,
            IReadOnlyList<T> values) where T : Object
        {
            SerializedProperty property = fields.FindProperty(name);
            if (property == null)
            {
                return;
            }
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}
#endif
