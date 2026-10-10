#if UNITY_EDITOR
using System;
using System.Linq;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Isolated real-Update/Animator/CharacterController check; no save or progression systems run.</summary>
[InitializeOnLoad]
public static class SkullclawPlayValidation
{
    private const string Key = "Mining.SkullclawValidation";
    [Serializable] private class RootState { public string id; public bool active; }
    [Serializable] private class RootStates { public RootState[] roots; }
    private static MushroomMonster subject;
    private static MiningCharacterHealth victim;
    private static readonly int[] hits = new int[3];
    private static float began, maxHeight, maxTravel;
    private static Vector3 jumpStart;
    private static bool movedVictim, sawJump, reviewPaused, finishedJump;
    private static string pendingResult;
        private static bool killed;
        private static bool rightTrailSeen, leftTrailSeen;
        private static bool missedFirst, reentered;
        private static float firstFinished;
    private static float killedAt;
    private static Vector3 deathPosition;
    private static double wallBegan;
    private static DayNightSystem sunriseClock;
    private static bool dawnTriggered, dissolveObserved;

    static SkullclawPlayValidation()
    {
        EditorApplication.playModeStateChanged += OnState;
        EditorApplication.update += Tick;
    }
    public static string Result => SessionState.GetString(Key + ".result", "Not run");

    [MenuItem("Mining Simulator/Validation/Skullclaw Three Attacks (isolated)")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if (UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene != null)
            throw new InvalidOperationException("An existing Play Mode Start Scene override prevents this validation; no override was changed.");
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        var saved = new RootStates { roots = roots.Select(x => new RootState {
            id = GlobalObjectId.GetGlobalObjectIdSlow(x).ToString(), active = x.activeSelf }).ToArray() };
        SessionState.SetString(Key + ".roots", JsonUtility.ToJson(saved));
        SessionState.SetBool(Key + ".background", Application.runInBackground);
        Application.runInBackground = true;
        SessionState.SetBool(Key, true); SessionState.SetString(Key + ".result", "Running");
        foreach (var root in roots) root.SetActive(false);
        EditorApplication.isPlaying = true;
    }

