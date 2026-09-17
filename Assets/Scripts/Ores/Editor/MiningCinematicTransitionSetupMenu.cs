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
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Exit Play Mode",
                    "Stop Play Mode before building the cinematic transition. " +
                    "Unity discards scene setup changes made while the game is running.", "OK");
                return;
            }

            MiningMainMenu mainMenu = Object.FindFirstObjectByType<MiningMainMenu>(
                FindObjectsInactive.Include);
            if (mainMenu == null)
            {
                Debug.LogWarning("Open the mining scene first: MiningMainMenu was not found. No scene was changed.");
                return;
            }

            SerializedObject menuFields = new(mainMenu);
            CanvasGroup menuGroup = Reference<CanvasGroup>(menuFields, "canvasGroup");
            CanvasGroup presentationGroup = Reference<CanvasGroup>(menuFields, "mainViewGroup");
            TextMeshProUGUI title = Reference<TextMeshProUGUI>(menuFields, "titleLabel");
            GameObject mainView = Reference<GameObject>(menuFields, "mainView");
            UnityEngine.UI.Button play = Reference<UnityEngine.UI.Button>(menuFields, "playButton");
            UnityEngine.UI.Button shop = Reference<UnityEngine.UI.Button>(menuFields, "shopButton");
            UnityEngine.UI.Button settings = Reference<UnityEngine.UI.Button>(menuFields, "settingsButton");
            UnityEngine.UI.Button exit = Reference<UnityEngine.UI.Button>(menuFields, "exitButton");
            Canvas canvas = mainMenu.GetComponentInParent<Canvas>(true);
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;

            // Only fade the Main Menu itself. A stale reference to a CanvasGroup above it would
            // make every gameplay HUD disappear together with the menu.
            if (menuGroup == null || menuGroup.transform != mainMenu.transform)
            {
                menuGroup = mainMenu.GetComponent<CanvasGroup>() ??
                            Undo.AddComponent<CanvasGroup>(mainMenu.gameObject);
                Set(menuFields, "canvasGroup", menuGroup);
            }

            if (menuGroup == null || play == null || settings == null ||
                exit == null || canvasRect == null)
            {
                Debug.LogWarning("Main Menu cinematic wiring is incomplete. Run 'Create Or Update Main Menu' first; no scene was changed.", mainMenu);
                return;
            }

            RectTransform overlay = EnsureOverlay(canvasRect, canvas);
            RectTransform rays = EnsureBurst(overlay, "Radiant Rays",
                MiningRadialBurstGraphic.BurstStyle.Rays, new Vector2(0.53f, 0.54f),
                new Vector2(1900f, 1900f));
            RectTransform flare = EnsureBurst(overlay, "Radiant Flare",
                MiningRadialBurstGraphic.BurstStyle.Flare, new Vector2(0.53f, 0.54f),
                new Vector2(1600f, 1600f));
            RectTransform flash = EnsureFlash(overlay);
            SetLayerRecursively(overlay.gameObject, canvasRect.gameObject.layer);
            UnityEngine.UI.Image overlayImage = flash.GetComponent<UnityEngine.UI.Image>();
            CanvasGroup overlayGroup = flash.GetComponent<CanvasGroup>();
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
            Set(cinematicFields, "menuPresentationCanvasGroup", presentationGroup);
            Set(cinematicFields, "menuPresentationRect",
                mainView != null ? mainView.transform as RectTransform : null);
            RectTransform logo = title != null
                ? title.rectTransform
                : mainView != null ? mainView.transform.Find("Title") as RectTransform : null;
            Set(cinematicFields, "logoRect", logo);
            Set(cinematicFields, "mainCamera", Camera.main != null
                ? Camera.main
                : Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include));
            Set(cinematicFields, "flashOverlay", overlayImage);
            Set(cinematicFields, "flashCanvasGroup", overlayGroup);
            Set(cinematicFields, "backgroundRect", Find(mainMenu.transform,
                "Showcase Background") as RectTransform);
            Set(cinematicFields, "showcaseMotion",
                mainMenu.GetComponent<MiningMainMenuShowcaseMotion>());
            Set(cinematicFields, "flareRect", flare);
            Set(cinematicFields, "flareCanvasGroup", flare.GetComponent<CanvasGroup>());
            Set(cinematicFields, "raysRect", rays);
            Set(cinematicFields, "raysCanvasGroup", rays.GetComponent<CanvasGroup>());
            SetColor(cinematicFields, "flashColor", new Color(1f, 0.86f, 1f, 1f));
            SetFloat(cinematicFields, "flashOpacity", 1f);
            SetFloat(cinematicFields, "backgroundZoom", 2.6f);
            SetFloat(cinematicFields, "rumbleStrength", 5f);
            SetFloat(cinematicFields, "menuExitDuration", 0.4f);
            SetFloat(cinematicFields, "cameraZoomDuration", 0.85f);
            SetFloat(cinematicFields, "flashStartDelay", 0.45f);
            SetFloat(cinematicFields, "flashInDuration", 0.15f);
            SetFloat(cinematicFields, "handoffDelayAfterFlash", 0.25f);
            SetFloat(cinematicFields, "flashHoldAfterHandoff", 0.4f);
            SetFloat(cinematicFields, "flashOutDuration", 1.35f);
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
            Debug.Log("Radiant Main Menu transition built. It now performs the reference " +
                      "camera dive, rumble, amethyst flare, 0.65 second whiteout window, hidden " +
                      "gameplay handoff, 1.35 second dissolve and HUD fly-in. Existing buttons " +
                      "and gameplay logic remain unchanged; save the scene.", overlay.gameObject);
        }

        [MenuItem("Mining Simulator/UI/Build Cinematic Menu Transition", true)]
        private static bool CanBuild()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static RectTransform EnsureOverlay(RectTransform canvas, Canvas parentCanvas)
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

            SetLayerRecursively(obj, canvas.gameObject.layer);

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
            image.color = Color.clear;
            image.raycastTarget = false;

            CanvasGroup group = obj.GetComponent<CanvasGroup>() ?? Undo.AddComponent<CanvasGroup>(obj);
            Undo.RecordObject(group, "Configure Cinematic Transition Overlay");
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
            group.ignoreParentGroups = true;

            // Isolate the flash from sibling panels that change their hierarchy order at runtime.
            // This Canvas affects only the overlay; authored HUD layout stays untouched.
            Canvas overlayCanvas = obj.GetComponent<Canvas>() ?? Undo.AddComponent<Canvas>(obj);
            Undo.RecordObject(overlayCanvas, "Configure Cinematic Transition Canvas");
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = (parentCanvas != null ? parentCanvas.sortingOrder : 0) + 100;
            _ = obj.GetComponent<UnityEngine.UI.GraphicRaycaster>() ??
                Undo.AddComponent<UnityEngine.UI.GraphicRaycaster>(obj);
            return rect;
        }

        private static RectTransform EnsureBurst(Transform parent, string name,
            MiningRadialBurstGraphic.BurstStyle style, Vector2 anchor, Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(MiningRadialBurstGraphic), typeof(CanvasGroup));
                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
                obj.transform.SetParent(parent, false);
            }
            else
            {
                obj = existing.gameObject;
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Layout " + name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            MiningRadialBurstGraphic graphic = obj.GetComponent<MiningRadialBurstGraphic>() ??
                                                Undo.AddComponent<MiningRadialBurstGraphic>(obj);
            Undo.RecordObject(graphic, "Style " + name);
            if (style == MiningRadialBurstGraphic.BurstStyle.Flare)
            {
                graphic.Configure(style, Color.white,
                    new Color(0.96f, 0.56f, 1f, 0.9f),
                    new Color(0.50f, 0.06f, 0.92f, 0f));
            }
            else
            {
                graphic.Configure(style, new Color(1f, 1f, 1f, 0.5f),
                    new Color(0.92f, 0.55f, 1f, 0.5f),
                    new Color(0.75f, 0.18f, 1f, 0f));
            }

            CanvasGroup group = obj.GetComponent<CanvasGroup>() ?? Undo.AddComponent<CanvasGroup>(obj);
            Undo.RecordObject(group, "Configure " + name);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return rect;
        }

        private static RectTransform EnsureFlash(Transform parent)
        {
            const string name = "Radiant Flash";
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
                obj.transform.SetParent(parent, false);
            }
            else
            {
                obj = existing.gameObject;
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Stretch " + name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            UnityEngine.UI.Image image = obj.GetComponent<UnityEngine.UI.Image>() ??
                                         Undo.AddComponent<UnityEngine.UI.Image>(obj);
            Undo.RecordObject(image, "Style " + name);
            image.sprite = null;
            image.color = new Color(1f, 0.86f, 1f, 1f);
            image.raycastTarget = true;

            CanvasGroup group = obj.GetComponent<CanvasGroup>() ?? Undo.AddComponent<CanvasGroup>(obj);
            Undo.RecordObject(group, "Configure " + name);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            group.ignoreParentGroups = true;
            rect.SetAsLastSibling();
            return rect;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
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

        private static void SetColor(SerializedObject fields, string name, Color value)
        {
            SerializedProperty property = fields.FindProperty(name);
            if (property != null)
            {
                property.colorValue = value;
            }
        }

        private static void SetFloat(SerializedObject fields, string name, float value)
        {
            SerializedProperty property = fields.FindProperty(name);
            if (property != null)
            {
                property.floatValue = value;
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
