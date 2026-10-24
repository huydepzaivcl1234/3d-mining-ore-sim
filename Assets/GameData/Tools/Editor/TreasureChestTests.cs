#if UNITY_EDITOR
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;
public sealed class TreasureChestTests
{
    [Test] public void DamageAndStrongestBurnPreserveKillerSource()
    {
        var root = new GameObject("Isolated damage attribution"); root.SetActive(false);
        var player = new GameObject("Player source");
        var other = new GameObject("Other source");
        try
        {
            var health = root.AddComponent<MiningCharacterHealth>();
            typeof(MiningCharacterHealth).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(health, null);
            health.ConfigureSpawnHealth(100);
            health.DealDamage(1, CombatDamageType.True, player);
            Assert.That(health.LastDamageSource, Is.EqualTo(player));
            health.ApplyBurn(100, .1f, 1, player);
            health.ApplyBurn(1, .1f, 1, other);
            typeof(MiningCharacterHealth).GetMethod("ApplyBurnDamage", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(health, new object[]{100f});
            Assert.That(health.Health, Is.Zero);
            Assert.That(health.LastDamageSource, Is.EqualTo(player));
            health.Respawn();
            health.DealDamage(1, CombatDamageType.True);
            Assert.That(health.LastDamageSource, Is.Null);
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(player); Object.DestroyImmediate(other); }
    }

    [Test] public void CrowdDoesNotBlockStrikesButWallsDo()
    {
        var root = new GameObject("Isolated attack LOS"); root.SetActive(false);
        var neighbour = new GameObject("Crowd neighbour"); neighbour.SetActive(false);
        var victim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            root.transform.position = new Vector3(10000, 10, 10000);
            var monster = root.AddComponent<MushroomMonster>();
            var motor = root.GetComponent<CharacterController>(); motor.center = Vector3.up;
            typeof(MushroomMonster).GetField("motor", flags).SetValue(monster, motor);
            neighbour.transform.position = root.transform.position + Vector3.forward * 2;
            neighbour.AddComponent<MushroomMonster>();
            neighbour.GetComponent<CharacterController>().center = Vector3.up;
            victim.transform.position = root.transform.position + Vector3.forward * 4 + Vector3.up;
            wall.transform.position = root.transform.position + Vector3.forward * 3 + Vector3.up;
            wall.SetActive(false);
            // Enable only collider objects; behaviour lifecycles remain disabled.
            monster.enabled = false;
            neighbour.GetComponent<MushroomMonster>().enabled = false;
            root.SetActive(true); neighbour.SetActive(true);
            Physics.SyncTransforms();
            var method = typeof(MushroomMonster).GetMethod("HasStrikeLineOfSight", flags);
            Assert.That((bool)method.Invoke(monster, new object[]{victim.transform, victim.transform.position}), Is.True);
            wall.SetActive(true); Physics.SyncTransforms();
            Assert.That((bool)method.Invoke(monster, new object[]{victim.transform, victim.transform.position}), Is.False);
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(neighbour); Object.DestroyImmediate(victim); Object.DestroyImmediate(wall); }
    }