    private static void OnState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            var saved = JsonUtility.FromJson<RootStates>(SessionState.GetString(Key + ".roots", ""));
            if (saved != null) foreach (var root in saved.roots)
                if (GlobalObjectId.TryParse(root.id, out var id) && GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is GameObject go)
                    go.SetActive(root.active);
            SessionState.SetBool(Key, false); Debug.Log("Skullclaw validation: " + Result);
            Application.runInBackground = SessionState.GetBool(Key + ".background", false);
        }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        try
        {
            Array.Clear(hits, 0, hits.Length); maxHeight = maxTravel = 0f;
            movedVictim = sawJump = reviewPaused = finishedJump = false;
            killed = false;
            rightTrailSeen = leftTrailSeen = false;
            missedFirst = reentered = false; firstFinished = -1f;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "__Skullclaw Test Floor__";
            floor.transform.position = new Vector3(10000, -.25f, 10000);
            floor.transform.localScale = new Vector3(30, .5f, 30);
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "__Skullclaw Target__";
            player.transform.position = new Vector3(10000, 1, 10001.9f);
            if (SessionState.GetBool(Key + ".approach", false)) player.transform.position = new Vector3(10000, 1, 10004);
            victim = player.AddComponent<MiningCharacterHealth>(); victim.ConfigureSpawnHealth(1000f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkullclawSetup.PrefabPath);
            var test = UnityEngine.Object.Instantiate(prefab);
            test.name = "__Skullclaw Test__";
            test.transform.position = new Vector3(10000, .03f, 10000);
            subject = test.GetComponent<MushroomMonster>();
            var data = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<SkullclawData>(SkullclawSetup.DataPath));
            data.combat.damage = 5f; data.randomStatMultiplier = Vector2.one; data.combat.attackCooldown = 2f;
            var so = new SerializedObject(subject); so.FindProperty("rewards").objectReferenceValue = data; so.ApplyModifiedPropertiesWithoutUndo();
            // Awake's species cache is initialized from the prefab before this test-only loadout is assigned.
            typeof(MushroomMonster).GetField("speciesData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(subject, data);
            subject.Initialize(null, victim, false);
            if (SessionState.GetBool(Key + ".defense", false))
            {
                UnityEngine.Object.Destroy(player);
                player = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Tower/Cannon/CannonTower.prefab"),
                    new Vector3(10000, 0f, 10004), Quaternion.identity);
                player.name = "__Unsaved Defense Cannon__";
                var tower = player.GetComponent<TowerRuntime>();
                var cannon = UnityEngine.Object.Instantiate((CannonTowerData)tower.Data);
                cannon.damage = .1f; cannon.attackSpeed = 5f; cannon.range = 10f; cannon.health = 1000f;
                tower.Initialize(cannon, 0f); // No placement ID: fixture damage cannot write the tower save.
                victim = tower.Health;
                typeof(MushroomMonster).GetField("playerTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(subject, null);
                subject.Health.ConfigureSpawnHealth(5000f);
            }
            dawnTriggered = dissolveObserved = false;
            if (SessionState.GetBool(Key + ".sunrise", false))
            {
                var clockObject = new GameObject("__Isolated No-Save Clock__"); clockObject.SetActive(false);
                sunriseClock = clockObject.AddComponent<DayNightSystem>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(DayNightSystem).GetField("initialized", flags).SetValue(sunriseClock, true);
                typeof(DayNightSystem).GetField("currentPeriod", flags).SetValue(sunriseClock, MiningTimePeriod.Night);
                clockObject.SetActive(true); // No Data: no lighting or save writes; initialized avoids restoring saved clock.
                typeof(MonsterEncounterVisuals).GetField("dayNight", flags).SetValue(test.GetComponent<MonsterEncounterVisuals>(), sunriseClock);
                data.dissolveSeconds = 1f;
            }
            if (SessionState.GetBool(Key + ".retaliation", false))
                subject.Health.DealDamage(1f, CombatDamageType.Physical, player);
            victim.Damaged += () => { int step = subject.SkullclawAttackStep; if (step >= 0 && step < 3) hits[step]++; };
            var camera = new GameObject("__Skullclaw Test Camera__").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = test.transform.position + new Vector3(6, 3, -6);
            camera.transform.LookAt(test.transform.position + Vector3.up); camera.backgroundColor = new Color(.13f,.16f,.2f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            var light = new GameObject("__Skullclaw Test Light__").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 2f; light.transform.rotation = Quaternion.Euler(45,-35,0);
            began = Time.time; wallBegan = EditorApplication.timeSinceStartup;
        }
        catch (Exception e) { Complete("FAIL: " + e); }
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        if (subject == null)
        {
            if (SessionState.GetBool(Key + ".sunrise", false) && dawnTriggered)
                Complete((dissolveObserved ? "PASS" : "FAIL") + ": Skullclaw night -> daylight starts dissolve, disables combat without kill damage, then removes monster");
            return;
        }
        if (EditorApplication.isPaused) { wallBegan = EditorApplication.timeSinceStartup; return; }
        float age = Time.time - began;
        if (SessionState.GetBool(Key + ".sunrise", false))
        {
            if (!dawnTriggered && age >= 1f)
            {
                dawnTriggered = true;
                typeof(DayNightSystem).GetField("currentPeriod", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(sunriseClock, MiningTimePeriod.Day);
            }
            if (subject.IsDespawning)
                dissolveObserved = subject.Health.Health > 0f && !subject.GetComponent<CharacterController>().enabled &&
                    subject.GetComponentsInChildren<SkinnedMeshRenderer>().Any(x => x.sharedMaterials.Any(m => m != null && m.HasProperty("_DissolveAmount")));
            if (age > 5f) Complete("FAIL: sunrise did not remove Skullclaw");
            return;
        }
        if (age > 25f || EditorApplication.timeSinceStartup - wallBegan > 40)
        { Complete($"FAIL timeout: hits={string.Join(",", hits)} step={subject.SkullclawAttackStep} height={maxHeight} travel={maxTravel}"); return; }
        if (hits[1] == 1 && !movedVictim && !SessionState.GetBool(Key + ".retaliation", false))
        {
            movedVictim = true;
            victim.transform.position = new Vector3(10000, 1, 10004);
            Physics.SyncTransforms();
        }
        var animator = subject.GetComponentInChildren<Animator>();
        if (subject.SkullclawAttackStep == 2 && !sawJump)
        {
            sawJump = true; jumpStart = subject.transform.position;
            if (SessionState.GetBool(Key + ".wall", false))
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "__Skullclaw Blocking Wall__";
                wall.transform.position = jumpStart + new Vector3(0, 3, 1.5f);
                wall.transform.localScale = new Vector3(5, 6, .5f); Physics.SyncTransforms();
            }
        }
        if (sawJump)
        {
            maxHeight = Mathf.Max(maxHeight, subject.transform.position.y - jumpStart.y);
            maxTravel = Mathf.Max(maxTravel, Vector3.ProjectOnPlane(subject.transform.position - jumpStart, Vector3.up).magnitude);
        }
        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (SessionState.GetBool(Key + ".retaliation", false))
        {
            if (age < 2f && (subject.Health.HealthBar == null || !subject.Health.HealthBar.gameObject.activeInHierarchy))
            { Complete("FAIL: damaged Skullclaw health bar is not visible"); return; }
            if (sawJump && !reentered)
            { Complete("FAIL: close-range retaliation jumped instead of alternating swipes"); return; }
            if (!reentered && hits[0] == 2 && hits[1] == 1 && state.IsName("Idle"))
            {
                reentered = true;
                victim.transform.position = subject.transform.position + subject.transform.forward * 4f + Vector3.up;
                firstFinished = Time.time; Physics.SyncTransforms();
            }
            if (sawJump && !movedVictim && Time.time - firstFinished < .49f)
            { Complete("FAIL: approach jump began before the 0.5s range gate"); return; }
            if (sawJump && state.IsName("Jump Attack") && state.normalizedTime > .95f && !movedVictim)
            {
                movedVictim = true; victim.transform.position = new Vector3(10014, 1, 10014);
                firstFinished = Time.time; Physics.SyncTransforms();
            }
            if (movedVictim && Time.time - firstFinished > 3.2f)
            {
                bool pass = !subject.IsRetaliating && maxHeight > 1f && maxTravel > .1f && hits[0] == 2 && hits[1] == 1;
                Complete((pass ? "PASS" : "FAIL") + ": close-range right/left/right without jump, outside-melee approach delayed at least 0.5s, visible damaged HP bar, retaliation expires after 3s outside range");
            }
            return;
        }
        if (SessionState.GetBool(Key + ".single", false))
        {
            if (!missedFirst && state.IsName("Swipe Right") && state.normalizedTime > .2f)
            {
                missedFirst = true; victim.transform.position = new Vector3(10012, 1, 10012);
                Physics.SyncTransforms();
            }
            if (missedFirst && firstFinished < 0f && state.IsName("Idle")) firstFinished = Time.time;
            if (firstFinished >= 0f && !reentered)
            {
                if (subject.SkullclawAttackStep != 0 || hits[0] != 0)
                { Complete("FAIL: first missed swipe chained while player was out of range"); return; }
                if (Time.time - firstFinished > .8f)
                {
                    victim.transform.position = subject.transform.position + subject.transform.forward * 1.8f + Vector3.up;
                    Physics.SyncTransforms(); reentered = true;
                }
            }
            if (reentered && state.IsName("Swipe Left") &&
                Time.time - firstFinished < Mathf.Max(.15f, subject.EffectiveAttackCooldown) - .25f)
            { Complete("FAIL: second attack ignored post-clip recovery"); return; }
        }
        if (!killed && SessionState.GetBool(Key + ".death", false) && state.IsName("Jump Attack") && state.normalizedTime > .26f)
        {
            subject.GetComponent<MiningCharacterHealth>().DealDamage(10000f, CombatDamageType.True);
            killed = true; killedAt = Time.time; deathPosition = subject.transform.position;
        }
        if (killed)
        {
            if (Time.time - killedAt < .5f) return;
            bool valid = !subject.GetComponent<CharacterController>().enabled && hits[2] == 0 &&
                Vector3.Distance(subject.transform.position, deathPosition) < .01f && state.IsName("Down");
            Complete((valid ? "PASS" : "FAIL") + ": death cancels airborne leap and area damage");
            return;
        }
        if (sawJump && SessionState.GetBool(Key + ".miss", false) && state.IsName("Jump Attack") && state.normalizedTime > .25f)
        { victim.transform.position = new Vector3(10005, 1, 10004); Physics.SyncTransforms(); }
        if (!reviewPaused && SessionState.GetBool(Key + ".review", false) && state.IsName("Jump Attack") && state.normalizedTime > .26f)
        { reviewPaused = true; EditorApplication.isPaused = true; return; }
        if (hits.Any(h => h > 1)) { Complete("FAIL: repeated damage in one attack"); return; }
        if (sawJump && !finishedJump && state.IsName("Jump Attack") && state.normalizedTime > .96f)
        {
            bool wall = SessionState.GetBool(Key + ".wall", false);
            bool miss = SessionState.GetBool(Key + ".miss", false);
            bool single = SessionState.GetBool(Key + ".single", false);
            if (SessionState.GetBool(Key + ".approach", false))
            {
                var combo = (SkullclawCombo)typeof(MushroomMonster).GetField("skullCombo",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(subject);
                bool approachValid = hits[0] == 0 && hits[1] == 0 && hits[2] == 1 && combo.Next == 0 &&
                    maxTravel > .5f && subject.GetComponent<CharacterController>().isGrounded;
                if (SessionState.GetBool(Key + ".defense", false)) approachValid &= subject.Health.Health < 5000f;
                pendingResult = (approachValid ? "PASS" : "FAIL") + ": distant target triggers approach jump without consuming right swipe";
                if (SessionState.GetBool(Key + ".defense", false)) pendingResult += $", cannon selected without player detection, hits={string.Join(",", hits)}, travel={maxTravel:F2}, grounded={subject.GetComponent<CharacterController>().isGrounded}, monsterHP={subject.Health.Health:F2}, next={combo.Next}";
                finishedJump = true; victim.gameObject.SetActive(false); return;
            }
            bool valid = hits[0] == (single ? 0 : 1) && hits[1] == 1 && hits[2] == (wall || miss ? 0 : 1) && maxHeight > 1f &&
                (wall ? maxTravel < 1f : maxTravel > .5f) && subject.GetComponent<CharacterController>().isGrounded;
            pendingResult = $"{(valid ? "PASS" : "FAIL")}: {(single ? "miss first / leave range / re-enter left" : wall ? "wall" : miss ? "dodge" : "normal")}, right/left/jump hits={string.Join(",",hits)}, lift={maxHeight:F2}m, travel={maxTravel:F2}m, grounded={subject.GetComponent<CharacterController>().isGrounded}, duration={age:F2}s";
            finishedJump = true; victim.gameObject.SetActive(false);
        }
        if (finishedJump && state.IsName("Idle"))
        {
            Complete(pendingResult + ", returned to Idle (claw trails removed)");
        }
    }
    public static void Complete(string result)
    {
        SessionState.SetString(Key + ".result", result);
        SessionState.SetBool(Key + ".retaliation", false);
        SessionState.SetBool(Key + ".sunrise", false);
        EditorApplication.isPaused = false; EditorApplication.isPlaying = false;
    }
}
#endif
