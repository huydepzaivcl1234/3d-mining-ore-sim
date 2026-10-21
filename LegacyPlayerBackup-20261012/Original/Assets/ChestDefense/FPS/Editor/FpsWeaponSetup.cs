#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiningSimulator.Ores;
using StarterAssets;

namespace ChestDefense.Fps.Editor
{
    public static class FpsWeaponSetup
    {
        const string Root = "Assets/ChestDefense/FPS";
        const string Generated = Root + "/Generated";
        [MenuItem("Chest Defense/FPS/3. Prepare reversible F8 trial on selected player")]
        public static void PrepareTrial()
        {
            if (Application.isPlaying) return;
            SetupPlayer();
            var player = Selection.activeGameObject;
            if (player == null) return;
            var weapons = player.GetComponent<FpsWeaponController>();
            var camera = player.GetComponent<FpsPlayerCamera>();
            if (weapons == null || camera == null) return;
            Undo.RecordObjects(new UnityEngine.Object[] { weapons, camera }, "Prepare FPS trial");
            weapons.saveAmmo = false;
            weapons.enabled = false; camera.enabled = false;
            var trial = player.GetComponent<FpsTrialToggle>() ?? Undo.AddComponent<FpsTrialToggle>(player);
            Undo.RecordObject(trial, "Bind FPS trial");
            trial.weapons = weapons; trial.cameraRig = camera;
            EditorUtility.SetDirty(weapons); EditorUtility.SetDirty(camera); EditorUtility.SetDirty(trial);
            Debug.Log("FPS trial ready: Play then F8 toggles guns / existing melee. Trial ammo is not saved. Scene is not saved automatically.");
        }
        [MenuItem("Chest Defense/FPS/1. Create Glock and AKM assets")]
        public static void CreateAssets()
        {
            if (Application.isPlaying) return;
            EnsureFolder(Generated);
            CreateGun("Pistol", false); CreateGun("Rifle", true);
            AssetDatabase.SaveAssets();
        }
        static FpsWeaponData CreateGun(string name, bool rifle)
        {
            string path = Generated + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<FpsWeaponData>(path);
            if (existing != null) return existing; // Never overwrite authored configuration.
            string modelPath = Root + "/Models/" + name + "_FPS.fbx";
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Missing model: " + modelPath);
            importer.animationType = ModelImporterAnimationType.Generic;
            // Supplied FBXs measure ~100x their meter-space mount/animation design in this Editor.
            importer.globalScale = .01f;
            importer.importAnimation = true;
            var settings = importer.defaultClipAnimations;
            foreach (var clip in settings)
            { clip.loopTime = clip.name.Contains("Idle"); clip.loopPose = clip.loopTime; }
            importer.clipAnimations = settings; importer.SaveAndReimport();
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .OrderBy(c => c.name.Contains("RootNode") ? 1 : 0).ToArray();
            var idle = clips.FirstOrDefault(c => c.name.Contains("Idle"));
            var shoot = clips.FirstOrDefault(c => c.name.Contains("Shoot"));
            var reload = clips.FirstOrDefault(c => c.name.Contains("Reload"));
            if (idle == null || shoot == null || reload == null) throw new InvalidOperationException("Missing animation takes: " + modelPath);
            string prefabPath = Generated + "/" + name + "View.prefab";
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var prefab = existingPrefab != null ? existingPrefab.GetComponent<FpsGunView>() : null;
            if (prefab == null)
            {
                var root = new GameObject(name + "View");
                try
                {
                    var view = root.AddComponent<FpsGunView>();
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
                    model.transform.SetParent(root.transform, false);
                    foreach (var c in model.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
                    view.rig = model.GetComponentInChildren<Animator>(true) ?? model.AddComponent<Animator>();
                    view.rig.runtimeAnimatorController = null; view.rig.applyRootMotion = false;
                    view.handRenderers = model.GetComponentsInChildren<Renderer>(true).Where(r => r.name == "ArmModel").ToArray();
                    foreach (var hand in view.handRenderers) hand.enabled = false;
                    var muzzle = new GameObject("Muzzle (adjust to barrel tip)").transform;
                    muzzle.SetParent(root.transform, false);
                    muzzle.localPosition = rifle ? new Vector3(.496f, 1.42f, .076f) : new Vector3(-.3676f, 1.688f, .148f);
                    if (!rifle)
                    {
                        var barrelTip = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Barrel_end");
                        if (barrelTip != null)
                        {
                            muzzle.SetParent(barrelTip, false);
                            muzzle.localPosition = Vector3.zero;
                        }
                    }
                    view.muzzle = muzzle;
                    view.audioSource = root.AddComponent<AudioSource>();
                    view.audioSource.playOnAwake = false; view.audioSource.spatialBlend = 0;
                    PrepareMaterials(model, name);
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath).GetComponent<FpsGunView>();
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var data = ScriptableObject.CreateInstance<FpsWeaponData>();
            data.weaponId = rifle ? "akm" : "glock"; data.nameKey = rifle ? "FPS_RIFLE" : "FPS_PISTOL";
            data.displayName = rifle ? "AKM" : "Glock"; data.viewPrefab = prefab;
            data.idle = idle; data.shoot = shoot; data.reload = reload;
            // These two source rigs face opposite X directions; keep gun geometry in front of the eye.
            data.hipEuler = rifle ? new Vector3(0, -90, 0) : new Vector3(21.7f, 90, 0);
            data.hipPosition = rifle ? new Vector3(.25f, -1.484f, .55f) : new Vector3(-.549f, -1.603f, .30f);
            data.aimEuler = data.hipEuler;
            data.aimPosition = rifle ? new Vector3(.076f, -1.404f, .40f) : new Vector3(-.719f, -1.533f, .25f);
            data.fireMode = rifle ? FpsFireMode.Automatic : FpsFireMode.SemiAutomatic;
            data.roundsPerSecond = rifle ? 8 : 4;
            data.minimumShotInterval = rifle ? .125f : .35f;
            data.magazineCapacity = rifle ? 30 : 12;
            data.startingReserve = rifle ? 120 : 60; data.maximumReserve = rifle ? 360 : 240;
            data.damageMultiplier = rifle ? .65f : 1;
            data.reloadSeconds = reload.length;
            data.shotSound = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/" + (rifle ? "RifleShot.wav" : "PistolShot.wav"));
            data.reloadSound = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Reload.wav");
            data.emptySound = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Empty.wav");
            AssetDatabase.CreateAsset(data, path); return data;
        }
        static void PrepareMaterials(GameObject model, string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                {
                    if (source == null) return null;
                    string path = Generated + "/" + name + "_" + source.name.Replace('/', '_') + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material != null) return material;
                    var color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
                    material = new Material(shader) { color = color };
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    AssetDatabase.CreateAsset(material, path); return material;
                }).ToArray();
        }
        [MenuItem("Chest Defense/FPS/2. Setup selected player and HUD")]
        public static void SetupPlayer()
        {
            if (Application.isPlaying) return;
            var player = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<MiningPlayerStats>() : null;
            if (player == null) { Debug.LogError("Select your existing PLAYER with MiningPlayerStats, not an NPC."); return; }
            var movement = player.GetComponent<ThirdPersonController>();
            var orbit = UnityEngine.Object.FindFirstObjectByType<MiningOrbitCamera>(FindObjectsInactive.Include);
            var cam = orbit != null && orbit.ControlledCamera != null ? orbit.ControlledCamera : Camera.main;
            if (movement == null || player.GetComponent<StarterAssetsInputs>() == null || cam == null || orbit == null)
            { Debug.LogError("Selected player needs its existing Starter Assets controller/input, orbit camera and main camera."); return; }
            CreateAssets();
            var pistol = AssetDatabase.LoadAssetAtPath<FpsWeaponData>(Generated + "/Pistol.asset");
            var rifle = AssetDatabase.LoadAssetAtPath<FpsWeaponData>(Generated + "/Rifle.asset");
            if (!pistol.TryValidate(out var reason) || !rifle.TryValidate(out reason)) { Debug.LogError(reason); return; }
            int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Setup FPS player");
            var weapons = player.GetComponent<FpsWeaponController>() ?? Undo.AddComponent<FpsWeaponController>(player.gameObject);
            var viewCamera = player.GetComponent<FpsPlayerCamera>() ?? Undo.AddComponent<FpsPlayerCamera>(player.gameObject);
            Undo.RecordObjects(new UnityEngine.Object[] { weapons, viewCamera }, "Bind FPS player");
            var panels = UnityEngine.Object.FindFirstObjectByType<MiningUiPanelCoordinator>(FindObjectsInactive.Include);
            if (weapons.loadout == null || weapons.loadout.Length == 0) weapons.loadout = new[] { pistol, rifle };
            if (weapons.cameraRig == null) weapons.cameraRig = viewCamera;
            if (weapons.panels == null) weapons.panels = panels;
            if (weapons.audioManager == null) weapons.audioManager = UnityEngine.Object.FindFirstObjectByType<MiningAudioManager>(FindObjectsInactive.Include);
            if (weapons.keybindSettings == null) weapons.keybindSettings = UnityEngine.Object.FindFirstObjectByType<MiningKeybindSettings>(FindObjectsInactive.Include);
            if (weapons.viewsRoot == null)
            {
                var root = new GameObject("FPS Views"); Undo.RegisterCreatedObjectUndo(root, "Create FPS views");
                Undo.SetTransformParent(root.transform, cam.transform, "Attach FPS view mount");
                root.transform.localPosition = Vector3.zero; root.transform.localRotation = Quaternion.identity; weapons.viewsRoot = root.transform;
            }
            if (weapons.incompatibleInputs == null || weapons.incompatibleInputs.Length == 0)
            {
                var melee = player.GetComponent<PlayerCombatInput>();
                var loot = UnityEngine.Object.FindObjectsByType<WorldLootClickInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                weapons.incompatibleInputs = loot.Cast<Behaviour>().Concat(melee != null ? new Behaviour[] { melee } : Array.Empty<Behaviour>()).ToArray();
            }
            if (viewCamera.targetCamera == null) viewCamera.targetCamera = cam;
            if (viewCamera.orbit == null) viewCamera.orbit = orbit;
            if (viewCamera.panels == null) viewCamera.panels = panels;
            if (viewCamera.weapons == null) viewCamera.weapons = weapons;
            if (viewCamera.bodyRenderers == null || viewCamera.bodyRenderers.Length == 0)
                viewCamera.bodyRenderers = player.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (viewCamera.otherCameraDrivers == null || viewCamera.otherCameraDrivers.Length == 0)
                viewCamera.otherCameraDrivers = cam.GetComponents<Behaviour>().Where(b => b.GetType().Name.Contains("CinemachineBrain")).ToArray();
            foreach (var interaction in UnityEngine.Object.FindObjectsByType<MiningInteractionSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                SetReferenceIfMissing(interaction, "fpsWeapons", weapons);
            if (weapons.keybindSettings != null) SetReferenceIfMissing(weapons.keybindSettings, "fpsWeapons", weapons);
            var hud = UnityEngine.Object.FindFirstObjectByType<FpsWeaponHud>(FindObjectsInactive.Include);
            if (hud == null) hud = CreateHud(player.gameObject.scene, weapons);
            else if (hud.weapons == null) { Undo.RecordObject(hud, "Bind FPS HUD"); hud.weapons = weapons; EditorUtility.SetDirty(hud); }
            EditorUtility.SetDirty(weapons); EditorUtility.SetDirty(viewCamera);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene); Undo.CollapseUndoOperations(undo);
            Selection.activeGameObject = player.gameObject;
            Debug.Log("FPS is configured in the open scene. Save manually, then Play. Left-click fire, right-click aim, R reload, Q/1/2 switch, hold Alt for HUD. Inspect eye/hip/ADS/muzzle positions for your imported rig; all values remain editable. Undo restores scene setup.");
        }
        static void SetReferenceIfMissing(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(owner); var p = so.FindProperty(field);
            if (p != null && p.objectReferenceValue == null) { p.objectReferenceValue = value; so.ApplyModifiedProperties(); }
        }
        public static FpsWeaponHud CreateHud(UnityEngine.SceneManagement.Scene scene, FpsWeaponController weapons)
        {
            var go = new GameObject("FPS HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup), typeof(FpsWeaponHud));
            Undo.RegisterCreatedObjectUndo(go, "Create FPS HUD"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var hud = go.GetComponent<FpsWeaponHud>(); hud.weapons = weapons; hud.group = go.GetComponent<CanvasGroup>();
            hud.group.blocksRaycasts = false; hud.group.interactable = false;
            TMP_FontAsset font = null;
            var theme = Resources.Load<TowerUiTheme>("TowerUiTheme");
            if (theme != null) font = theme.bodyFont;
            if (font == null) font = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(t => t.font).FirstOrDefault(f => f != null);
            var card = Rect("Ammo card", go.transform, new Vector2(420, 110), new Vector2(0, 205));
            card.anchorMin = card.anchorMax = new Vector2(.5f, 0);
            Box(card, new Color32(31, 28, 39, 220));
            hud.weaponName = Label(card, "Weapon", new Vector2(-100, 28), new Vector2(180, 38), 26, font);
            hud.weaponName.text = "Glock";
            hud.ammo = Label(card, "Ammo", new Vector2(100, 28), new Vector2(180, 44), 34, font);
            hud.ammo.alignment = TextAlignmentOptions.Right; hud.ammo.text = "12 / 60";
            hud.hint = Label(card, "Controls", new Vector2(0, -20), new Vector2(390, 44), 18, font);
            hud.hint.text = "R Reload · Q Switch · Hold Alt for cursor";
            hud.reloadFill = Box(Rect("Reload progress", card, new Vector2(336, 3), new Vector2(0, -42)), new Color32(24, 178, 163, 255));
            hud.reloadFill.type = Image.Type.Filled; hud.reloadFill.fillMethod = Image.FillMethod.Horizontal; hud.reloadFill.fillAmount = 0;
            hud.crosshair = Rect("Crosshair", go.transform, Vector2.one * 24, Vector2.zero);
            for (int i = 0; i < 4; i++)
            {
                var tick = Rect("Tick", hud.crosshair, new Vector2(2, 5), Vector2.zero);
                tick.anchorMin = tick.anchorMax = i == 0 ? new Vector2(.5f, 1) : i == 1 ? new Vector2(1, .5f) : i == 2 ? new Vector2(.5f, 0) : new Vector2(0, .5f);
                if (i % 2 == 1) tick.sizeDelta = new Vector2(5, 2);
                Box(tick, Color.white);
            }
            var marker = Rect("Hit marker", go.transform, Vector2.one * 20, Vector2.zero);
            hud.hitMarker = marker.gameObject.AddComponent<CanvasGroup>(); hud.hitMarker.alpha = 0;
            for (int i = 0; i < 4; i++)
            {
                float angle = 45 + i * 90; var direction = Quaternion.Euler(0, 0, angle) * Vector3.up;
                var tick = Rect("Hit", marker, new Vector2(2, 6), (Vector2)direction * 8);
                tick.localRotation = Quaternion.Euler(0, 0, angle); Box(tick, new Color32(237, 202, 123, 255));
            }
            return hud;
        }
        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        static Image Box(RectTransform rect, Color color) { var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
        static TMP_Text Label(Transform parent, string name, Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font)
        {
            var text = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = fontSize; text.color = new Color32(255, 244, 218, 255); text.raycastTarget = false; return text;
        }
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/'); string parent = path.Substring(0, slash);
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
#endif
