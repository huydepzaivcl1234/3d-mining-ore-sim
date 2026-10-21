#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Explicit, bounded fixture. Original scene roots and the real save profile are never used for gameplay.
[InitializeOnLoad]
public static class ChestDefensePlayValidation
{
    private const string Key = "ChestDefense.ProgressionValidation";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    [Serializable] private class Root { public string id; public bool active; }
    [Serializable] private class Snapshot { public Root[] roots; }
    private static TreasureChest chest;
    private static TreasureChestDefenseFeedback feedback;
    private static MiningCharacterHealth playerHealth;
    private static MiningQuestSystem quests;
    private static Keyboard keyboard;
    private static GameObject fixture;
    private static int stage, frame;
    private static double started, finished;
    private static bool ready;

    static ChestDefensePlayValidation()
    { EditorApplication.playModeStateChanged += Changed; EditorApplication.update += Tick; }

    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first");
        if (UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene != null)
            throw new InvalidOperationException("Clear the Play start scene override first");
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        SessionState.SetString(Key + ".roots", JsonUtility.ToJson(new Snapshot { roots = roots.Select(r =>
            new Root { id = GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(), active = r.activeSelf }).ToArray() }));
        SessionState.SetBool(Key + ".background", Application.runInBackground);
        SessionState.SetBool(Key, true); SessionState.SetString(Key + ".result", "Running");
        foreach (var root in roots) root.SetActive(false);
        Application.runInBackground = true;
        EditorApplication.isPlaying = true;
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            var snapshot = JsonUtility.FromJson<Snapshot>(SessionState.GetString(Key + ".roots", ""));
            foreach (var root in snapshot.roots)
                if (GlobalObjectId.TryParse(root.id, out var id) &&
                    GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is GameObject go) go.SetActive(root.active);
            Application.runInBackground = SessionState.GetBool(Key + ".background", false);
            typeof(GameSave).GetField("testStore", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            foreach (var device in InputSystem.devices.ToArray())
                if (device.name == "Chest repair validation") InputSystem.RemoveDevice(device);
            SessionState.SetBool(Key, false); ready = false;
            return;
        }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try
        {
            typeof(GameSave).GetField("testStore", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null,
                new GameSaveStore(System.IO.Path.GetFullPath("Temp/ChestDefenseValidation-" + Guid.NewGuid().ToString("N") + ".json")));
            EditorApplication.isPaused = false;
            fixture = new GameObject("Isolated chest defense validation"); fixture.SetActive(false);
            var data = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TreasureChestData>("Assets/GameData/Base/TreasureChestData.asset"));
            data.emergencyRepairSeconds = .4f;
            chest = UnityEngine.Object.Instantiate(data.prefab, fixture.transform);
            typeof(TreasureChest).GetField("data", Flags).SetValue(chest, data);
            typeof(TreasureChest).GetField("persistProgress", Flags).SetValue(chest, false);
            chest.transform.position = new Vector3(15000, 0, 15000);
            var player = new GameObject("Repair test player"); player.SetActive(false); player.transform.SetParent(fixture.transform);
            var stats = player.AddComponent<MiningPlayerStats>(); stats.enabled = false;
            playerHealth = player.AddComponent<MiningCharacterHealth>();
            player.transform.position = chest.transform.position + Vector3.back * 2;
            var camera = new GameObject("Defense test camera").AddComponent<Camera>(); camera.transform.SetParent(fixture.transform);
            camera.tag = "MainCamera"; camera.transform.position = chest.transform.position + new Vector3(0, 3, -7);
            camera.transform.LookAt(chest.transform.position + Vector3.up * 1.2f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(fixture.transform);
            floor.transform.position = chest.transform.position + Vector3.down * .5f; floor.transform.localScale = new Vector3(25, 1, 25);
            var questRoot = new GameObject("Isolated quests"); questRoot.SetActive(false); questRoot.transform.SetParent(fixture.transform);
            var questData = ScriptableObject.CreateInstance<MiningQuestData>(); questData.AddDefenseQuests();
            typeof(MiningQuestData).GetField("saveKey", Flags).SetValue(questData, "Validation.Quests");
            quests = questRoot.AddComponent<MiningQuestSystem>(); typeof(MiningQuestSystem).GetField("data", Flags).SetValue(quests, questData);
            player.SetActive(true); questRoot.SetActive(true); fixture.SetActive(true);
            keyboard = InputSystem.AddDevice<Keyboard>("Chest repair validation");
            stage = 0; frame = Time.frameCount; started = EditorApplication.timeSinceStartup; ready = true;
        }
        catch (Exception e) { Finish("FAIL setup: " + e.GetBaseException().Message); }
    }

    private static void PressKey(bool held) => InputSystem.QueueStateEvent(keyboard,
        held ? new KeyboardState(UnityEngine.InputSystem.Key.E) : new KeyboardState());

    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static void Tick()
    {
        if (!ready || !EditorApplication.isPlaying || frame == Time.frameCount) return;
        frame = Time.frameCount;
        try
        {
            if (EditorApplication.timeSinceStartup - started > 30) throw new Exception("Validation timeout, stage " + stage);
            if (stage == 0)
            {
                feedback = chest.GetComponent<TreasureChestDefenseFeedback>(); if (feedback == null) return;
                var tower = new GameObject("Kill attribution cannon"); tower.SetActive(false); tower.transform.SetParent(fixture.transform);
                tower.AddComponent<TowerRuntime>().enabled = false; tower.SetActive(true);
                var species = ScriptableObject.CreateInstance<MushroomData>(); species.experience = 5;
                var victim = new GameObject("Actual death attribution"); victim.SetActive(false); victim.transform.SetParent(fixture.transform);
                var monster = victim.AddComponent<MushroomMonster>();
                typeof(MushroomMonster).GetField("rewards", Flags).SetValue(monster, species); victim.SetActive(true);
                monster.enabled = false; monster.Health.ConfigureSpawnHealth(10);
                monster.Health.DealDamage(20, CombatDamageType.True, tower);
                typeof(MushroomMonster).GetMethod("OnDeath", Flags).Invoke(monster, null);
                Check(chest.Experience == 5 && quests.GetProgress("daily_mushroom") == 1, "Tower kill XP/count or duplicate guard failed");
                var expired = new GameObject("Despawn must not reward"); expired.SetActive(false); expired.transform.SetParent(fixture.transform);
                var despawn = expired.AddComponent<MushroomMonster>();
                typeof(MushroomMonster).GetField("rewards", Flags).SetValue(despawn, species); expired.SetActive(true);
                despawn.enabled = false; despawn.BeginDespawn();
                typeof(MushroomMonster).GetMethod("OnDeath", Flags).Invoke(despawn, null);
                Check(chest.Experience == 5 && quests.GetProgress("daily_mushroom") == 1, "Despawn granted credit");
                chest.Health.DealDamage(100000, CombatDamageType.True);
                Check(!chest.CanRepair, "Fixture unexpectedly has repair money");
                PressKey(true); stage = 1;
            }
            else if (stage == 1 && feedback.RepairProgress > .1f) { PressKey(false); stage = 2; }
            else if (stage == 2 && feedback.RepairProgress == 0) { PressKey(true); stage = 3; }
            else if (stage == 3 && feedback.RepairProgress > .1f)
            { playerHealth.DealDamage(1, CombatDamageType.True); stage = 4; }
            else if (stage == 4)
            { Check(feedback.RepairProgress == 0, "Player damage did not cancel repair"); PressKey(false); stage = 5; }
            else if (stage == 5) { PressKey(true); stage = 6; }
            else if (stage == 6 && chest.IsAlive)
            {
                Check(Mathf.Abs(chest.Health.Health / chest.Health.MaxHealth - .25f) < .001f, "Repair HP incorrect");
                Check(chest.Experience == 5, "Repair lost XP"); PressKey(false);
                playerHealth.transform.position = chest.transform.position + Vector3.right * 10;
                chest.Health.DealDamage(1, CombatDamageType.True);
                stage = 7;
            }
            else if (stage == 7)
            {
                var warning = chest.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "Chest warning");
                Check(warning.gameObject.activeSelf, "Remote damage warning missing");
                Check(chest.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "Hold E repair").gameObject.activeSelf == false,
                    "Repair prompt visible while far away/alive");
                ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Temp/ChestDefenseThreat.png"));
                SessionState.SetString(Key + ".result", "PASS tower kill XP, duplicate/despawn guards, hold/release, damage interruption, free 25% repair, distant warning");
                finished = EditorApplication.timeSinceStartup; stage = 8;
            }
            else if (stage == 8 && EditorApplication.timeSinceStartup - finished > 6)
            {
                var warning = chest.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "Chest warning");
                Check(!warning.gameObject.activeSelf, "Warning failed to expire");
                Finish(SessionState.GetString(Key + ".result", "") + ", warning expiry");
            }
        }
        catch (Exception e) { Finish("FAIL stage " + stage + ": " + e.GetBaseException().Message); }
    }

    private static void Finish(string result)
    {
        ready = false; SessionState.SetString(Key + ".result", result); Debug.Log(result);
        if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
        EditorApplication.isPlaying = false;
    }
}
#endif
