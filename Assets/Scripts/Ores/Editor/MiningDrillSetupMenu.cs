using System;
using System.Linq;
using TMPro;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Editor
{
    /// <summary>Creates the drill prefab once without modifying existing HUD, icons, scenes, or prefabs.</summary>
    public static class MiningDrillSetupMenu
    {
        private const string DrillFolder = "Assets/Prefabs/Drill";
        private const string DrillPrefabPath = DrillFolder + "/MiningDrillStation.prefab";
        private const string DrillDataPath = "Assets/GameData/Drill/MiningDrillData.asset";
        private const string PushedDrillModelPath = "Assets/Prefabs/Untitled.fbx";
        private const string WorkingControllerPath = DrillFolder + "/MiningDrillWorking.controller";
        private const string BrokenModelName = "Broken Drill Model";
        private const string WorkingModelName = "Working Drill Model";

        [MenuItem("Mining Simulator/Drill/Install Pushed Model In Scene")]
        private static void InstallPushedModelInScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before installing the drill model in the Scene.");
                return;
            }

            MiningDrillStation station = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<MiningDrillStation>()
                : null;
            station ??= UnityEngine.Object.FindFirstObjectByType<MiningDrillStation>();
            if (station == null)
            {
                Debug.LogError("No MiningDrillStation exists in the open Scene. Select the drill object and try again.");
                return;
            }

            GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(PushedDrillModelPath);
            if (sourceModel == null)
            {
                Debug.LogError($"Missing pushed drill model at {PushedDrillModelPath}.");
                return;
            }

            AnimationClip workingClip = PrepareAndFindWorkingClip();
            AnimatorController controller = workingClip != null
                ? GetOrCreateWorkingController(workingClip)
                : null;

            Transform brokenRoot = GetOrCreateModelInstance(station.transform, sourceModel,
                BrokenModelName, out bool createdBroken);
            Transform workingRoot = GetOrCreateModelInstance(station.transform, sourceModel,
                WorkingModelName, out bool createdWorking);

            ConfigureVisualState(brokenRoot, showBrokenParts: true);
            ConfigureVisualState(workingRoot, showBrokenParts: false);

            Animator brokenAnimator = brokenRoot.GetComponent<Animator>();
            if (brokenAnimator != null)
            {
                brokenAnimator.enabled = false;
            }

            Animator workingAnimator = workingRoot.GetComponent<Animator>();
            if (workingAnimator == null)
            {
                workingAnimator = Undo.AddComponent<Animator>(workingRoot.gameObject);
            }

            workingAnimator.runtimeAnimatorController = controller;
            workingAnimator.applyRootMotion = false;
            workingAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            SerializedObject serializedStation = new(station);
            serializedStation.FindProperty("brokenModelRoot").objectReferenceValue = brokenRoot;
            serializedStation.FindProperty("modelRoot").objectReferenceValue = workingRoot;
            // The imported animation owns Drill_Spin. Clear the old procedural placeholder
            // so the two animation systems cannot rotate the drill head at the same time.
            serializedStation.FindProperty("drillHead").objectReferenceValue = null;
            serializedStation.FindProperty("previewWorkingModelInEditMode").boolValue = false;
            serializedStation.ApplyModifiedProperties();

            brokenRoot.gameObject.SetActive(true);
            workingRoot.gameObject.SetActive(false);
            EditorUtility.SetDirty(station);
            EditorSceneManager.MarkSceneDirty(station.gameObject.scene);
            Selection.activeGameObject = station.gameObject;

            string creationSummary = createdBroken || createdWorking
                ? "Created the Broken and Working Scene model objects."
                : "Reused the existing Broken and Working Scene model objects.";
            string animationSummary = workingClip != null
                ? $"Working animation: {workingClip.name}."
                : "No usable animation clip was found; model states were still connected.";
            Debug.Log($"{creationSummary} {animationSummary} Save the Scene to keep the setup. " +
                      "No drill prefab, UI, icon, or GameData asset was modified.", station);
        }

        [MenuItem("Mining Simulator/Setup/Create Drill Station Prefab (Safe)")]
        private static void CreateDrillStationPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(DrillPrefabPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("Mining drill prefab already exists. It was not changed or overwritten.", existing);
                return;
            }

            EnsureFolder("Assets/Prefabs", "Drill");
            MiningDrillData data = AssetDatabase.LoadAssetAtPath<MiningDrillData>(DrillDataPath);
            if (data == null)
            {
                Debug.LogError($"Missing drill data at {DrillDataPath}. Reimport the project before creating the prefab.");
                return;
            }

            GameObject root = new("Mining Drill Station", typeof(BoxCollider),
                typeof(MiningDrillStation));
            try
            {
                BoxCollider collider = root.GetComponent<BoxCollider>();
                collider.center = new Vector3(0f, 1.5f, 0f);
                collider.size = new Vector3(3f, 3f, 3f);

                Transform modelMount = CreateTransform(root.transform, "Blender Model Mount");
                Transform drillHeadMount = CreateTransform(modelMount, "Drill Head Mount");

                GameObject lightObject = new("Affordable Light", typeof(Light));
                lightObject.transform.SetParent(root.transform, false);
                lightObject.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                Light statusLight = lightObject.GetComponent<Light>();
                statusLight.type = LightType.Point;
                statusLight.range = 6f;
                statusLight.intensity = 0f;

                MiningDrillPanel panel = CreatePanel(root.transform);
                SerializedObject station = new(root.GetComponent<MiningDrillStation>());
                station.FindProperty("drillData").objectReferenceValue = data;
                station.FindProperty("drillPanel").objectReferenceValue = panel;
                station.FindProperty("modelRoot").objectReferenceValue = modelMount;
                station.FindProperty("drillHead").objectReferenceValue = drillHeadMount;
                station.FindProperty("statusLight").objectReferenceValue = statusLight;
                station.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DrillPrefabPath);
                AssetDatabase.SaveAssets();
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log("Created a new drill prefab without modifying any existing UI, icon, scene, or prefab. " +
                          "Add the Blender model under 'Blender Model Mount' and assign its renderers.", prefab);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static MiningDrillPanel CreatePanel(Transform stationRoot)
        {
            GameObject canvasObject = new("Drill HUD Canvas", typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(stationRoot, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = CreateUiObject(canvasObject.transform, "Drill Panel",
                typeof(Image), typeof(CanvasGroup), typeof(MiningDrillPanel));
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500f, 360f);
            panelRect.anchoredPosition = Vector2.zero;
            panelObject.GetComponent<Image>().color = new Color(0.94f, 0.98f, 1f, 0.98f);

            TextMeshProUGUI title = CreateText(panelRect, "Title", "MÁY KHOAN TỰ ĐỘNG",
                new Vector2(0f, 130f), new Vector2(440f, 50f), 30f);
            TextMeshProUGUI level = CreateText(panelRect, "Level", "CHƯA MUA",
                new Vector2(0f, 70f), new Vector2(420f, 40f), 24f);
            TextMeshProUGUI production = CreateText(panelRect, "Production", "-",
                new Vector2(0f, 25f), new Vector2(420f, 40f), 21f);
            TextMeshProUGUI next = CreateText(panelRect, "Next Level", "Cấp tiếp",
                new Vector2(0f, -25f), new Vector2(420f, 45f), 19f);
            Button actionButton = CreateButton(panelRect, "Purchase Or Upgrade", new Vector2(0f, -90f),
                new Vector2(300f, 58f), new Color(0.15f, 0.82f, 0.35f, 1f), out TextMeshProUGUI action);
            Button closeButton = CreateButton(panelRect, "Close", new Vector2(210f, 155f),
                new Vector2(42f, 42f), new Color(0.95f, 0.18f, 0.28f, 1f), out TextMeshProUGUI close);
            close.text = "X";

            MiningDrillPanel panel = panelObject.GetComponent<MiningDrillPanel>();
            SerializedObject serializedPanel = new(panel);
            serializedPanel.FindProperty("panelRoot").objectReferenceValue = panelRect;
            serializedPanel.FindProperty("purchaseOrUpgradeButton").objectReferenceValue = actionButton;
            serializedPanel.FindProperty("closeButton").objectReferenceValue = closeButton;
            serializedPanel.FindProperty("titleLabel").objectReferenceValue = title;
            serializedPanel.FindProperty("levelLabel").objectReferenceValue = level;
            serializedPanel.FindProperty("productionLabel").objectReferenceValue = production;
            serializedPanel.FindProperty("nextLevelLabel").objectReferenceValue = next;
            serializedPanel.FindProperty("actionLabel").objectReferenceValue = action;
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 position,
            Vector2 size, Color color, out TextMeshProUGUI label)
        {
            GameObject buttonObject = CreateUiObject(parent, name, typeof(Image), typeof(Button),
                typeof(SmoothButtonPunch));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetRect(rect, position, size);
            buttonObject.GetComponent<Image>().color = color;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            buttonObject.GetComponent<SmoothButtonPunch>().SetTarget(rect);
            label = CreateText(rect, "Label", name, Vector2.zero, size, 20f);
            return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string value,
            Vector2 position, Vector2 size, float fontSize)
        {
            GameObject textObject = CreateUiObject(parent, name, typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetRect(rect, position, size);
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = value;
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.color = new Color(0.08f, 0.11f, 0.16f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            return label;
        }

        private static GameObject CreateUiObject(Transform parent, string name,
            params System.Type[] components)
        {
            GameObject result = new(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            foreach (System.Type component in components)
            {
                if (component != typeof(RectTransform))
                {
                    result.AddComponent(component);
                }
            }
            return result;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Transform CreateTransform(Transform parent, string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static Transform GetOrCreateModelInstance(Transform stationRoot, GameObject sourceModel,
            string objectName, out bool created)
        {
            Transform existing = stationRoot.Find(objectName);
            if (existing != null)
            {
                created = false;
                return existing;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(sourceModel, stationRoot) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"Could not instantiate {sourceModel.name} in the Scene.");
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Create {objectName}");
            instance.name = objectName;
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            created = true;
            return instance.transform;
        }

        private static void ConfigureVisualState(Transform root, bool showBrokenParts)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = IsBrokenPart(renderer.transform, root) == showBrokenParts;
            }

            // Cameras and Blender scene lights are authoring helpers, not gameplay objects.
            foreach (Camera modelCamera in root.GetComponentsInChildren<Camera>(true))
            {
                modelCamera.enabled = false;
            }

            foreach (Light modelLight in root.GetComponentsInChildren<Light>(true))
            {
                modelLight.enabled = false;
            }
        }

        private static bool IsBrokenPart(Transform item, Transform root)
        {
            Transform current = item;
            while (current != null && current != root)
            {
                string itemName = current.name;
                if (itemName.EndsWith("_Broken", StringComparison.OrdinalIgnoreCase) ||
                    itemName.StartsWith("Broken_", StringComparison.OrdinalIgnoreCase) ||
                    itemName.StartsWith("Debris_", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static AnimationClip PrepareAndFindWorkingClip()
        {
            if (AssetImporter.GetAtPath(PushedDrillModelPath) is ModelImporter importer)
            {
                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }

                bool changed = false;
                foreach (ModelImporterClipAnimation clip in clips)
                {
                    if (!clip.loopTime || !clip.loopPose)
                    {
                        clip.loopTime = true;
                        clip.loopPose = true;
                        changed = true;
                    }
                }

                if (changed || importer.clipAnimations.Length == 0)
                {
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
            }

            AnimationClip[] importedClips = AssetDatabase.LoadAllAssetsAtPath(PushedDrillModelPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return importedClips.FirstOrDefault(clip =>
                       clip.name.Equals("Drill_Spin|Drill_SpinAction", StringComparison.OrdinalIgnoreCase))
                   ?? importedClips.FirstOrDefault(clip =>
                       clip.name.Contains("Drill_SpinAction", StringComparison.OrdinalIgnoreCase) &&
                       !clip.name.EndsWith(".001", StringComparison.OrdinalIgnoreCase))
                   ?? importedClips.FirstOrDefault();
        }

        private static AnimatorController GetOrCreateWorkingController(AnimationClip workingClip)
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(WorkingControllerPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder("Assets/Prefabs", "Drill");
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(
                WorkingControllerPath, workingClip);
            controller.name = "MiningDrillWorking";
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