    [Test] public void CentredPunchNeverMovesGameplayCollider()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Base/BaseTreasureChest.prefab");
        var root = Object.Instantiate(prefab); root.SetActive(false);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            var chest = root.GetComponent<TreasureChest>();
            typeof(TreasureChest).GetField("persistProgress", flags).SetValue(chest, false);
            var body = root.GetComponent<BoxCollider>();
            var position = body.transform.position; var scale = body.transform.lossyScale;
            typeof(TreasureChest).GetMethod("BuildVisualPivot", flags).Invoke(chest, null);
            var pivot = (Transform)typeof(TreasureChest).GetField("visualPivot", flags).GetValue(chest);
            Assert.That(pivot, Is.Not.Null);
            pivot.localScale = Vector3.one * 1.15f;
            Assert.That(body.transform.position, Is.EqualTo(position));
            Assert.That(body.transform.lossyScale, Is.EqualTo(scale));
            foreach (var child in pivot.GetComponentsInChildren<Collider>(true)) Assert.That(child.enabled, Is.False);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test] public void RepairUsesWorldSpaceBillboardAndShowsMoneyCost()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Base/BaseTreasureChest.prefab");
        var root = Object.Instantiate(prefab); root.SetActive(false);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            var chest = root.GetComponent<TreasureChest>();
            typeof(TreasureChest).GetField("persistProgress", flags).SetValue(chest, false);
            typeof(TreasureChest).GetMethod("Awake", flags).Invoke(chest, null);
            chest.Health.ConfigureSpawnHealth(500);
            var hud = root.GetComponent<TreasureChestHud>(); hud.Bind(chest);
            var button = (UnityEngine.UI.Button)typeof(TreasureChestHud).GetField("repairButton", flags).GetValue(hud);
            Assert.That(button.GetComponentInParent<Canvas>(true).renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(button.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>(true), Is.Not.Null);
            var panel = (RectTransform)typeof(TreasureChestHud).GetField("panel", flags).GetValue(hud);
            Assert.That(hud.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
            Assert.That(panel.Find("Chest stats/Income"), Is.Not.Null);
            Assert.That(panel.Find("Chest stats/Armor"), Is.Not.Null);
            Assert.That(panel.Find("Chest stats/Magic resistance"), Is.Not.Null);
            Assert.That(panel.Find("HP Background").GetComponent<RectTransform>().sizeDelta.y, Is.GreaterThan(
                panel.Find("XP Background").GetComponent<RectTransform>().sizeDelta.y));
            var cameraRoot = new GameObject("Isolated stats camera");
            try
            {
                var camera = cameraRoot.AddComponent<Camera>();
                typeof(TreasureChestHud).GetField("viewer", flags).SetValue(hud, camera);
                var update = typeof(TreasureChestHud).GetMethod("UpdateHealthPanel", flags);
                update.Invoke(hud, new object[]{chest.Data.panelNearDistance, 1f});
                Assert.That(panel.gameObject.activeSelf, Is.True);
                Assert.That(panel.rotation, Is.EqualTo(camera.transform.rotation));
                update.Invoke(hud, new object[]{chest.Data.panelHideDistance + 1f, 1f});
                Assert.That(panel.gameObject.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(cameraRoot); }
            var cost = (TMPro.TextMeshProUGUI)typeof(TreasureChestHud).GetField("repairCostLabel", flags).GetValue(hud);
            Assert.That(cost.text, Is.EqualTo(chest.RepairCost.ToString("0.##")));
            Assert.That(chest.RepairCost, Is.GreaterThan(0));
            Assert.That(button.gameObject.activeSelf, Is.False);
            Assert.That(((UnityEngine.UI.Image)button.targetGraphic).sprite, Is.EqualTo(chest.Data.repairButtonSprite));
            Assert.That(chest.Data.repairButtonSprite, Is.Not.Null);
            Assert.That(button.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(340,89)));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test] public void HealthPanelHidesWhenFarAndReturnsWhenNear()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Base/BaseTreasureChest.prefab");
        var root = Object.Instantiate(prefab); root.SetActive(false);
        var cameraRoot = new GameObject("Isolated HP proximity camera");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            var chest = root.GetComponent<TreasureChest>();
            typeof(TreasureChest).GetField("persistProgress", flags).SetValue(chest, false);
            typeof(TreasureChest).GetMethod("Awake", flags).Invoke(chest, null);
            var hud = root.GetComponent<TreasureChestHud>(); hud.Bind(chest);
            typeof(TreasureChestHud).GetField("viewer", flags).SetValue(hud, cameraRoot.AddComponent<Camera>());
            var panel = (RectTransform)typeof(TreasureChestHud).GetField("panel", flags).GetValue(hud);
            var group = panel.GetComponent<CanvasGroup>();
            var update = typeof(TreasureChestHud).GetMethod("UpdateHealthPanel", flags);
            // Starting far away must not flash a fully visible health panel.
            update.Invoke(hud, new object[]{20f, 1f});
            Assert.That(group.alpha, Is.Zero);
            Assert.That(panel.gameObject.activeSelf, Is.False);
            update.Invoke(hud, new object[]{2f, 1f});
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(panel.gameObject.activeSelf, Is.True);
            // The old 30 m setting left the panel visible at this distance.
            update.Invoke(hud, new object[]{10f, 1f});
            Assert.That(group.alpha, Is.Zero);
            Assert.That(panel.gameObject.activeSelf, Is.False);
            Assert.That(group.blocksRaycasts, Is.False);
            update.Invoke(hud, new object[]{2f, 1f});
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(panel.gameObject.activeSelf, Is.True);
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(cameraRoot); }
    }

    [Test] public void ChestAudioIsConfiguredAndSpatial()
    {
        var d = UnityEditor.AssetDatabase.LoadAssetAtPath<TreasureChestData>("Assets/GameData/Base/TreasureChestData.asset");
        var root = Object.Instantiate(d.prefab.gameObject); root.SetActive(false);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            var chest = root.GetComponent<TreasureChest>();
            typeof(TreasureChest).GetField("persistProgress",flags).SetValue(chest,false);
            typeof(TreasureChest).GetMethod("ConfigureAudio",flags).Invoke(chest,null);
            var audio = (AudioSource)typeof(TreasureChest).GetField("chestAudio",flags).GetValue(chest);
            Assert.That(audio.spatialBlend, Is.EqualTo(1));
            Assert.That(audio.rolloffMode, Is.EqualTo(AudioRolloffMode.Linear));
            Assert.That(audio.maxDistance, Is.GreaterThan(audio.minDistance));
            Assert.That(audio.playOnAwake, Is.False);
            foreach(var clip in new[]{d.openSfx,d.closeSfx,d.payoutSfx,d.hitSfx,d.breakSfx,d.repairSfx})
                Assert.That(clip,Is.Not.Null);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(49, 49)]
    [TestCase(50, 50)]
    [TestCase(51, 50)]
    [TestCase(int.MaxValue, 50)]
    public void DailyBudgetIsDayNumberCappedAtFifty(int day, int expected)
    {
        var root = new GameObject("Isolated daily budget"); root.SetActive(false);
        try
        {
            var zone = root.AddComponent<MonsterSpawnZone>();
            Assert.That(zone.CalculateDailyMonsterCount(999, day), Is.EqualTo(expected));
            Assert.That(MonsterSpawnZone.MaximumLivingMonsters, Is.EqualTo(50));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test] public void LevelIncreasesHealthGoldAndRequiredExperience()
    {
        var d=ScriptableObject.CreateInstance<TreasureChestData>();
        try {Assert.That(d.MaxHealth(2),Is.GreaterThan(d.MaxHealth(1)));Assert.That(d.Gold(2),Is.GreaterThan(d.Gold(1)));Assert.That(d.RequiredExperience(2),Is.GreaterThan(d.RequiredExperience(1)));}
        finally {Object.DestroyImmediate(d);}
    }

[Test] public void AnimationKeepsAuthoredHingeAndStaticCollider()
    {
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Base/BaseTreasureChest.prefab");
        var root=Object.Instantiate(prefab);root.SetActive(false);
        try
        {
            var chest=root.GetComponent<TreasureChest>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(TreasureChest).GetField("persistProgress",flags).SetValue(chest,false);
            var lid=(Transform)typeof(TreasureChest).GetField("lid",flags).GetValue(chest);
            Vector3 anchor=lid.localPosition,scale=lid.localScale;
            Assert.That(anchor.y,Is.GreaterThan(.1f));
            var collider=root.GetComponent<BoxCollider>();Vector3 center=collider.center,size=collider.size;
            foreach(bool open in new[]{true,false})
                foreach(float t in new[]{0f,.25f,.5f,.75f,1f})
                {
                    typeof(TreasureChest).GetMethod("SampleLid",flags).Invoke(chest,new object[]{open,t});
                    Assert.That(lid.localPosition,Is.EqualTo(anchor));Assert.That(lid.localScale,Is.EqualTo(scale));
                    Assert.That(collider.center,Is.EqualTo(center));Assert.That(collider.size,Is.EqualTo(size));
                }
            Assert.That(Quaternion.Angle(lid.localRotation,Quaternion.identity),Is.LessThan(.01f));
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test] public void GoldPopupUsesCoinIconAndLargerReadableAmount()
    {
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Base/BaseTreasureChest.prefab");
        var root=Object.Instantiate(prefab);root.SetActive(false);
        try
        {
            var chest=root.GetComponent<TreasureChest>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(TreasureChest).GetField("persistProgress",flags).SetValue(chest,false);
            typeof(TreasureChest).GetMethod("Awake",flags).Invoke(chest,null);
            chest.Health.ConfigureSpawnHealth(chest.Data.MaxHealth(1));
            var hud=root.GetComponent<TreasureChestHud>();hud.Bind(chest);
            typeof(TreasureChestHud).GetMethod("ShowMoney",flags).Invoke(hud,new object[]{125f});
            var icon=(UnityEngine.UI.Image)typeof(TreasureChestHud).GetField("moneyIcon",flags).GetValue(hud);
            var text=(TMPro.TextMeshProUGUI)typeof(TreasureChestHud).GetField("moneyText",flags).GetValue(hud);
            Assert.That(icon.sprite,Is.Not.Null);Assert.That(text.text,Is.EqualTo("+125"));
            Assert.That(text.fontSize,Is.GreaterThan(30));Assert.That(icon.rectTransform.sizeDelta.x,Is.EqualTo(chest.Data.moneyPopupIconSize));
            float iconRight=icon.rectTransform.anchoredPosition.x+icon.rectTransform.sizeDelta.x*.5f;
            Assert.That(text.rectTransform.anchoredPosition.x,Is.GreaterThan(iconRight));
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test] public void OnlyKillXPLevelsChestAndPreservesExistingDamage()
    {
        var root=new GameObject("Isolated chest economy test");
        root.SetActive(false);
        var d=ScriptableObject.CreateInstance<TreasureChestData>();
        try
        {
            var wallet=root.AddComponent<PlayerWallet>();
            var chest=root.AddComponent<TreasureChest>();
            var health=root.GetComponent<MiningCharacterHealth>();
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            typeof(TreasureChest).GetField("persistProgress",flags).SetValue(chest,false);
            typeof(TreasureChest).GetField("data",flags).SetValue(chest,d);
            typeof(TreasureChest).GetField("wallet",flags).SetValue(chest,wallet);
            typeof(TreasureChest).GetField("health",flags).SetValue(chest,health);
            root.SetActive(true);
            health.ConfigureSpawnHealth(d.MaxHealth(1));
            health.DealDamage(30,CombatDamageType.True);
            
float before=wallet.CurrentMoney;
            typeof(TreasureChest).GetMethod("Pay",flags).Invoke(chest,new object[]{10});
            Assert.That(chest.Level, Is.EqualTo(1));
            Assert.That(chest.Experience, Is.EqualTo(0));
            // No Start / persistent save is invoked in this isolated EditMode test.
            chest.GrantKillExperience(d.RequiredExperience(1));
            Assert.That(chest.Level,Is.EqualTo(2));
            Assert.That(chest.Experience,Is.EqualTo(0));
            Assert.That(wallet.CurrentMoney-before,Is.EqualTo(10*d.Gold(1)));
            Assert.That(health.MaxHealth-health.Health,Is.EqualTo(30).Within(.01f));
            
Assert.That(health.MaxHealth,Is.EqualTo(d.MaxHealth(2)));
        }
        finally {Object.DestroyImmediate(root);Object.DestroyImmediate(d);}
    }


[Test] public void SpawnRingRejectsOutsideAndSamplesTwelveToSixteenMetres()
    {
        Assert.That(WorldNavigationGrid.Instance, Is.Null, "Run this isolated test outside Play Mode.");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var root = new GameObject("Isolated spawn ring"); root.SetActive(false);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(10000, -.1f, 10000);
        floor.transform.localScale = new Vector3(40, .2f, 40);
        var data = ScriptableObject.CreateInstance<TreasureChestData>();
        TreasureChest chest = null;
        try
        {
            var grid = root.AddComponent<WorldNavigationGrid>();
            typeof(WorldNavigationGrid).GetMethod("Awake", flags).Invoke(grid, null);
            grid.ConfigureArea(new Vector3(10000, 0, 10000), new Vector2(40, 40));
            grid.Configure(1f, .5f, 1.8f, 45f, .5f, ~0, 512, 256);
            Physics.SyncTransforms(); grid.Rebake();
            var child = new GameObject("Test chest"); child.transform.SetParent(root.transform);
            child.transform.position = new Vector3(10000, 0, 10000);
            chest = child.AddComponent<TreasureChest>();
            typeof(TreasureChest).GetField("persistProgress", flags).SetValue(chest, false);
            typeof(TreasureChest).GetField("data", flags).SetValue(chest, data);
            typeof(TreasureChest).GetMethod("Awake", flags).Invoke(chest, null);
            chest.Health.ConfigureSpawnHealth(500);
            root.SetActive(true);
            typeof(TreasureChest).GetMethod("OnEnable", flags).Invoke(chest, null);
            var zone = root.AddComponent<MonsterSpawnZone>();
            var sample = typeof(MonsterSpawnZone).GetMethod("TryGetSpawnCandidate", flags);
            var valid = typeof(MonsterSpawnZone).GetMethod("IsValidSpawnDistance", flags);
            for (int i = 0; i < 128; i++)
            {
                var args = new object[] { Vector3.zero };
                Assert.That((bool)sample.Invoke(zone, args), Is.True);
                var point = (Vector3)args[0];
                Assert.That(Vector3.Distance(point, chest.transform.position), Is.InRange(11.999f, 16.001f));
                Assert.That((bool)valid.Invoke(zone, new object[] { point }), Is.True);
            }
            Assert.That((bool)valid.Invoke(zone, new object[] { chest.transform.position + Vector3.right * 6 }), Is.False);
            Assert.That((bool)valid.Invoke(zone, new object[] { chest.transform.position + Vector3.right * 17 }), Is.False);
        }
        finally
        {
            if (chest != null) typeof(TreasureChest).GetMethod("OnDisable", flags).Invoke(chest, null);
            Object.DestroyImmediate(root); Object.DestroyImmediate(floor); Object.DestroyImmediate(data);
        }
    }


[Test] public void SuppliedArtworkAndFillAmountsAreBound()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Base/BaseTreasureChest.prefab");
        var root = Object.Instantiate(prefab); root.SetActive(false);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            var chest = root.GetComponent<TreasureChest>();
            typeof(TreasureChest).GetField("persistProgress", flags).SetValue(chest, false);
            typeof(TreasureChest).GetMethod("Awake", flags).Invoke(chest, null);
            chest.Health.ConfigureSpawnHealth(500);
            chest.Health.DealDamage(250, CombatDamageType.True);
            typeof(TreasureChest).GetField("<Experience>k__BackingField", flags).SetValue(chest, 5f);
            var hud = root.GetComponent<TreasureChestHud>(); hud.Bind(chest);
            var hp = (UnityEngine.UI.Image)typeof(TreasureChestHud).GetField("hp", flags).GetValue(hud);
            var xp = (UnityEngine.UI.Image)typeof(TreasureChestHud).GetField("xp", flags).GetValue(hud);
            Assert.That(hp.sprite, Is.EqualTo(chest.Data.healthFill).And.Not.Null);
            Assert.That(xp.sprite, Is.EqualTo(chest.Data.experienceFill).And.Not.Null);
            Assert.That(hp.fillAmount, Is.EqualTo(.5f).Within(.001f));
            Assert.That(xp.fillAmount, Is.EqualTo(.5f).Within(.001f));
            Assert.That(hp.sprite.texture.width, Is.GreaterThan(100));
            Assert.That(hp.type, Is.EqualTo(UnityEngine.UI.Image.Type.Sliced));
            Assert.That(hp.rectTransform.sizeDelta.x, Is.EqualTo(243f).Within(.01f));
            Assert.That(hp.rectTransform.anchoredPosition.x, Is.EqualTo(4f));
            Assert.That(hp.sprite.border.x, Is.GreaterThan(0));
            var frame = root.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            Assert.That(System.Array.Exists(frame, image => image.name == "Panel frame" && image.sprite != null), Is.True);
            chest.Health.DealDamage(250, CombatDamageType.True);
            typeof(TreasureChestHud).GetMethod("Refresh", flags).Invoke(hud, null);
            Assert.That(hp.fillAmount, Is.Zero);
            var highlight = (UnityEngine.UI.Image)typeof(TreasureChestHud).GetField("hpHighlight", flags).GetValue(hud);
            Assert.That(highlight.enabled, Is.False);
        }
        finally { Object.DestroyImmediate(root); }
    }


[Test] public void SameDayResetRebuildsExhaustedEncounterSchedule()
    {
        var root = new GameObject("Isolated encounter reset"); root.SetActive(false);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var state = Random.state;
        try
        {
            var clock = root.AddComponent<DayNightSystem>();
            var zone = root.AddComponent<MonsterSpawnZone>();
            typeof(MonsterSpawnZone).GetField("dayNight", flags).SetValue(zone, clock);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<MushroomMonster>("Assets/Prefabs/Monsters/MushroomMonster.prefab");
            Assert.That(prefab, Is.Not.Null);
            typeof(MonsterSpawnZone).GetField("monsters", flags).SetValue(zone,
                new System.Collections.Generic.List<MonsterSpawnEntry> { new MonsterSpawnEntry { prefab = prefab, minimumPlayerLevel = 1 } });
            typeof(MonsterSpawnZone).GetField("scheduledDay", flags).SetValue(zone, clock.DayNumber);
            typeof(MonsterSpawnZone).GetField("nextWave", flags).SetValue(zone, 99);
            int day = clock.DayNumber;
            zone.ResetEncounter();
            Assert.That(zone.ForecastDay, Is.EqualTo(day));
            Assert.That(typeof(MonsterSpawnZone).GetField("nextWave", flags).GetValue(zone), Is.EqualTo(0));
            Assert.That(zone.DailyForecast.Count, Is.GreaterThan(0));
            foreach (var entry in zone.DailyForecast)
            {
                Assert.That(entry.Remaining, Is.GreaterThan(0));
                Assert.That(entry.Skipped, Is.Zero);
            }
        }
        finally { Object.DestroyImmediate(root); Random.state = state; }
    }


[Test] public void ResetChestRestoresLevelExperienceHealthAndPayoutClockWithoutSaving()
    {
        var root = new GameObject("Isolated chest reset"); root.SetActive(false);
        var data = ScriptableObject.CreateInstance<TreasureChestData>();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            var chest = root.AddComponent<TreasureChest>();
            typeof(TreasureChest).GetField("persistProgress", flags).SetValue(chest, false);
            typeof(TreasureChest).GetField("data", flags).SetValue(chest, data);
            typeof(TreasureChest).GetMethod("Awake", flags).Invoke(chest, null);
            typeof(TreasureChest).GetField("<Level>k__BackingField", flags).SetValue(chest, 7);
            typeof(TreasureChest).GetField("<Experience>k__BackingField", flags).SetValue(chest, 12f);
            typeof(TreasureChest).GetField("pendingTicks", flags).SetValue(chest, 3);
            typeof(TreasureChest).GetField("payoutClock", flags).SetValue(chest, 4f);
            chest.Health.ConfigureSpawnHealth(data.MaxHealth(7));
            chest.Health.DealDamage(chest.Health.MaxHealth, CombatDamageType.True);
            chest.ResetProgress();
            Assert.That(chest.Level, Is.EqualTo(1));
            Assert.That(chest.Experience, Is.Zero);
            Assert.That(chest.Health.Health, Is.EqualTo(data.MaxHealth(1)));
            Assert.That(typeof(TreasureChest).GetField("pendingTicks", flags).GetValue(chest), Is.EqualTo(0));
            Assert.That(typeof(TreasureChest).GetField("payoutClock", flags).GetValue(chest), Is.EqualTo(0f));
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(data); }
    }
}
#endif
