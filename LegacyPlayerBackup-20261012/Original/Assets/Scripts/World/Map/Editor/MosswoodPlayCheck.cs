using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MiningSimulator.Ores;

// Opt-in test only: a separate scene and a reset temporary save, never the user's gameplay scene.
[InitializeOnLoad]
public static class MosswoodPlayCheck
{
    const string Key = "Mosswood.PlayCheck";
    const string ScenePath = "Assets/GameData/Maps/Mosswood/MosswoodPlayCheck.unity";
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
    static PlayerDeathRespawn respawn;
    static MiningCharacterHealth health;
    static GameObject player;
    static TreasureChest chest;
    static readonly List<MushroomMonster> monsters = new();
    static readonly List<float> resetMs = new();
    static readonly List<float> frameMs = new();
    static int frame, cycle, phase;
    static double began, phaseBegan;
    static bool ready;
    static string report;

    static MosswoodPlayCheck()
    {
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.update += Tick;
    }

    public static string Status => SessionState.GetString(Key + ".report", "Not run");

    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Key, false))
            throw new InvalidOperationException("Another Play session is active.");
        if (EditorSceneManager.playModeStartScene != null)
            throw new InvalidOperationException("Existing start scene override: refusing to replace it.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("Test scene already exists; inspect it before rerunning.");
        var original = SceneManager.GetActiveScene();
        var source = original.GetRootGameObjects().First(x => x.GetComponent<PlayerDeathRespawn>() != null);
        var test = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var copy = UnityEngine.Object.Instantiate(source);
            copy.name = "Validation Player"; copy.SetActive(false);
            SceneManager.MoveGameObjectToScene(copy, test);
            copy.transform.SetPositionAndRotation(new Vector3(0, .12f, 20), Quaternion.identity);
            // Internal clone references survive; external scene references must not be serialized into the test.
            foreach (var component in copy.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var reference = property.objectReferenceValue;
                    var go = reference as GameObject ?? (reference as Component)?.gameObject;
                    if (go != null && go.scene == original) property.objectReferenceValue = null;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/GameData/Maps/Mosswood/MosswoodDefenseMap.prefab"), test);
            var cameraRoot = new GameObject("Validation camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraRoot, test);
            cameraRoot.tag = "MainCamera";
            cameraRoot.transform.SetPositionAndRotation(new Vector3(18, 16, -24),
                Quaternion.LookRotation(new Vector3(0, 1, 5) - new Vector3(18, 16, -24)));
            cameraRoot.GetComponent<Camera>().farClipPlane = 140;
            var lightRoot = new GameObject("Validation sun", typeof(Light));
            SceneManager.MoveGameObjectToScene(lightRoot, test);
            lightRoot.transform.rotation = Quaternion.Euler(50, -35, 0);
            var sun = lightRoot.GetComponent<Light>(); sun.type = LightType.Directional;
            sun.color = new Color(1, .91f, .78f); sun.intensity = 1.35f;
            EditorSceneManager.SaveScene(test, ScenePath);
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(test, true);
        }
        SessionState.SetBool(Key + ".background", Application.runInBackground);
        SessionState.SetString(Key + ".original", original.path);
        SessionState.SetBool(Key, true); SessionState.SetString(Key + ".report", "Starting isolated Play check");
        Application.runInBackground = true;
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        EditorApplication.isPlaying = true;
    }

    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorSceneManager.playModeStartScene = null;
            Application.runInBackground = SessionState.GetBool(Key + ".background", false);
            typeof(GameSave).GetField("testStore", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            SessionState.SetBool(Key, false); ready = false;
            AssetDatabase.DeleteAsset(ScenePath);
            return;
        }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        EditorSceneManager.playModeStartScene = null; // Never leave a test override behind.
        try
        {
            var store = new GameSaveStore(System.IO.Path.GetFullPath("Temp/MosswoodSave-" + Guid.NewGuid().ToString("N") + ".json"));
            store.Reset(); // No migration from the user's PlayerPrefs.
            typeof(GameSave).GetField("testStore", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, store);
            player = SceneManager.GetActiveScene().GetRootGameObjects().First(x => x.name == "Validation Player");
            respawn = player.GetComponent<PlayerDeathRespawn>(); health = player.GetComponent<MiningCharacterHealth>();
            var panel = new GameObject("Validation respawn visibility"); panel.SetActive(false);
            typeof(PlayerDeathRespawn).GetField("respawnPanel", F).SetValue(respawn, panel);
            player.SetActive(true);
            var grid = new GameObject("Validation A star").AddComponent<WorldNavigationGrid>();
            grid.ConfigureArea(Vector3.zero, new Vector2(80, 80));
            grid.Configure(1, .55f, 1.8f, 45, .5f, ~0, 512, 256);
            Physics.SyncTransforms(); grid.Rebake();
            frame = Time.frameCount; began = phaseBegan = EditorApplication.timeSinceStartup;
            phase = cycle = 0; resetMs.Clear(); frameMs.Clear(); monsters.Clear(); ready = true;
            report = "Actual scene player clone; temporary save; ";
            EditorApplication.isPaused = false;
        }
        catch (Exception e) { Finish("FAIL setup: " + e.GetBaseException().Message); }
    }

    static void Tick()
    {
        if (!ready || !EditorApplication.isPlaying) return;
        if (EditorApplication.timeSinceStartup - began > 110)
        { Finish("FAIL timeout phase " + phase + "; " + report); return; }
        if (frame == Time.frameCount) { EditorApplication.QueuePlayerLoopUpdate(); return; }
        frame = Time.frameCount;
        try
        {
            if (phase == 0)
            {
                frameMs.Add(Time.unscaledDeltaTime * 1000);
                if (EditorApplication.timeSinceStartup - phaseBegan < 3) return;
                health.DealDamage(100000, CombatDamageType.True);
                if (!player.GetComponent<PlayerKnockbackRagdoll>().IsIncapacitated)
                    throw new Exception("Actual player did not enter death ragdoll.");
                phase = 1; phaseBegan = EditorApplication.timeSinceStartup;
            }
            else if (phase == 1)
            {
                bool landed = (bool)typeof(PlayerDeathRespawn).GetField("landed", F).GetValue(respawn);
                var panel = (GameObject)typeof(PlayerDeathRespawn).GetField("respawnPanel", F).GetValue(respawn);
                if (!landed && panel.activeSelf) throw new Exception("Respawn panel displayed before landing.");
                if (!landed) return;
                var clock = System.Diagnostics.Stopwatch.StartNew(); respawn.RespawnNow(); clock.Stop();
                if (health.Health <= 0) throw new Exception("Landed player cannot respawn at clear spawn.");
                resetMs.Add((float)clock.Elapsed.TotalMilliseconds);
                if (!player.GetComponent<CharacterController>().enabled || !player.GetComponent<Animator>().enabled)
                    throw new Exception("Controller/Animator did not restore.");
                cycle++; phase = cycle < 5 ? 0 : 2; phaseBegan = EditorApplication.timeSinceStartup;
            }
            else if (phase == 2)
            {
                report += "5 grounded ragdoll/respawn cycles PASS; reset call ms=" + string.Join(",", resetMs.Select(x=>x.ToString("F2"))) +
                    "; baseline median frame=" + frameMs.OrderBy(x=>x).ElementAt(frameMs.Count/2).ToString("F2") + "ms (Editor, not GPU benchmark); ";
                player.GetComponent<CharacterController>().enabled = false;
                player.transform.position = new Vector3(0, .12f, 37);
                player.GetComponent<CharacterController>().enabled = true;
                var fixture = new GameObject("Validation encounters"); fixture.SetActive(false);
                var data = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TreasureChestData>("Assets/GameData/Base/TreasureChestData.asset"));
                chest = UnityEngine.Object.Instantiate(data.prefab, fixture.transform);
                typeof(TreasureChest).GetField("persistProgress", F).SetValue(chest, false);
                chest.transform.position = Vector3.zero;
                fixture.SetActive(true);
                chest.Health.ConfigureSpawnHealth(100000);
                for (int i=0;i<4;i++)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/MushroomMonster.prefab");
                    var monsterRoot = UnityEngine.Object.Instantiate(prefab, Quaternion.Euler(0,i*90,0)*Vector3.forward*18+Vector3.up*.12f, Quaternion.identity);
                    var monster = monsterRoot.GetComponent<MushroomMonster>();
                    monster.Initialize(null, health); monsters.Add(monster);
                }
                phase=3; phaseBegan=EditorApplication.timeSinceStartup;
            }
            else if (phase == 3)
            {
                if (chest.Health.Health < chest.Health.MaxHealth && monsters.All(x=>Vector3.Distance(x.transform.position,chest.transform.position)<6))
                    Finish("PASS: " + report + "4 real mushrooms approached chest; actual chest damage confirmed.");
                else if (EditorApplication.timeSinceStartup-phaseBegan > 55)
                    Finish("FAIL approach: " + report + string.Join(";",monsters.Select(x=>x.NavigationStatus+" distance="+Vector3.Distance(x.transform.position,chest.transform.position).ToString("F1"))) + "; chest HP="+chest.Health.Health);
            }
        }
        catch (Exception e) { Finish("FAIL: " + e.GetBaseException().Message + "; " + report); }
    }

    static void Finish(string result)
    {
        SessionState.SetString(Key + ".report", result); Debug.Log("MOSSWOOD PLAY CHECK: " + result);
        ready = false; EditorApplication.isPlaying = false;
    }
}
