using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using MiningSimulator.Ores;

namespace ChestDefense.Fps.Editor
{
    // Separate temporary scene and save; never saves or replaces SampleScene.
    [InitializeOnLoad]
    public static class FpsTrialPlayCheck
    {
        const string Key = "FPS.TrialCheck";
        const string Path = "Assets/ChestDefense/FPS/Generated/FpsTrialCheck.unity";
        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static GameObject player, wall;
        static Camera camera;
        static FpsTrialToggle trial;
        static MiningCharacterHealth target;
        static bool ready;
        static int phase, frame;
        static double started, changed;
        static float originalFov;
        static Mouse testMouse;
        static Mouse previousMouse;
        static Keyboard testKeyboard, previousKeyboard;
        static FpsWeaponHud hud;
        static bool reloadMoved;
        static Vector3 magazineStart;
        static bool shotCaptured;
        static float beforeHealth, beforePitch;
        static int beforeAmmo;
        public static string Status => SessionState.GetString(Key + ".result", "Not run");
        public static string VisualStatus => SessionState.GetString(Key + ".visual", "Not run");
        static FpsTrialPlayCheck()
        { EditorApplication.playModeStateChanged += State; EditorApplication.update += Tick; }
        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.playModeStartScene != null)
                throw new InvalidOperationException("An existing Play session/start override is active.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Path) != null)
                throw new InvalidOperationException("Temporary check scene already exists; inspect before running.");
            var source = UnityEngine.Object.FindFirstObjectByType<FpsTrialToggle>(FindObjectsInactive.Include);
            if (source == null) throw new Exception("Prepare the F8 trial first.");
            var original = SceneManager.GetActiveScene();
            var test = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var clone = UnityEngine.Object.Instantiate(source.gameObject);
                clone.name = "FPS validation player"; clone.SetActive(false);
                SceneManager.MoveGameObjectToScene(clone, test);
                clone.transform.SetPositionAndRotation(new Vector3(0, .1f, 0), Quaternion.identity);
                foreach (var component in clone.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;
                    var serialized = new SerializedObject(component); var p = serialized.GetIterator();
                    while (p.Next(true))
                    {
                        if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                        var reference = p.objectReferenceValue;
                        var go = reference as GameObject ?? (reference as Component)?.gameObject;
                        if (go != null && go.scene == original) p.objectReferenceValue = null;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var camRoot = new GameObject("FPS validation camera", typeof(Camera), typeof(AudioListener));
                SceneManager.MoveGameObjectToScene(camRoot, test); camRoot.tag = "MainCamera";
                var orbit = camRoot.AddComponent<MiningOrbitCamera>();
                var sourceOrbit = new SerializedObject(source.cameraRig.orbit);
                var testOrbit = new SerializedObject(orbit);
                testOrbit.FindProperty("gameData").objectReferenceValue = sourceOrbit.FindProperty("gameData").objectReferenceValue;
                testOrbit.FindProperty("controlledCamera").objectReferenceValue = camRoot.GetComponent<Camera>();
                testOrbit.FindProperty("followTarget").objectReferenceValue = clone.transform;
                testOrbit.ApplyModifiedPropertiesWithoutUndo();
                var sun = new GameObject("FPS validation sun", typeof(Light)); SceneManager.MoveGameObjectToScene(sun, test);
                sun.GetComponent<Light>().type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(40, -30, 0);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(floor, test);
                floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(30, 1, 30);
                FpsWeaponSetup.CreateHud(test, clone.GetComponent<FpsWeaponController>());
                EditorSceneManager.SaveScene(test, Path);
            }
            finally { SceneManager.SetActiveScene(original); EditorSceneManager.CloseScene(test, true); }
            SessionState.SetBool(Key, true); SessionState.SetBool(Key + ".background", Application.runInBackground);
            SessionState.SetString(Key + ".result", "Starting isolated FPS Play check");
            SessionState.SetString(Key + ".visual", "");
            Application.runInBackground = true;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Path);
            EditorApplication.isPlaying = true;
        }
        static void State(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorSceneManager.playModeStartScene = null;
                typeof(GameSave).GetField("testStore", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
                Application.runInBackground = SessionState.GetBool(Key + ".background", false);
                if (testMouse != null) InputSystem.RemoveDevice(testMouse);
                if (testKeyboard != null) InputSystem.RemoveDevice(testKeyboard);
                if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
                testMouse = null;
                if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
                SessionState.SetBool(Key, false); ready = false; AssetDatabase.DeleteAsset(Path); return;
            }
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorSceneManager.playModeStartScene = null;
            try
            {
                var store = new GameSaveStore(System.IO.Path.GetFullPath("Temp/FpsTrial-" + Guid.NewGuid().ToString("N") + ".json"));
                store.Reset(); typeof(GameSave).GetField("testStore", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, store);
                player = SceneManager.GetActiveScene().GetRootGameObjects().First(x => x.name == "FPS validation player");
                camera = Camera.main; originalFov = camera.fieldOfView;
                trial = player.GetComponent<FpsTrialToggle>(); trial.cameraRig.targetCamera = camera;
                trial.cameraRig.orbit = camera.GetComponent<MiningOrbitCamera>(); trial.cameraRig.otherCameraDrivers = Array.Empty<Behaviour>();
                var mount = new GameObject("Validation view mount"); mount.transform.SetParent(camera.transform, false);
                trial.weapons.viewsRoot = mount.transform; trial.weapons.saveAmmo = false;
                // Death ownership is checked through health without exercising unrelated checkpoint/ragdoll saves.
                var respawn = player.GetComponent<PlayerDeathRespawn>(); if (respawn != null) respawn.enabled = false;
                var ragdoll = player.GetComponent<PlayerKnockbackRagdoll>(); if (ragdoll != null) ragdoll.enabled = false;
                player.SetActive(true); trial.SetActive(true);
                var dummy = GameObject.CreatePrimitive(PrimitiveType.Cube); dummy.name = "Validation monster";
                dummy.transform.position = new Vector3(0, 1.8f, 8); dummy.transform.localScale = new Vector3(2, 2, 1);
                var monster = dummy.AddComponent<MushroomMonster>(); monster.enabled = false;
                dummy.GetComponent<CharacterController>().enabled = false;
                target = monster.Health; target.ConfigureSpawnHealth(1000);
                previousMouse = Mouse.current; testMouse = InputSystem.AddDevice<Mouse>("FPS validation mouse");
                testMouse.MakeCurrent();
                trial.weapons.fire.ApplyBindingOverride(testMouse.leftButton.path);
                previousKeyboard = Keyboard.current; testKeyboard = InputSystem.AddDevice<Keyboard>("FPS validation keyboard");
                trial.weapons.reload.ApplyBindingOverride(testKeyboard.rKey.path);
                hud = UnityEngine.Object.FindFirstObjectByType<FpsWeaponHud>();
                // Include the HUD in Camera.Render captures; this is the isolated test Canvas only.
                var hudCanvas = hud.GetComponent<Canvas>();
                hudCanvas.renderMode = RenderMode.ScreenSpaceCamera; hudCanvas.worldCamera = camera; hudCanvas.planeDistance = .05f;
                reloadMoved = false; shotCaptured = false;
                var gameView = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x => x.GetType().Name == "GameView");
                if (gameView != null) gameView.Focus();
                started = changed = EditorApplication.timeSinceStartup; phase = 0; frame = Time.frameCount; ready = true;
            }
            catch (Exception e) { Finish("FAIL setup: " + e); }
        }
        static void Tick()
        {
            if (!ready || !EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup - started > 60) { Finish("FAIL timeout phase " + phase); return; }
            if (frame == Time.frameCount) { EditorApplication.QueuePlayerLoopUpdate(); return; }
            frame = Time.frameCount;
            if (phase == 8 && trial.weapons.CurrentMagazine.Loaded < beforeAmmo)
            {
                try
                {
                    var gun = camera.GetComponentsInChildren<FpsGunView>().First(v => v.gameObject.activeInHierarchy);
                    var root = gun.rig.GetComponentsInChildren<Transform>(true).First(t => t.name == "Root");
                    Require(Vector3.Dot(-root.up, camera.transform.forward) > .9f, "Rifle turns away while firing");
                    if (!shotCaptured) { CaptureView("RifleShot"); shotCaptured = true; }
                }
                catch (Exception e) { Finish("FAIL moving shot: " + e.Message); return; }
            }
            if (phase == 12 || phase == 16)
            {
                var gun = camera.GetComponentsInChildren<FpsGunView>().FirstOrDefault(v => v.gameObject.activeInHierarchy);
                if (gun != null && trial.weapons.CurrentMagazine.Reloading)
                {
                    var magazine = gun.rig.GetComponentsInChildren<Transform>(true).First(t => t.name == "Magazine");
                    reloadMoved |= Vector3.Distance(gun.rig.transform.InverseTransformPoint(magazine.position), magazineStart) > .02f;
                }
            }
            // Capture ADS after the authored smooth transition has settled, not mid-raise.
            double settle = phase == 10 || phase == 11 ? 1.2 : .6;
            if (EditorApplication.timeSinceStartup - changed < settle) return;
            try
            {
                if (phase == 0)
                {
                    Require(trial.weapons.HasWeapon && trial.cameraRig.OwnsView, "FPS did not capture camera");
                    Require(trial.weapons.CanUseWeapon, "Fire blocked: focus=" + Application.isFocused + " orbitLock=" + trial.cameraRig.orbit.IsInputLocked);
                    beforePitch = (float)typeof(FpsPlayerCamera).GetField("pitch", Private).GetValue(trial.cameraRig);
                    InputSystem.QueueStateEvent(testMouse, new MouseState { delta = new Vector2(0, 20) });
                }
                else if (phase == 1)
                {
                    float pitch = (float)typeof(FpsPlayerCamera).GetField("pitch", Private).GetValue(trial.cameraRig);
                    float expected = 20 * trial.cameraRig.orbit.MouseDegreesPerPixel * trial.cameraRig.orbit.MouseSensitivity;
                    Require(Mathf.Abs((beforePitch - pitch) - expected) < .01f, "Mouse-up direction/sensitivity mismatch: " + (beforePitch-pitch) + " expected=" + expected);
                    typeof(FpsPlayerCamera).GetField("pitch", Private).SetValue(trial.cameraRig, 0f);
                    camera.transform.rotation = Quaternion.identity;
                    beforeHealth = target.Health; beforeAmmo = trial.weapons.CurrentMagazine.Loaded;
                    InputSystem.QueueStateEvent(testMouse, new MouseState().WithButton(MouseButton.Left));
                }
                else if (phase == 2)
                {
                    Require(target.Health < beforeHealth && trial.weapons.CurrentMagazine.Loaded == beforeAmmo-1, "Real Fire action did not consume ammo and damage target");
                    InputSystem.QueueStateEvent(testMouse, new MouseState());
                    CaptureView("Pistol");
                    wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = new Vector3(0, 1.8f, 4);
                    wall.transform.localScale = new Vector3(3, 3, 1); Physics.SyncTransforms();
                    beforeHealth = target.Health;
                }
                else if (phase == 3)
                {
                    InputSystem.QueueStateEvent(testMouse, new MouseState().WithButton(MouseButton.Left));
                }
                else if (phase == 4)
                {
                    Require(target.Health == beforeHealth, "Shot passed through wall");
                    InputSystem.QueueStateEvent(testMouse, new MouseState());
                    UnityEngine.Object.Destroy(wall);
                    Require(trial.weapons.Equip(1), "AKM equip failed");
                }
                else if (phase == 5)
                {
                    Require(trial.weapons.CurrentWeapon.weaponId == "akm", "Wrong selected weapon");
                    CaptureView("Rifle");
                    beforeAmmo = trial.weapons.CurrentMagazine.Loaded;
                    trial.cameraRig.orbit.SetInputLocked(true);
                    InputSystem.QueueStateEvent(testMouse, new MouseState().WithButton(MouseButton.Left));
                }
                else if (phase == 6)
                {
                    Require(!trial.weapons.CanUseWeapon && trial.weapons.CurrentMagazine.Loaded == beforeAmmo, "Modal lock allowed shooting");
                    InputSystem.QueueStateEvent(testMouse, new MouseState());
                    trial.cameraRig.orbit.SetInputLocked(false);
                }
                else if (phase == 7)
                {
                    Require(trial.weapons.CanUseWeapon, "Unlock did not restore gun input");
                    InputSystem.QueueStateEvent(testMouse, new MouseState().WithButton(MouseButton.Left));
                }
                else if (phase == 8)
                {
                    Require(trial.weapons.CurrentMagazine.Loaded < beforeAmmo, "Automatic fire did not resume");
                    InputSystem.QueueStateEvent(testMouse, new MouseState());
                    trial.weapons.CurrentMagazine.TryFire(Time.timeAsDouble, .1);
                    Require(trial.weapons.CurrentMagazine.BeginReload(Time.timeAsDouble, .5), "Reload did not begin");
                    trial.SetActive(false);
                    Require(!trial.weapons.CurrentMagazine.Reloading, "Disable did not cancel reload");
                }
                else if (phase == 9)
                {
                    Require(!trial.cameraRig.OwnsView && camera.fieldOfView == originalFov, "Camera FOV not restored");
                    var melee = player.GetComponent<PlayerCombatInput>(); Require(melee == null || melee.enabled, "Melee not restored");
                    trial.SetActive(true);
                    Require(trial.weapons.Equip(0), "Glock restore failed");
                    trial.weapons.aim.ApplyBindingOverride(testMouse.rightButton.path);
                    InputSystem.QueueStateEvent(testMouse, new MouseState().WithButton(MouseButton.Right));
                }
                else if (phase == 10)
                {
                    Require(trial.weapons.IsAiming && camera.fieldOfView < trial.cameraRig.standingFieldOfView - 5, "Glock ADS did not engage");
                    CaptureView("PistolADS");
                    Require(trial.weapons.Equip(1), "AKM ADS switch failed");
                }
                else if (phase == 11)
                {
                    Require(trial.weapons.IsAiming, "AKM ADS did not engage");
                    CaptureView("RifleADS");
                    InputSystem.QueueStateEvent(testMouse, new MouseState());
                    beforeAmmo = trial.weapons.CurrentMagazine.Loaded;
                    var gun = camera.GetComponentsInChildren<FpsGunView>().First(v => v.gameObject.activeInHierarchy);
                    magazineStart = gun.rig.transform.InverseTransformPoint(gun.rig.GetComponentsInChildren<Transform>(true).First(t => t.name == "Magazine").position);
                    reloadMoved = false; InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(UnityEngine.InputSystem.Key.R));
                }
                else if (phase == 12)
                {
                    Require(trial.weapons.CurrentMagazine.Reloading, "R input did not start AKM reload");
                    InputSystem.QueueStateEvent(testKeyboard, new KeyboardState()); CaptureView("RifleReload");
                }
                else if (phase == 13)
                {
                    if (trial.weapons.CurrentMagazine.Reloading) return;
                    Require(reloadMoved && trial.weapons.CurrentMagazine.Loaded > beforeAmmo, "AKM reload did not animate/commit ammo");
                    Require(hud != null && hud.ammo.text.Length > 0 && hud.ammo.textInfo.characterCount > 0, "Ammo HUD text missing");
                    Require(hud.weaponName.rectTransform.parent.GetComponent<RectTransform>().anchoredPosition.y >= 190, "Ammo HUD overlaps player HP");
                    trial.weapons.Equip(0);
                }
                else if (phase == 14)
                {
                    InputSystem.QueueStateEvent(testMouse, new MouseState().WithButton(MouseButton.Left));
                }
                else if (phase == 15)
                {
                    InputSystem.QueueStateEvent(testMouse, new MouseState()); beforeAmmo = trial.weapons.CurrentMagazine.Loaded;
                    var gun = camera.GetComponentsInChildren<FpsGunView>().First(v => v.gameObject.activeInHierarchy);
                    magazineStart = gun.rig.transform.InverseTransformPoint(gun.rig.GetComponentsInChildren<Transform>(true).First(t => t.name == "Magazine").position);
                    reloadMoved = false; InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(UnityEngine.InputSystem.Key.R));
                }
                else if (phase == 16)
                {
                    Require(trial.weapons.CurrentMagazine.Reloading, "R input did not start Glock reload");
                    InputSystem.QueueStateEvent(testKeyboard, new KeyboardState()); CaptureView("PistolReload");
                }
                else if (phase == 17)
                {
                    if (trial.weapons.CurrentMagazine.Reloading) return;
                    Require(reloadMoved && trial.weapons.CurrentMagazine.Loaded > beforeAmmo, "Glock reload did not animate/commit ammo");
                    player.GetComponent<MiningCharacterHealth>().DealDamage(100000, CombatDamageType.True);
                }
                else
                {
                    Require(!trial.cameraRig.OwnsView, "FPS retained death camera");
                    Finish("PASS Play: mouse direction/sensitivity, Fire/ammo/damage, wall occlusion, gun-only hip/ADS, rifle direction throughout automatic firing, actual R reload + animated magazines + ammo commit for both guns, HUD text/layout, modal locks, cancel, F8 rollback and death release. Virtual mouse/keyboard; user save untouched."); return;
                }
                phase++; changed = EditorApplication.timeSinceStartup;
            }
            catch (Exception e) { Finish("FAIL phase " + phase + ": " + e.GetBaseException().Message); }
        }
        static void CaptureView(string weapon)
        {
            foreach (var text in hud.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            var gun = camera.GetComponentsInChildren<FpsGunView>().First(v => v.gameObject.activeInHierarchy);
            string bounds = string.Join("\n", gun.GetComponentsInChildren<Renderer>(true).Select(r => r.name + " visible=" + r.enabled + " center=" + camera.transform.InverseTransformPoint(r.bounds.center) + " extents=" + r.bounds.extents));
            bounds += "\n" + string.Join("\n", gun.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Barrel" || t.name == "Barrel_end" || t.name == "Slide_end").Select(t => t.name + " pos=" + camera.transform.InverseTransformPoint(t.position) + " up=" + camera.transform.InverseTransformDirection(t.up)));
            bounds += "\nMuzzle=" + camera.transform.InverseTransformPoint(gun.muzzle.position) + " view=" + gun.transform.localPosition + " rig=" + gun.rig.transform.localPosition;
            bounds += "\n" + string.Join("\n", gun.rig.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Root" || t.name == "RootNode").Select(t => t.name + " pos=" + camera.transform.InverseTransformPoint(t.position) + " local=" + t.localPosition + " up=" + camera.transform.InverseTransformDirection(t.up)));
            bounds += "\n" + string.Join("\n", hud.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t => t.name + " text=" + t.text + " vertices=" + t.mesh.vertexCount + " cull=" + t.canvasRenderer.cull + " depth=" + t.canvasRenderer.absoluteDepth + " scale=" + t.transform.lossyScale + " uv=" + t.textInfo.meshInfo[0].uvs0[0]));
            SessionState.SetString(Key + ".visual", VisualStatus + "\n" + weapon + " " + bounds);
            Require(gun.handRenderers.All(r => r == null || !r.enabled), "Hand renderer still visible");
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            bool inFrustum = gun.GetComponentsInChildren<Renderer>().Any(r => r.enabled && GeometryUtility.TestPlanesAABB(planes, r.bounds));
            if (weapon.EndsWith("ADS"))
            {
                Require(Mathf.Abs(camera.transform.InverseTransformPoint(gun.muzzle.position).x) < .03f, "ADS barrel not aligned with center aim");
                Require(gun.rearSight!=null && gun.frontSight!=null,"Missing iron sight anchors");
                foreach(var sight in new[]{gun.rearSight,gun.frontSight})
                {
                    var p=camera.WorldToViewportPoint(sight.position);
                    Require(p.z>.03f && Mathf.Abs(p.x-.5f)<.02f && Mathf.Abs(p.y-.5f)<.025f,"ADS sights not centered");
                }
                Require(camera.transform.InverseTransformPoint(gun.rearSight.position).z<.25f,"ADS rear sight too far from eye");
            }
            var previous = camera.targetTexture; var active = RenderTexture.active;
            var render = new RenderTexture(960, 540, 24); var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = render; camera.Render(); RenderTexture.active = render;
                image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
                System.IO.File.WriteAllBytes("Docs/AI/FpsTrial"+weapon+".png", image.EncodeToPNG());
            }
            finally
            { camera.targetTexture = previous; RenderTexture.active = active; render.Release(); UnityEngine.Object.Destroy(render); UnityEngine.Object.Destroy(image); }
            Require(inFrustum, "Gun lies outside camera frustum");
        }
        static void Require(bool condition, string reason) { if (!condition) throw new Exception(reason); }
        static void Finish(string result)
        { ready = false; SessionState.SetString(Key + ".result", result); EditorApplication.isPlaying = false; }
    }
}
