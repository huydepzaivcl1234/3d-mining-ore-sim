#if UNITY_EDITOR
using System;
using System.Linq;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Bounded live physics check without running save/progression components.</summary>
[InitializeOnLoad]
public static class PlayerKnockbackValidation
{
    private const string Key = "Mining.PlayerKnockbackValidation";
    [Serializable] private class RootState { public string id; public bool active; }
    [Serializable] private class RootStates { public RootState[] roots; }
    private static PlayerKnockbackRagdoll subject;
    private static MiningOrbitCamera orbit;
    private static Quaternion fixedRecoveryRotation;
    private static bool capturedRecoveryRotation;
    private static float began;
    private static double wallBegan;
    private static bool sawGetUp;
    private static bool repeated;
    private static GameObject testRespawnPanel;
    private static Camera testCamera;
    private static Vector3 flightCameraOffset;
    private static float deathAt;
    private static bool checkedFlight;
    static PlayerKnockbackValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
        EditorApplication.update += Tick;
    }

    [MenuItem("Mining Simulator/Validation/Player Knockback (isolated)")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if (UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene != null)
            throw new InvalidOperationException("Clear the Play Mode Start Scene override before this isolated validation.");
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        if (!roots.Any(x => x.name == "Player")) throw new InvalidOperationException("Open the scene with Player first.");
        var state = new RootStates { roots = roots.Select(x => new RootState {
            id = GlobalObjectId.GetGlobalObjectIdSlow(x).ToString(), active = x.activeSelf }).ToArray() };
        SessionState.SetString(Key + ".roots", JsonUtility.ToJson(state));
        SessionState.SetBool(Key, true);
        SessionState.SetString(Key + ".result", "Running");
        foreach (var root in roots) root.SetActive(false);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            var saved = JsonUtility.FromJson<RootStates>(SessionState.GetString(Key + ".roots", ""));
            if (saved != null) foreach (var root in saved.roots)
            {
                if (!GlobalObjectId.TryParse(root.id, out var id)) continue;
                var go = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                if (go != null) go.SetActive(root.active);
            }
            SessionState.SetBool(Key, false);
            Debug.Log("Player knockback validation: " + SessionState.GetString(Key + ".result", "Interrupted"));
        }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try
        {
            var original = SceneManager.GetActiveScene().GetRootGameObjects().First(x => x.name == "Player");
            // Clone the whole hierarchy to retain the exact avatar binding paths, but no game logic.
            var test = UnityEngine.Object.Instantiate(original);
            test.name = "__Player Knockback Validation__";
            // Remove dependants before their RequireComponent dependencies. The clone stays inactive.
            string[] dependencyOrder = { "PlayerMonsterHeadDeflection", "PlayerDeathRespawn", "MiningHitReaction",
                "MiningPlayerStamina", "PlayerCombatInput", "PlayerKnockbackRagdoll", "ThirdPersonController",
                "MiningPlayerStats", "MiningCharacterHealth", "PlayerInput" };
            foreach (string name in dependencyOrder)
                foreach (var script in test.GetComponentsInChildren<MonoBehaviour>(true))
                    if (script != null && script.GetType().Name == name && !RetainControl(script)) UnityEngine.Object.DestroyImmediate(script);
            foreach (var script in test.GetComponentsInChildren<MonoBehaviour>(true))
                if (script != null && !RetainControl(script)) UnityEngine.Object.DestroyImmediate(script);
            foreach (var otherAnimator in test.GetComponentsInChildren<Animator>(true))
                if (otherAnimator.gameObject != test) UnityEngine.Object.DestroyImmediate(otherAnimator);
            test.transform.SetPositionAndRotation(new Vector3(10000f, 0f, 10000f), Quaternion.identity);
            var animator = test.GetComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            test.GetComponent<CharacterController>().enabled = true;
            test.AddComponent<MiningCharacterHealth>();
            subject = test.AddComponent<PlayerKnockbackRagdoll>();
            var death = test.AddComponent<PlayerDeathRespawn>();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "__Knockback Test Floor__";
            floor.transform.position = new Vector3(10000f, -.25f, 10000f);
            floor.transform.localScale = new Vector3(30f, .5f, 30f);
            var cameraObject = new GameObject("__Knockback Test Camera__");
            cameraObject.tag = "MainCamera";
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            testCamera = camera;
            camera.transform.position = test.transform.position + new Vector3(4f, 3f, -5f);
            camera.transform.LookAt(test.transform.position + Vector3.up);
            orbit = cameraObject.AddComponent<MiningOrbitCamera>();
            var cameraSetup = new SerializedObject(orbit);
            var originalOrbit = UnityEngine.Object.FindObjectsByType<MiningOrbitCamera>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).First(x => x != orbit && x.FollowTarget == original.transform);
            cameraSetup.FindProperty("gameData").objectReferenceValue =
                new SerializedObject(originalOrbit).FindProperty("gameData").objectReferenceValue;
            cameraSetup.FindProperty("controlledCamera").objectReferenceValue = camera;
            cameraSetup.FindProperty("followTarget").objectReferenceValue = test.transform;
            cameraSetup.ApplyModifiedPropertiesWithoutUndo();
            testRespawnPanel = new GameObject("__Landing Gated Respawn Panel__");
            testRespawnPanel.SetActive(false);
            var deathSetup = new SerializedObject(death);
            deathSetup.FindProperty("respawnPanel").objectReferenceValue = testRespawnPanel;
            deathSetup.FindProperty("spectatorCamera").objectReferenceValue = camera;
            deathSetup.FindProperty("cameraDrivers").arraySize = 1;
            deathSetup.FindProperty("cameraDrivers").GetArrayElementAtIndex(0).objectReferenceValue = orbit;
            deathSetup.FindProperty("lethalLaunchLift").floatValue = 5f;
            deathSetup.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("__Knockback Test Light__").AddComponent<Light>().type = LightType.Directional;
            cameraObject.SetActive(true);
            test.SetActive(true);
            orbit.SetShiftLocked(true);
            if (!subject.ApplyKnockback(new Vector3(0f, 3f, 4f))) throw new InvalidOperationException("Knockback not initialized.");
            if (!test.GetComponent<CharacterController>().enabled || test.GetComponentsInChildren<Rigidbody>().Any(x => !x.isKinematic)) throw new InvalidOperationException("Living knockback incorrectly enabled ragdoll.");
            began = Time.time;
            wallBegan = EditorApplication.timeSinceStartup;
            sawGetUp = repeated = false;
            capturedRecoveryRotation = false;
            checkedFlight = false;
        }
        catch (Exception e) { Complete("FAIL: " + e.Message); }
    }

    private static Vector3 heldPosition;
    private static float heldAt;
    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || subject == null) return;
        if (EditorApplication.isPaused) { wallBegan = EditorApplication.timeSinceStartup; return; }
        float age = Time.time - began;
        if (age > 25f || EditorApplication.timeSinceStartup - wallBegan > 40d)
        { Complete("FAIL: death ragdoll timeout; state=" + subject.State); return; }
        if (!repeated && subject.State == PlayerKnockbackRagdoll.KnockdownState.Normal && age > .6f)
        {
            if (!subject.GetComponent<CharacterController>().enabled || subject.GetComponentsInChildren<Rigidbody>().Any(x => !x.isKinematic))
            { Complete("FAIL: living knockback did not recover with capsule"); return; }
            repeated = true;
            var source = new GameObject("__Lethal Hit Source__");
            source.transform.position = subject.transform.position - Vector3.forward * 3f;
            subject.GetComponent<MiningCharacterHealth>().DealDamage(10000f, CombatDamageType.True, source);
            deathAt = Time.time;
            flightCameraOffset = testCamera.transform.position - subject.CameraFocusPosition;
            if (testRespawnPanel.activeSelf || !subject.GetComponent<PlayerDeathRespawn>().IsAwaitingLanding)
            { Complete("FAIL: respawn panel opened during launch"); return; }
            subject.GetComponent<PlayerDeathRespawn>().RespawnNow();
            if (subject.GetComponent<MiningCharacterHealth>().Health > 0f)
            { Complete("FAIL: airborne RespawnNow bypassed landing gate"); return; }
            if (subject.GetComponent<Animator>().enabled || subject.GetComponent<CharacterController>().enabled ||
                !subject.GetComponentsInChildren<Rigidbody>().Any(x => !x.isKinematic))
            { Complete("FAIL: lethal damage did not transfer ownership to ragdoll"); return; }
        }
        if (repeated && subject.GetComponent<PlayerDeathRespawn>().IsAwaitingLanding)
        {
            if (testRespawnPanel.activeSelf)
            { Complete("FAIL: respawn panel visible before landing"); return; }
            if (Time.time - deathAt > .15f)
            {
                if (Vector3.Distance(testCamera.transform.position - subject.CameraFocusPosition, flightCameraOffset) > .4f || orbit.enabled)
                { Complete("FAIL: death camera did not follow airborne hips exclusively"); return; }
                checkedFlight = true;
            }
        }
        if (!repeated || subject.State != PlayerKnockbackRagdoll.KnockdownState.DeadHeld) return;
        if (!checkedFlight || !subject.HasLanded || !testRespawnPanel.activeSelf)
        { Complete("FAIL: landing did not unlock panel after verified flight"); return; }
        if (!sawGetUp) { sawGetUp = true; heldPosition = subject.CameraFocusPosition; heldAt = Time.time; }
        if (SessionState.GetBool(Key + ".reviewDead", false))
        { SessionState.SetBool(Key + ".reviewDead", false); EditorApplication.isPaused = true; return; }
        if (Vector3.Distance(heldPosition, subject.CameraFocusPosition) > .001f)
        { Complete("FAIL: landed corpse pose moved after freezing"); return; }
        if (Time.time - heldAt < 1f) return;
        subject.GetComponent<PlayerDeathRespawn>().RespawnNow();
        if (subject.GetComponent<MiningCharacterHealth>().Health <= 0f || !subject.GetComponent<CharacterController>().enabled ||
            !subject.GetComponent<Animator>().enabled || subject.IsIncapacitated ||
            subject.GetComponentsInChildren<Rigidbody>().Any(x => !x.isKinematic))
        { Complete("FAIL: respawn failed to restore animation/capsule/physics"); return; }
        Complete("PASS: living knockback; airborne hips camera follow, hidden respawn panel and blocked early respawn -> ground contact opens panel -> held corpse -> real respawn");
    }

    private static void Complete(string result)
    {
        SessionState.SetString(Key + ".result", result);
        EditorApplication.isPlaying = false;
    }

    // Keep real movement, combat overlays, input and weapon events in the fixture.
    // Save/progression/world scripts are still removed, so validation never spends rewards.
    private static bool RetainControl(MonoBehaviour script)
    {
        string type = script.GetType().Name;
        return type == "ThirdPersonController" || type == "StarterAssetsInputs" ||
            type == "PlayerInput" || type == "PlayerCombatInput" || type == "EquipmentSystem";
    }
}
#endif
