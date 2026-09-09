#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Microlight.MicroBar;
using MiningSimulator.Ores;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiningSimulator.Editor
{
    /// <summary>
    /// Creates the starter ore data and prefabs with the same data-driven pattern used
    /// by the Tower Defense project's TowerData and setup menu.
    /// </summary>
    public static class MiningOreSetupMenu
    {
        private const string ModelFolder = "Assets/Ores/Models";
        private const string GameDataPath = "Assets/GameData/MiningGameData.asset";
        private const string NpcDataFolder = "Assets/GameData/NPC";
        private const string NpcDataPath = NpcDataFolder + "/NpcData.asset";
        private const string SpawnDataFolder = "Assets/GameData/Spawning";
        private const string SpawnDataPath = SpawnDataFolder + "/OreSpawnData.asset";
        private const string UpgradeDataFolder = "Assets/GameData/Upgrades";
        private const string UpgradeDataPath = UpgradeDataFolder + "/MiningUpgradeData.asset";
        private const string UiDataFolder = "Assets/GameData/UI";
        private const string UiDataPath = UiDataFolder + "/MiningUiData.asset";
        private const string AudioDataFolder = "Assets/GameData/Audio";
        private const string AudioDataPath = AudioDataFolder + "/MiningAudioData.asset";
        private const string RebirthDataFolder = "Assets/GameData/Rebirth";
        private const string RebirthDataPath = RebirthDataFolder + "/MiningRebirthData.asset";
        private const string DataFolder = "Assets/GameData/Ores";
        private const string PrefabFolder = "Assets/Prefabs/Ores";
        private const string NpcPrefabFolder = "Assets/Prefabs/NPC";
        private const string SystemPrefabFolder = "Assets/Prefabs/Systems";
        private const string UiPrefabFolder = "Assets/Prefabs/UI";
        private const string LuckyBlockRewardIconPath =
            UiPrefabFolder + "/UpgradeLuckyBlockReward.png";
        private const string LuckyBlockDropChanceIconPath =
            UiPrefabFolder + "/UpgradeLuckyBlockDropChance.png";
        private const string NpcExperienceIconPath =
            UiPrefabFolder + "/UpgradeNpcExperience.png";
        private const string NpcPrefabPath = NpcPrefabFolder + "/MiningNpc.prefab";
        private const string RuntimePrefabPath = SystemPrefabFolder + "/MiningRuntime.prefab";
        private const string RewardPopupPrefabPath = UiPrefabFolder + "/OreRewardPopup.prefab";
        private const string HudCanvasName = "Mining HUD Canvas";
        private const string HealthBarPrefabPath =
            "Assets/Microlight/MicroBar/Prefabs/SimpleBars/Sprite_SimpleMicroBarSRP.prefab";
        private const string UiMicroBarPrefabPath =
            "Assets/Microlight/MicroBar/Prefabs/SimpleBars/Image_SimpleMicroBar.prefab";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        private readonly struct OreSpec
        {
            public OreSpec(OreKind kind, string name, string description, int tier,
                int miningPower, int durability, int clickDamage, int sellValue,
                OreRarity rarity, Color mapColor, string modelFile)
            {
                Kind = kind;
                Name = name;
                Description = description;
                Tier = tier;
                MiningPower = miningPower;
                Durability = durability;
                ClickDamage = clickDamage;
                SellValue = sellValue;
                Rarity = rarity;
                MapColor = mapColor;
                ModelFile = modelFile;
            }

            public OreKind Kind { get; }
            public string Name { get; }
            public string Description { get; }
            public int Tier { get; }
            public int MiningPower { get; }
            public int Durability { get; }
            public int ClickDamage { get; }
            public int SellValue { get; }
            public OreRarity Rarity { get; }
            public Color MapColor { get; }
            public string ModelFile { get; }
        }

        private static readonly OreSpec[] StarterOres =
        {
            new(OreKind.Stone, "Stone", "Common stone. The first material a miner can break.",
                1, 1, 10, 1, 1, OreRarity.Common, new Color(0.48f, 0.52f, 0.56f), "stone_tier1.fbx"),
            new(OreKind.Coal, "Coal", "Dark fuel ore unlocked after basic stone mining.",
                2, 3, 20, 2, 4, OreRarity.Uncommon, new Color(0.10f, 0.12f, 0.14f), "coal_tier2.fbx"),
            new(OreKind.Copper, "Copper", "Valuable metallic ore used for stronger upgrades.",
                3, 6, 35, 3, 9, OreRarity.Uncommon, new Color(0.82f, 0.32f, 0.08f), "copper_tier3.fbx"),
            new(OreKind.Iron, "Iron", "Rare iron deposits used for advanced mining equipment.",
                4, 10, 60, 4, 18, OreRarity.Rare, new Color(0.55f, 0.60f, 0.66f), "iron_tier4.fbx"),
            new(OreKind.Gold, "Gold", "Rare gold deposits with a high sell value.",
                5, 15, 90, 5, 35, OreRarity.Rare, new Color(1f, 0.68f, 0.08f), "gold_tier5.fbx"),
            new(OreKind.Diamond, "Diamond", "Epic crystal ore requiring powerful miners.",
                6, 25, 150, 6, 75, OreRarity.Epic, new Color(0.12f, 0.78f, 1f), "diamond_tier6.fbx")
        };

        [MenuItem("Mining Simulator/Setup/Create or Update Starter Ores")]
        public static void CreateOrUpdateStarterOres()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(NpcDataFolder);
            EnsureFolder(SpawnDataFolder);
            EnsureFolder(UpgradeDataFolder);
            EnsureFolder(UiDataFolder);
            EnsureFolder(AudioDataFolder);
            EnsureFolder(RebirthDataFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(NpcPrefabFolder);
            EnsureFolder(SystemPrefabFolder);
            EnsureFolder(UiPrefabFolder);

            var dataAssets = new OreData[StarterOres.Length];

            for (int index = 0; index < StarterOres.Length; index++)
            {
                OreSpec spec = StarterOres[index];
                OreData data = CreateOrUpdateData(spec);
                dataAssets[index] = data;
                GameObject prefab = CreateOrUpdatePrefab(spec, data);
                if (prefab != null && data.Prefab == null)
                {
                    SetObjectReference(data, "prefab", prefab);
                }
            }

            MiningGameData gameData = CreateOrUpdateGameData();
            NpcData npcData = CreateOrUpdateNpcData();
            OreSpawnData spawnData = CreateOrUpdateSpawnData(dataAssets);
            MiningUpgradeData upgradeData = CreateOrUpdateUpgradeData();
            MiningUiData uiData = CreateOrUpdateUiData();
            MiningAudioData audioData = CreateOrUpdateAudioData();
            MiningRebirthData rebirthData = CreateOrUpdateRebirthData();
            OreRewardPopup rewardPopupPrefab = CreateOrUpdateRewardPopupPrefab(uiData);
            MiningNpc npcPrefab = CreateOrUpdateNpcPrefab(npcData);
            CreateOrUpdateRuntimePrefab(npcPrefab, gameData, npcData, spawnData, upgradeData, uiData,
                audioData, rebirthData, rewardPopupPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshLuckyBlockUpgradeUiInActiveScene(logMissingSceneObjects: false);
            RefreshNpcProgressionUiInActiveScene(logMissingSceneObjects: false);
            Debug.Log($"Mining ore setup complete: {StarterOres.Length} independent OreData assets and prefabs.");
        }

        [MenuItem("Mining Simulator/Setup/Refresh Lucky Block Upgrade UI")]
        public static void RefreshLuckyBlockUpgradeUi()
        {
            RefreshLuckyBlockUpgradeUiInActiveScene(logMissingSceneObjects: true);
        }

        [MenuItem("Mining Simulator/Setup/Refresh NPC Progression UI")]
        public static void RefreshNpcProgressionUi()
        {
            RefreshNpcProgressionUiInActiveScene(logMissingSceneObjects: true);
        }

        // Entry point used by Unity batch mode and CI.
        public static void CreateOrUpdateStarterOresBatch()
        {
            CreateOrUpdateStarterOres();
        }

        [MenuItem("Mining Simulator/Setup/Setup Complete Mining Gameplay")]
        public static void AddMiningRuntimeToActiveScene()
        {
            CreateOrUpdateStarterOres();
            Scene scene = SceneManager.GetActiveScene();
            bool changed = AddRuntimeToSceneIfMissing(scene, registerUndo: true);
            changed |= EnsureEventSystemInScene(scene, registerUndo: true);
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        [MenuItem("Mining Simulator/Setup/Organize Scene GameManager")]
        public static void ConvertRuntimeToSceneGameManager()
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<OreSpawner>(true) == null)
                {
                    continue;
                }

                bool changed = ConvertRuntimeRootToSceneGameManager(root, registerUndo: true);
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }

                Selection.activeGameObject = root;
                Debug.Log("Scene GameManager is independent and its systems are organized into clear child objects.", root);
                return;
            }

            Debug.LogWarning("No mining runtime systems were found in the active scene.");
        }

        public static void SetupSampleSceneBatch()
        {
            CreateOrUpdateStarterOres();
            Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            bool changed = AddRuntimeToSceneIfMissing(scene, registerUndo: false);
            changed |= EnsureEventSystemInScene(scene, registerUndo: false);
            if (changed)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static OreData CreateOrUpdateData(OreSpec spec)
        {
            string path = $"{DataFolder}/{spec.Name}.asset";
            OreData existing = AssetDatabase.LoadAssetAtPath<OreData>(path);
            if (existing != null)
            {
                // Migration only: old assets predate clickDamage. Do not reset designer values.
                var existingSerialized = new SerializedObject(existing);
                SerializedProperty clickDamage = existingSerialized.FindProperty("clickDamage");
                if (clickDamage != null && clickDamage.intValue <= 0)
                {
                    clickDamage.intValue = spec.ClickDamage;
                }
                SerializedProperty maximumMiningNpcs = existingSerialized.FindProperty("maximumMiningNpcs");
                if (maximumMiningNpcs != null && maximumMiningNpcs.intValue <= 0)
                {
                    maximumMiningNpcs.intValue = 3;
                }
                SerializedProperty npcStandDistance = existingSerialized.FindProperty("npcStandDistance");
                if (npcStandDistance != null && npcStandDistance.floatValue <= 0f)
                {
                    npcStandDistance.floatValue = 1.4f;
                }
                SerializedProperty legacyRareOre = existingSerialized.FindProperty("rareOre");
                SerializedProperty rarity = existingSerialized.FindProperty("rarity");
                if (legacyRareOre != null && legacyRareOre.boolValue && rarity != null)
                {
                    rarity.enumValueIndex = (int)spec.Rarity;
                    legacyRareOre.boolValue = false;
                }
                existingSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(existing);
                return existing;
            }

            OreData data = ScriptableObject.CreateInstance<OreData>();
            AssetDatabase.CreateAsset(data, path);
            var serialized = new SerializedObject(data);
            serialized.FindProperty("kind").enumValueIndex = (int)spec.Kind;
            serialized.FindProperty("displayName").stringValue = spec.Name;
            serialized.FindProperty("description").stringValue = spec.Description;
            serialized.FindProperty("tier").intValue = spec.Tier;
            serialized.FindProperty("miningPowerRequired").intValue = spec.MiningPower;
            serialized.FindProperty("durability").intValue = spec.Durability;
            serialized.FindProperty("clickDamage").intValue = spec.ClickDamage;
            serialized.FindProperty("baseSellValue").intValue = spec.SellValue;
            serialized.FindProperty("maximumMiningNpcs").intValue = 3;
            serialized.FindProperty("npcStandDistance").floatValue = 1.4f;
            serialized.FindProperty("rareOre").boolValue = false;
            serialized.FindProperty("rarity").enumValueIndex = (int)spec.Rarity;
            serialized.FindProperty("mapColor").colorValue = spec.MapColor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject CreateOrUpdatePrefab(OreSpec spec, OreData data)
        {
            string prefabPath = $"{PrefabFolder}/{spec.Name}.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    Ore existingOre = contents.GetComponent<Ore>() ?? contents.AddComponent<Ore>();
                    if (existingOre.Data == null)
                    {
                        existingOre.SetData(data);
                    }
                    EnsureCollider(contents);
                    AddHealthBarIfMissing(contents, existingOre);
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
                return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            string modelPath = $"{ModelFolder}/{spec.ModelFile}";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError($"Cannot create {spec.Name} prefab. Model was not imported at {modelPath}.");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"Cannot instantiate model for {spec.Name}.");
                return null;
            }

            try
            {
                instance.name = spec.Name;
                Ore ore = instance.GetComponent<Ore>() ?? instance.AddComponent<Ore>();
                ore.SetData(data);
                EnsureCollider(instance);
                AddHealthBarIfMissing(instance, ore);

                return PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static MiningGameData CreateOrUpdateGameData()
        {
            MiningGameData gameData = AssetDatabase.LoadAssetAtPath<MiningGameData>(GameDataPath);
            if (gameData == null)
            {
                gameData = ScriptableObject.CreateInstance<MiningGameData>();
                AssetDatabase.CreateAsset(gameData, GameDataPath);
            }

            return gameData;
        }

        private static NpcData CreateOrUpdateNpcData()
        {
            NpcData npcData = AssetDatabase.LoadAssetAtPath<NpcData>(NpcDataPath);
            if (npcData == null)
            {
                npcData = ScriptableObject.CreateInstance<NpcData>();
                AssetDatabase.CreateAsset(npcData, NpcDataPath);
            }
            return npcData;
        }

        private static OreSpawnData CreateOrUpdateSpawnData(OreData[] dataAssets)
        {
            OreSpawnData spawnData = AssetDatabase.LoadAssetAtPath<OreSpawnData>(SpawnDataPath);
            if (spawnData == null)
            {
                spawnData = ScriptableObject.CreateInstance<OreSpawnData>();
                AssetDatabase.CreateAsset(spawnData, SpawnDataPath);
            }

            var serialized = new SerializedObject(spawnData);
            SerializedProperty table = serialized.FindProperty("oreSpawnTable");
            if (table != null && table.arraySize == 0)
            {
                table.arraySize = dataAssets.Length;
                float[] defaultWeights = { 60f, 30f, 10f, 0f, 0f, 0f };
                for (int index = 0; index < dataAssets.Length; index++)
                {
                    SerializedProperty entry = table.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("ore").objectReferenceValue = dataAssets[index];
                    entry.FindPropertyRelative("spawnWeight").floatValue =
                        index < defaultWeights.Length ? defaultWeights[index] : 1f;
                }
            }
            else if (table != null)
            {
                foreach (OreData dataAsset in dataAssets)
                {
                    bool exists = false;
                    for (int index = 0; index < table.arraySize; index++)
                    {
                        SerializedProperty entry = table.GetArrayElementAtIndex(index);
                        if (entry.FindPropertyRelative("ore").objectReferenceValue == dataAsset)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        int newIndex = table.arraySize;
                        table.InsertArrayElementAtIndex(newIndex);
                        SerializedProperty newEntry = table.GetArrayElementAtIndex(newIndex);
                        newEntry.FindPropertyRelative("ore").objectReferenceValue = dataAsset;
                        newEntry.FindPropertyRelative("spawnWeight").floatValue = 0f;
                    }
                }
            }

            SerializedProperty rarityRules = serialized.FindProperty("rarityRules");
            if (rarityRules != null && rarityRules.arraySize == 0)
            {
                rarityRules.arraySize = 5;
                float[] unlockWeights = { 0f, 0f, 20f, 4f, 1f };
                for (int index = 0; index < rarityRules.arraySize; index++)
                {
                    SerializedProperty rule = rarityRules.GetArrayElementAtIndex(index);
                    rule.FindPropertyRelative("rarity").enumValueIndex = index;
                    rule.FindPropertyRelative("baseWeightMultiplier").floatValue = 1f;
                    rule.FindPropertyRelative("affectedByRareUpgrade").boolValue =
                        index >= (int)OreRarity.Rare;
                    rule.FindPropertyRelative("zeroWeightUnlockAtOneHundredPercentBonus").floatValue =
                        unlockWeights[index];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawnData);
            return spawnData;
        }

        private static MiningUpgradeData CreateOrUpdateUpgradeData()
        {
            MiningUpgradeData upgradeData =
                AssetDatabase.LoadAssetAtPath<MiningUpgradeData>(UpgradeDataPath);
            if (upgradeData == null)
            {
                upgradeData = ScriptableObject.CreateInstance<MiningUpgradeData>();
                AssetDatabase.CreateAsset(upgradeData, UpgradeDataPath);
            }
            return upgradeData;
        }

        private static MiningUiData CreateOrUpdateUiData()
        {
            MiningUiData uiData = AssetDatabase.LoadAssetAtPath<MiningUiData>(UiDataPath);
            if (uiData == null)
            {
                uiData = ScriptableObject.CreateInstance<MiningUiData>();
                AssetDatabase.CreateAsset(uiData, UiDataPath);
            }
            return uiData;
        }

        private static void RefreshLuckyBlockUpgradeUiInActiveScene(
            bool logMissingSceneObjects)
        {
            MiningUpgradePanel panel = UnityEngine.Object.FindFirstObjectByType<MiningUpgradePanel>(
                FindObjectsInactive.Include);
            MiningUpgradeSystem upgradeSystem =
                UnityEngine.Object.FindFirstObjectByType<MiningUpgradeSystem>(
                    FindObjectsInactive.Include);
            PlayerWallet wallet = UnityEngine.Object.FindFirstObjectByType<PlayerWallet>(
                FindObjectsInactive.Include);
            MiningUpgradeData upgradeData =
                AssetDatabase.LoadAssetAtPath<MiningUpgradeData>(UpgradeDataPath);
            MiningUiData uiData = AssetDatabase.LoadAssetAtPath<MiningUiData>(UiDataPath);

            if (panel == null || upgradeSystem == null || wallet == null ||
                upgradeData == null || uiData == null)
            {
                if (logMissingSceneObjects)
                {
                    Debug.LogError("Cannot refresh Lucky Block upgrade UI. The active Scene must " +
                                   "contain PlayerWallet, MiningUpgradeSystem and " +
                                   "MiningUpgradePanel.");
                }
                return;
            }

            AssignLuckyBlockUpgradeIconsIfMissing(uiData);
            GameObject sceneRoot = panel.transform.root.gameObject;
            Undo.RecordObject(panel, "Refresh Lucky Block Upgrade UI");
            ConfigureUpgradePanel(sceneRoot, panel, upgradeSystem, wallet, upgradeData, uiData);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeTransform = sceneRoot.transform.Find(HudCanvasName + "/Upgrade Panel");
            Debug.Log("Lucky Block upgrade cards and icons were refreshed in the active Scene.",
                panel);
        }

        private static void AssignLuckyBlockUpgradeIconsIfMissing(MiningUiData uiData)
        {
            var serialized = new SerializedObject(uiData);
            SetReferenceIfMissing(serialized.FindProperty("luckyBlockRewardIconSprite"),
                AssetDatabase.LoadAssetAtPath<Sprite>(LuckyBlockRewardIconPath));
            SetReferenceIfMissing(serialized.FindProperty("luckyBlockDropChanceIconSprite"),
                AssetDatabase.LoadAssetAtPath<Sprite>(LuckyBlockDropChanceIconPath));
            if (serialized.ApplyModifiedPropertiesWithoutUndo())
            {
                EditorUtility.SetDirty(uiData);
            }
        }

        private static void RefreshNpcProgressionUiInActiveScene(bool logMissingSceneObjects)
        {
            NpcShop shop = UnityEngine.Object.FindFirstObjectByType<NpcShop>(
                FindObjectsInactive.Include);
            OreSpawner oreSpawner = UnityEngine.Object.FindFirstObjectByType<OreSpawner>(
                FindObjectsInactive.Include);
            MiningUpgradeSystem upgradeSystem =
                UnityEngine.Object.FindFirstObjectByType<MiningUpgradeSystem>(
                    FindObjectsInactive.Include);
            MiningUpgradePanel upgradePanel =
                UnityEngine.Object.FindFirstObjectByType<MiningUpgradePanel>(
                    FindObjectsInactive.Include);
            MiningRebirthSystem rebirthSystem =
                UnityEngine.Object.FindFirstObjectByType<MiningRebirthSystem>(
                    FindObjectsInactive.Include);
            PlayerWallet wallet = UnityEngine.Object.FindFirstObjectByType<PlayerWallet>(
                FindObjectsInactive.Include);
            NpcData npcData = AssetDatabase.LoadAssetAtPath<NpcData>(NpcDataPath);
            MiningUpgradeData upgradeData =
                AssetDatabase.LoadAssetAtPath<MiningUpgradeData>(UpgradeDataPath);
            MiningUiData uiData = AssetDatabase.LoadAssetAtPath<MiningUiData>(UiDataPath);

            if (shop == null || oreSpawner == null || upgradeSystem == null ||
                upgradePanel == null || wallet == null || npcData == null ||
                upgradeData == null || uiData == null)
            {
                if (logMissingSceneObjects)
                {
                    Debug.LogError("Cannot refresh NPC progression UI. The active Scene must " +
                                   "contain NpcShop, OreSpawner, PlayerWallet, " +
                                   "MiningUpgradeSystem and MiningUpgradePanel.");
                }
                return;
            }

            AssignNpcExperienceIconIfMissing(uiData);
            GameObject sceneRoot = shop.transform.root.gameObject;
            NpcProgressionSystem progression = shop.GetComponent<NpcProgressionSystem>();
            if (progression == null)
            {
                progression = Undo.AddComponent<NpcProgressionSystem>(shop.gameObject);
            }

            var progressionSerialized = new SerializedObject(progression);
            SetReferenceIfMissing(progressionSerialized.FindProperty("oreSpawner"), oreSpawner);
            SetReferenceIfMissing(progressionSerialized.FindProperty("upgradeSystem"), upgradeSystem);
            SetReferenceIfMissing(progressionSerialized.FindProperty("npcData"), npcData);
            progressionSerialized.ApplyModifiedPropertiesWithoutUndo();

            var shopSerialized = new SerializedObject(shop);
            SetReferenceIfMissing(shopSerialized.FindProperty("progressionSystem"), progression);
            shopSerialized.ApplyModifiedPropertiesWithoutUndo();

            if (rebirthSystem != null)
            {
                var rebirthSerialized = new SerializedObject(rebirthSystem);
                SetReferenceIfMissing(rebirthSerialized.FindProperty("npcProgressionSystem"),
                    progression);
                rebirthSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            ConfigureNpcProgressionHud(sceneRoot, progression, shop, npcData, uiData);
            ConfigureUpgradePanel(sceneRoot, upgradePanel, upgradeSystem, wallet, upgradeData, uiData);
            EditorUtility.SetDirty(progression);
            EditorUtility.SetDirty(shop);
            EditorUtility.SetDirty(upgradePanel);
            EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeTransform = sceneRoot.transform.Find(HudCanvasName + "/NPC Progress HUD");
            Debug.Log("NPC level, power, smooth XP HUD and XP upgrade were refreshed in the active Scene.",
                progression);
        }

        private static void AssignNpcExperienceIconIfMissing(MiningUiData uiData)
        {
            var serialized = new SerializedObject(uiData);
            SetReferenceIfMissing(serialized.FindProperty("npcExperienceIconSprite"),
                AssetDatabase.LoadAssetAtPath<Sprite>(NpcExperienceIconPath));
            if (serialized.ApplyModifiedPropertiesWithoutUndo())
            {
                EditorUtility.SetDirty(uiData);
            }
        }

        private static MiningAudioData CreateOrUpdateAudioData()
        {
            MiningAudioData audioData = AssetDatabase.LoadAssetAtPath<MiningAudioData>(AudioDataPath);
            if (audioData == null)
            {
                audioData = ScriptableObject.CreateInstance<MiningAudioData>();
                AssetDatabase.CreateAsset(audioData, AudioDataPath);
            }
            return audioData;
        }

        private static MiningRebirthData CreateOrUpdateRebirthData()
        {
            MiningRebirthData rebirthData =
                AssetDatabase.LoadAssetAtPath<MiningRebirthData>(RebirthDataPath);
            if (rebirthData == null)
            {
                rebirthData = ScriptableObject.CreateInstance<MiningRebirthData>();
                AssetDatabase.CreateAsset(rebirthData, RebirthDataPath);
            }
            return rebirthData;
        }

        private static OreRewardPopup CreateOrUpdateRewardPopupPrefab(MiningUiData uiData)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RewardPopupPrefabPath);
            bool isNew = existing == null;
            GameObject popupObject = isNew
                ? new GameObject("Ore Reward Popup", typeof(TextMeshPro), typeof(OreRewardPopup))
                : PrefabUtility.LoadPrefabContents(RewardPopupPrefabPath);

            try
            {
                TextMeshPro label = popupObject.GetComponent<TextMeshPro>() ??
                                    popupObject.AddComponent<TextMeshPro>();
                OreRewardPopup popup = popupObject.GetComponent<OreRewardPopup>() ??
                                        popupObject.AddComponent<OreRewardPopup>();
                label.text = string.Format(uiData.RewardPopupFormat,
                    uiData.RewardPopupPreviewAmount);
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = uiData.RewardPopupFontSize;
                label.color = uiData.RewardPopupColor;
                label.outlineColor = uiData.RewardPopupOutlineColor;
                label.outlineWidth = uiData.RewardPopupOutlineWidth;
                popupObject.transform.localScale = Vector3.one * uiData.RewardPopupWorldScale;
                Renderer popupRenderer = label.GetComponent<Renderer>();
                if (popupRenderer != null)
                {
                    popupRenderer.sortingOrder = uiData.CanvasSortingOrder;
                }

                var serialized = new SerializedObject(popup);
                serialized.FindProperty("label").objectReferenceValue = label;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(popupObject, RewardPopupPrefabPath);
            }
            finally
            {
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(popupObject);
                }
                else
                {
                    PrefabUtility.UnloadPrefabContents(popupObject);
                }
            }

            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(RewardPopupPrefabPath);
            return saved != null ? saved.GetComponent<OreRewardPopup>() : null;
        }

        private static MiningNpc CreateOrUpdateNpcPrefab(NpcData npcData)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath);
            if (existing != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(NpcPrefabPath);
                try
                {
                    MiningNpc miningNpc = contents.GetComponent<MiningNpc>() ?? contents.AddComponent<MiningNpc>();
                    ConfigureNpcPrefab(contents, miningNpc, npcData);
                    PrefabUtility.SaveAsPrefabAsset(contents, NpcPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
                GameObject updated = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath);
                return updated != null ? updated.GetComponent<MiningNpc>() : null;
            }

            GameObject npcObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                npcObject.name = "Mining NPC";
                npcObject.transform.localScale = npcData.BodyScale;
                MiningNpc miningNpc = npcObject.AddComponent<MiningNpc>();
                ConfigureNpcPrefab(npcObject, miningNpc, npcData);

                GameObject helmet = GameObject.CreatePrimitive(PrimitiveType.Cube);
                helmet.name = "Miner Helmet";
                helmet.transform.SetParent(npcObject.transform, false);
                helmet.transform.localPosition = npcData.HelmetLocalPosition;
                helmet.transform.localScale = npcData.HelmetLocalScale;
                UnityEngine.Object.DestroyImmediate(helmet.GetComponent<Collider>());

                GameObject pickaxeHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickaxeHandle.name = "Pickaxe Handle";
                pickaxeHandle.transform.SetParent(npcObject.transform, false);
                pickaxeHandle.transform.localPosition = npcData.ToolLocalPosition;
                pickaxeHandle.transform.localRotation = Quaternion.Euler(npcData.ToolLocalEulerAngles);
                pickaxeHandle.transform.localScale = npcData.ToolLocalScale;
                UnityEngine.Object.DestroyImmediate(pickaxeHandle.GetComponent<Collider>());

                GameObject pickaxeHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickaxeHead.name = "Pickaxe Head";
                pickaxeHead.transform.SetParent(pickaxeHandle.transform, false);
                pickaxeHead.transform.localPosition = npcData.ToolHeadLocalPosition;
                pickaxeHead.transform.localScale = npcData.ToolHeadLocalScale;
                UnityEngine.Object.DestroyImmediate(pickaxeHead.GetComponent<Collider>());
                miningNpc.ConfigureTool(pickaxeHandle.transform);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(npcObject, NpcPrefabPath);
                return saved != null ? saved.GetComponent<MiningNpc>() : null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(npcObject);
            }
        }

        private static void ConfigureNpcPrefab(GameObject npcObject, MiningNpc miningNpc,
            NpcData npcData)
        {
            var npcSerialized = new SerializedObject(miningNpc);
            SetReferenceIfMissing(npcSerialized.FindProperty("npcData"), npcData);
            npcSerialized.ApplyModifiedPropertiesWithoutUndo();

            CapsuleCollider capsule = npcObject.GetComponent<CapsuleCollider>() ??
                                      npcObject.AddComponent<CapsuleCollider>();
            capsule.radius = npcData.ColliderRadius;
            capsule.height = npcData.ColliderHeight;

            Rigidbody body = npcObject.GetComponent<Rigidbody>() ?? npcObject.AddComponent<Rigidbody>();
            body.mass = npcData.Mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private static void CreateOrUpdateRuntimePrefab(MiningNpc npcPrefab, MiningGameData gameData,
            NpcData npcData, OreSpawnData spawnData, MiningUpgradeData upgradeData, MiningUiData uiData,
            MiningAudioData audioData, MiningRebirthData rebirthData,
            OreRewardPopup rewardPopupPrefab)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimePrefabPath);
            bool isNew = existing == null;
            GameObject runtime = isNew
                ? new GameObject("MiningRuntime")
                : PrefabUtility.LoadPrefabContents(RuntimePrefabPath);

            try
            {
                PlayerWallet wallet = runtime.GetComponent<PlayerWallet>() ?? runtime.AddComponent<PlayerWallet>();
                OreSpawner spawner = runtime.GetComponent<OreSpawner>() ?? runtime.AddComponent<OreSpawner>();
                OreClickInput clickInput = runtime.GetComponent<OreClickInput>() ?? runtime.AddComponent<OreClickInput>();
                NpcShop shop = runtime.GetComponent<NpcShop>() ?? runtime.AddComponent<NpcShop>();
                MiningHud hud = runtime.GetComponent<MiningHud>() ?? runtime.AddComponent<MiningHud>();
                MiningUpgradeSystem upgradeSystem = runtime.GetComponent<MiningUpgradeSystem>() ??
                                                    runtime.AddComponent<MiningUpgradeSystem>();
                MiningUpgradePanel upgradePanel = runtime.GetComponent<MiningUpgradePanel>() ??
                                                   runtime.AddComponent<MiningUpgradePanel>();
                MiningRebirthSystem rebirthSystem = runtime.GetComponent<MiningRebirthSystem>() ??
                                                      runtime.AddComponent<MiningRebirthSystem>();
                MiningRebirthPanel rebirthPanel = runtime.GetComponent<MiningRebirthPanel>() ??
                                                    runtime.AddComponent<MiningRebirthPanel>();
                MiningAudioManager audioManager = runtime.GetComponent<MiningAudioManager>() ??
                                                  runtime.AddComponent<MiningAudioManager>();
                MiningAudioSettingsPanel audioSettingsPanel =
                    runtime.GetComponent<MiningAudioSettingsPanel>() ??
                    runtime.AddComponent<MiningAudioSettingsPanel>();
                MiningUiPanelCoordinator panelCoordinator =
                    runtime.GetComponent<MiningUiPanelCoordinator>() ??
                    runtime.AddComponent<MiningUiPanelCoordinator>();
                MiningOrbitCamera orbitCamera = runtime.GetComponent<MiningOrbitCamera>() ??
                                                runtime.AddComponent<MiningOrbitCamera>();

                Transform audioRoot = EnsureChildObject(runtime.transform, "Audio");
                Transform musicSourceObject = EnsureChildObject(audioRoot, "Music Source");
                Transform sfxSourceObject = EnsureChildObject(audioRoot, "SFX Source");
                AudioSource musicSource = musicSourceObject.GetComponent<AudioSource>();
                if (musicSource == null)
                {
                    musicSource = musicSourceObject.gameObject.AddComponent<AudioSource>();
                }

                AudioSource sfxSource = sfxSourceObject.GetComponent<AudioSource>();
                if (sfxSource == null)
                {
                    sfxSource = sfxSourceObject.gameObject.AddComponent<AudioSource>();
                }
                musicSource.playOnAwake = false;
                musicSource.loop = audioData.LoopMusic;
                musicSource.volume = audioData.MusicVolume;
                musicSource.spatialBlend = 0f;
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.volume = 1f;
                sfxSource.spatialBlend = 0f;

                var serialized = new SerializedObject(spawner);
                SetReferenceIfMissing(serialized.FindProperty("wallet"), wallet);
                SetReferenceIfMissing(serialized.FindProperty("spawnData"), spawnData);
                SetReferenceIfMissing(serialized.FindProperty("upgradeSystem"), upgradeSystem);
                SetReferenceIfMissing(serialized.FindProperty("uiData"), uiData);
                SetReferenceIfMissing(serialized.FindProperty("rewardPopupPrefab"), rewardPopupPrefab);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var upgradeSystemSerialized = new SerializedObject(upgradeSystem);
                SetReferenceIfMissing(upgradeSystemSerialized.FindProperty("wallet"), wallet);
                SetReferenceIfMissing(upgradeSystemSerialized.FindProperty("upgradeData"), upgradeData);
                upgradeSystemSerialized.ApplyModifiedPropertiesWithoutUndo();

                var clickSerialized = new SerializedObject(clickInput);
                SetReferenceIfMissing(clickSerialized.FindProperty("gameData"), gameData);
                clickSerialized.ApplyModifiedPropertiesWithoutUndo();

                var shopSerialized = new SerializedObject(shop);
                SetReferenceIfMissing(shopSerialized.FindProperty("wallet"), wallet);
                SetReferenceIfMissing(shopSerialized.FindProperty("oreSpawner"), spawner);
                SetReferenceIfMissing(shopSerialized.FindProperty("npcPrefab"), npcPrefab);
                SetReferenceIfMissing(shopSerialized.FindProperty("npcData"), npcData);
                SetReferenceIfMissing(shopSerialized.FindProperty("upgradeSystem"), upgradeSystem);
                shopSerialized.ApplyModifiedPropertiesWithoutUndo();

                var hudSerialized = new SerializedObject(hud);
                SetReferenceIfMissing(hudSerialized.FindProperty("wallet"), wallet);
                SetReferenceIfMissing(hudSerialized.FindProperty("npcShop"), shop);
                SetReferenceIfMissing(hudSerialized.FindProperty("gameData"), gameData);
                CreateEditableHudIfMissing(runtime, hudSerialized, uiData);
                SerializedProperty npcCountFormat = hudSerialized.FindProperty("npcCountFormat");
                if (npcCountFormat != null && npcCountFormat.stringValue == "NPC đào quặng: {0}")
                {
                    npcCountFormat.stringValue = "NPC đào quặng: {0}/{1}";
                }
                SerializedProperty purchaseFailedMessage =
                    hudSerialized.FindProperty("purchaseFailedMessage");
                if (purchaseFailedMessage != null &&
                    purchaseFailedMessage.stringValue == "Không đủ tiền hoặc thiếu cấu hình NPC.")
                {
                    purchaseFailedMessage.stringValue =
                        "Không đủ tiền hoặc đã đạt giới hạn thợ mỏ.";
                }
                hudSerialized.ApplyModifiedPropertiesWithoutUndo();

                ConfigureUpgradePanel(runtime, upgradePanel, upgradeSystem, wallet, upgradeData, uiData);
                ConfigureRebirthHud(runtime, rebirthPanel, rebirthSystem, wallet, uiData);
                ConfigureAudioSettings(runtime, audioSettingsPanel, audioManager, uiData);
                ConfigureUiPanelCoordinator(runtime, panelCoordinator, uiData);
                ConfigureButtonSfx(runtime, audioManager);

                var rebirthSerialized = new SerializedObject(rebirthSystem);
                rebirthSerialized.FindProperty("wallet").objectReferenceValue = wallet;
                rebirthSerialized.FindProperty("upgradeSystem").objectReferenceValue = upgradeSystem;
                rebirthSerialized.FindProperty("rebirthData").objectReferenceValue = rebirthData;
                rebirthSerialized.ApplyModifiedPropertiesWithoutUndo();

                var audioSerialized = new SerializedObject(audioManager);
                SetReferenceIfMissing(audioSerialized.FindProperty("audioData"), audioData);
                SetReferenceIfMissing(audioSerialized.FindProperty("oreSpawner"), spawner);
                SetReferenceIfMissing(audioSerialized.FindProperty("npcShop"), shop);
                SetReferenceIfMissing(audioSerialized.FindProperty("upgradeSystem"), upgradeSystem);
                SetReferenceIfMissing(audioSerialized.FindProperty("upgradePanel"), upgradePanel);
                SetReferenceIfMissing(audioSerialized.FindProperty("rebirthSystem"), rebirthSystem);
                SetReferenceIfMissing(audioSerialized.FindProperty("rebirthPanel"), rebirthPanel);
                SetReferenceIfMissing(audioSerialized.FindProperty("audioSettingsPanel"),
                    audioSettingsPanel);
                audioSerialized.FindProperty("musicSource").objectReferenceValue = musicSource;
                audioSerialized.FindProperty("sfxSource").objectReferenceValue = sfxSource;
                audioSerialized.ApplyModifiedPropertiesWithoutUndo();

                var cameraSerialized = new SerializedObject(orbitCamera);
                SetReferenceIfMissing(cameraSerialized.FindProperty("gameData"), gameData);
                cameraSerialized.ApplyModifiedPropertiesWithoutUndo();

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(runtime);
                PrefabUtility.SaveAsPrefabAsset(runtime, RuntimePrefabPath);
            }
            finally
            {
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(runtime);
                }
                else
                {
                    PrefabUtility.UnloadPrefabContents(runtime);
                }
            }
        }

        private static void CreateEditableHudIfMissing(GameObject runtime, SerializedObject hudSerialized,
            MiningUiData uiData)
        {
            Transform existingCanvas = runtime.transform.Find(HudCanvasName);
            if (existingCanvas != null)
            {
                WireExistingHud(existingCanvas, hudSerialized);
                StyleEditableShop(existingCanvas, uiData);
                return;
            }

            GameObject canvasObject = new(HudCanvasName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.transform.SetParent(runtime.transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = uiData.CanvasSortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = uiData.ReferenceResolution;
            scaler.matchWidthOrHeight = uiData.MatchWidthOrHeight;

            GameObject panel = CreateUiObject("NPC Shop", canvasObject.transform, typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = uiData.ShopPanelPosition;
            panelRect.sizeDelta = uiData.ShopPanelSize;
            panel.GetComponent<Image>().color = uiData.ShopPanelColor;

            TextMeshProUGUI moneyText = CreateText(panel.transform, "Money", uiData.MoneyTextPosition,
                Mathf.RoundToInt(uiData.MoneyFontSize), uiData.ShopTextSize, uiData.ShopTextColor);
            TextMeshProUGUI npcCountText = CreateText(panel.transform, "NPC Count", uiData.NpcCountTextPosition,
                Mathf.RoundToInt(uiData.NpcCountFontSize), uiData.ShopTextSize, uiData.ShopTextColor);
            Button buyButton = CreateButton(panel.transform, "Buy Mining NPC", uiData.BuyButtonPosition,
                uiData.BuyButtonSize, Mathf.RoundToInt(uiData.BuyButtonFontSize), out TextMeshProUGUI buyLabel);
            StyleButton(buyButton.transform, uiData.BuyButtonColor, uiData.BuyButtonTextColor,
                uiData.OutlineColor, uiData.OutlineThickness, uiData);
            TextMeshProUGUI statusText = CreateText(panel.transform, "Status", uiData.StatusTextPosition,
                Mathf.RoundToInt(uiData.StatusFontSize), uiData.ShopTextSize, uiData.StatusTextColor);
            statusText.text = "Chuột phải: xoay • WASD: di chuyển";

            hudSerialized.FindProperty("moneyText").objectReferenceValue = moneyText;
            hudSerialized.FindProperty("npcCountText").objectReferenceValue = npcCountText;
            hudSerialized.FindProperty("statusText").objectReferenceValue = statusText;
            hudSerialized.FindProperty("buyButton").objectReferenceValue = buyButton;
            hudSerialized.FindProperty("buyButtonLabel").objectReferenceValue = buyLabel;
            StyleEditableShop(canvasObject.transform, uiData);
        }

        private static void StyleEditableShop(Transform canvas, MiningUiData uiData)
        {
            if (canvas == null || uiData == null)
            {
                return;
            }

            Canvas targetCanvas = canvas.GetComponent<Canvas>();
            if (targetCanvas != null)
            {
                targetCanvas.sortingOrder = uiData.CanvasSortingOrder;
            }

            Transform panel = canvas.Find("NPC Shop");
            if (panel == null)
            {
                return;
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = uiData.ShopPanelPosition;
            panelRect.sizeDelta = uiData.ShopPanelSize;
            Image panelImage = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
            panelImage.color = uiData.ShopPanelColor;
            ApplyOutline(panel.gameObject, uiData.OutlineColor, uiData.OutlineThickness);

            Transform header = EnsureUiObject(panel, "Header", typeof(Image));
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = uiData.ShopHeaderSize;
            header.GetComponent<Image>().color = uiData.ShopHeaderColor;
            ApplyOutline(header.gameObject, uiData.OutlineColor, uiData.OutlineThickness);

            TextMeshProUGUI title = EnsureText(header, "Title");
            StretchRect(title.rectTransform);
            title.text = uiData.ShopTitle;
            title.fontSize = uiData.ShopTitleFontSize;
            title.color = uiData.TitleTextColor;
            title.alignment = TextAlignmentOptions.Center;

            StyleShopText(panel.Find("Money"), uiData.MoneyTextPosition, uiData.ShopStatTextSize,
                uiData.MoneyFontSize, uiData.ShopTextColor);
            StyleShopText(panel.Find("NPC Count"), uiData.NpcCountTextPosition, uiData.ShopStatTextSize,
                uiData.NpcCountFontSize, uiData.ShopTextColor);
            StyleShopText(panel.Find("Status"), uiData.StatusTextPosition, uiData.ShopTextSize,
                uiData.StatusFontSize, uiData.StatusTextColor);

            Transform buyButton = panel.Find("Buy Mining NPC");
            ConfigureTopLeftRect(buyButton, uiData.BuyButtonPosition, uiData.BuyButtonSize);
            StyleButton(buyButton, uiData.BuyButtonColor, uiData.BuyButtonTextColor,
                uiData.OutlineColor, uiData.OutlineThickness, uiData);
            TextMeshProUGUI buyLabel = buyButton?.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (buyLabel != null)
            {
                buyLabel.fontSize = uiData.BuyButtonFontSize;
            }

            EnsureHudIcon(panel, "Money Icon", uiData.MoneyIconPosition, uiData.HudIconSize,
                uiData.MoneyIconSprite, uiData.MoneyIconFallback, uiData.MoneyIconColor, uiData);
            EnsureHudIcon(panel, "NPC Icon", uiData.NpcIconPosition, uiData.HudIconSize,
                uiData.NpcIconSprite, uiData.NpcIconFallback, uiData.NpcIconColor, uiData);
            EnsureHudIcon(buyButton, "Icon", uiData.BuyButtonIconPosition, uiData.HudIconSize,
                uiData.BuyNpcIconSprite, uiData.BuyNpcIconFallback, uiData.MoneyIconColor, uiData);
        }

        private static void StyleShopText(Transform target, Vector2 position, Vector2 size,
            float fontSize, Color color)
        {
            ConfigureTopLeftRect(target, position, size);
            TextMeshProUGUI text = target?.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                return;
            }

            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Left;
        }

        private static void ConfigureTopLeftRect(Transform target, Vector2 position, Vector2 size)
        {
            RectTransform rect = target as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ConfigureUpgradePanel(GameObject runtime, MiningUpgradePanel panelController,
            MiningUpgradeSystem upgradeSystem, PlayerWallet wallet, MiningUpgradeData upgradeData,
            MiningUiData uiData)
        {
            Transform canvas = runtime.transform.Find(HudCanvasName);
            Transform shopPanel = canvas?.Find("NPC Shop");
            if (canvas == null || shopPanel == null || uiData == null)
            {
                return;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.referenceResolution = uiData.ReferenceResolution;
                scaler.matchWidthOrHeight = uiData.MatchWidthOrHeight;
            }

            RectTransform shopRect = shopPanel.GetComponent<RectTransform>();
            float requiredShopHeight = Mathf.Abs(uiData.OpenButtonPosition.y) + uiData.OpenButtonSize.y +
                                       uiData.OutlineThickness;
            if (shopRect != null && shopRect.sizeDelta.y < requiredShopHeight)
            {
                shopRect.sizeDelta = new Vector2(shopRect.sizeDelta.x, requiredShopHeight);
            }

            Transform openButtonTransform = shopPanel.Find("Open Upgrades");
            if (openButtonTransform == null)
            {
                Button openButton = CreateButton(shopPanel, "Open Upgrades", uiData.OpenButtonPosition,
                    uiData.OpenButtonSize, Mathf.RoundToInt(uiData.NavigationFontSize), out TextMeshProUGUI openLabel);
                openLabel.text = "NÂNG CẤP";
                openButtonTransform = openButton.transform;
            }
            StyleButton(openButtonTransform, uiData.NavigationButtonColor, uiData.TitleTextColor,
                uiData.OutlineColor, uiData.OutlineThickness, uiData);
            EnsureHudIcon(openButtonTransform, "Icon", uiData.OpenUpgradeIconPosition,
                uiData.HudIconSize, uiData.OpenUpgradeIconSprite, uiData.OpenUpgradeIconFallback,
                uiData.UpgradeIconColor, uiData);

            Transform upgradePanelTransform = canvas.Find("Upgrade Panel");
            if (upgradePanelTransform == null)
            {
                GameObject upgradePanel = CreateUiObject("Upgrade Panel", canvas, typeof(Image));
                upgradePanelTransform = upgradePanel.transform;
            }

            RectTransform panelRect = upgradePanelTransform.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            Vector2 upgradePanelSize = uiData.PanelSize;
            upgradePanelSize.y = Mathf.Max(upgradePanelSize.y,
                uiData.ExperienceUpgradePanelMinimumHeight);
            panelRect.sizeDelta = upgradePanelSize;
            Image panelImage = upgradePanelTransform.GetComponent<Image>() ??
                               upgradePanelTransform.gameObject.AddComponent<Image>();
            panelImage.color = uiData.PanelColor;
            ApplyOutline(upgradePanelTransform.gameObject, uiData.OutlineColor, uiData.OutlineThickness);

            Transform header = EnsureUiObject(upgradePanelTransform, "Header", typeof(Image));
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = uiData.HeaderSize;
            header.GetComponent<Image>().color = uiData.HeaderColor;
            ApplyOutline(header.gameObject, uiData.OutlineColor, uiData.OutlineThickness);

            Transform oldTitle = upgradePanelTransform.Find("Title");
            if (oldTitle != null)
            {
                oldTitle.SetParent(header, false);
            }
            TextMeshProUGUI title = EnsureText(header, "Title");
            StretchRect(title.rectTransform);
            title.text = "NÂNG CẤP";
            title.fontSize = uiData.TitleFontSize;
            title.color = uiData.TitleTextColor;
            title.alignment = TextAlignmentOptions.Center;

            Button closeButton = EnsureStyledButton(upgradePanelTransform, "Close", uiData.CloseButtonPosition,
                uiData.CloseButtonSize, uiData.CloseButtonColor, uiData.TitleTextColor, uiData);
            Image closeImage = closeButton.GetComponent<Image>();
            closeImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            closeImage.type = Image.Type.Simple;
            TextMeshProUGUI closeLabel = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            closeLabel.text = "X";
            closeLabel.fontSize = uiData.NavigationFontSize;

            EnsureUpgradeCard(upgradePanelTransform, "Money Reward Upgrade", 0,
                GetUpgradePreview(upgradeData.MoneyReward), uiData.MoneyRewardIconSprite,
                uiData.MoneyRewardIconFallback, uiData);
            EnsureUpgradeCard(upgradePanelTransform, "Rare Ore Upgrade", 1,
                GetUpgradePreview(upgradeData.RareOreSpawn), uiData.RareOreIconSprite,
                uiData.RareOreIconFallback, uiData);
            EnsureUpgradeCard(upgradePanelTransform, "Ore Damage Upgrade", 2,
                GetUpgradePreview(upgradeData.OreDamage), uiData.OreDamageIconSprite,
                uiData.OreDamageIconFallback, uiData);
            EnsureUpgradeCard(upgradePanelTransform, "Ore Spawn Speed Upgrade", 3,
                GetUpgradePreview(upgradeData.OreSpawnSpeed), uiData.OreSpawnSpeedIconSprite,
                uiData.OreSpawnSpeedIconFallback, uiData);
            EnsureUpgradeCard(upgradePanelTransform, "NPC Move Speed Upgrade", 4,
                GetUpgradePreview(upgradeData.NpcMoveSpeed), uiData.NpcMoveSpeedIconSprite,
                uiData.NpcMoveSpeedIconFallback, uiData);
            EnsureUpgradeCard(upgradePanelTransform, "NPC Capacity Upgrade", 5,
                GetCapacityUpgradePreview(upgradeData.NpcCapacity), uiData.NpcCapacityIconSprite,
                uiData.NpcCapacityIconFallback, uiData);
            Sprite luckyBlockRewardIcon = uiData.LuckyBlockRewardIconSprite != null
                ? uiData.LuckyBlockRewardIconSprite
                : AssetDatabase.LoadAssetAtPath<Sprite>(LuckyBlockRewardIconPath);
            Sprite luckyBlockDropChanceIcon = uiData.LuckyBlockDropChanceIconSprite != null
                ? uiData.LuckyBlockDropChanceIconSprite
                : AssetDatabase.LoadAssetAtPath<Sprite>(LuckyBlockDropChanceIconPath);
            EnsureUpgradeCard(upgradePanelTransform, "Lucky Block Reward Upgrade", 6,
                GetUpgradePreview(upgradeData.LuckyBlockReward),
                luckyBlockRewardIcon, uiData.LuckyBlockRewardIconFallback, uiData);
            EnsureUpgradeCard(upgradePanelTransform, "Lucky Block Drop Chance Upgrade", 7,
                GetUpgradePreview(upgradeData.LuckyBlockDropChance),
                luckyBlockDropChanceIcon,
                uiData.LuckyBlockDropChanceIconFallback, uiData);
            Sprite npcExperienceIcon = uiData.NpcExperienceIconSprite != null
                ? uiData.NpcExperienceIconSprite
                : AssetDatabase.LoadAssetAtPath<Sprite>(NpcExperienceIconPath);
            EnsureUpgradeCard(upgradePanelTransform, "NPC Experience Upgrade", 8,
                GetUpgradePreview(upgradeData.NpcExperience), npcExperienceIcon,
                uiData.NpcExperienceIconFallback, uiData);

            Vector2 backButtonPosition = uiData.BackButtonPosition;
            backButtonPosition.y = Mathf.Min(backButtonPosition.y,
                uiData.ExperienceUpgradeBackButtonY);
            Button backButton = EnsureStyledButton(upgradePanelTransform, "Back", backButtonPosition,
                uiData.BackButtonSize, uiData.NavigationButtonColor, uiData.TitleTextColor, uiData);
            TextMeshProUGUI backLabel = backButton.GetComponentInChildren<TextMeshProUGUI>(true);
            backLabel.text = "QUAY LẠI";
            backLabel.fontSize = uiData.NavigationFontSize;
            // Keep the panel visible in Edit/Prefab Mode so designers can select and edit it.
            // MiningUpgradePanel applies the play-mode visibility during Awake.
            upgradePanelTransform.gameObject.SetActive(true);

            var serialized = new SerializedObject(panelController);
            SetReferenceIfMissing(serialized.FindProperty("upgradeSystem"), upgradeSystem);
            SetReferenceIfMissing(serialized.FindProperty("wallet"), wallet);
            SetReferenceIfMissing(serialized.FindProperty("shopPanel"), shopPanel.gameObject);
            SetReferenceIfMissing(serialized.FindProperty("upgradePanel"), upgradePanelTransform.gameObject);
            SetReferenceIfMissing(serialized.FindProperty("panelCoordinator"),
                runtime.GetComponent<MiningUiPanelCoordinator>());
            serialized.FindProperty("openOnPlay").boolValue = uiData.OpenUpgradePanelOnPlay;
            WireUpgradeButton(serialized, "openButton", null, openButtonTransform);
            WireUpgradeButton(serialized, "backButton", null, upgradePanelTransform.Find("Back"));
            WireUpgradeButton(serialized, "closeButton", null, upgradePanelTransform.Find("Close"));
            WireUpgradeButton(serialized, "moneyRewardButton", "moneyRewardLabel",
                upgradePanelTransform.Find("Money Reward Upgrade"));
            WireUpgradeButton(serialized, "rareOreSpawnButton", "rareOreSpawnLabel",
                upgradePanelTransform.Find("Rare Ore Upgrade"));
            WireUpgradeButton(serialized, "oreDamageButton", "oreDamageLabel",
                upgradePanelTransform.Find("Ore Damage Upgrade"));
            WireUpgradeButton(serialized, "oreSpawnSpeedButton", "oreSpawnSpeedLabel",
                upgradePanelTransform.Find("Ore Spawn Speed Upgrade"));
            WireUpgradeButton(serialized, "npcMoveSpeedButton", "npcMoveSpeedLabel",
                upgradePanelTransform.Find("NPC Move Speed Upgrade"));
            WireUpgradeButton(serialized, "npcCapacityButton", "npcCapacityLabel",
                upgradePanelTransform.Find("NPC Capacity Upgrade"));
            WireUpgradeButton(serialized, "luckyBlockRewardButton", "luckyBlockRewardLabel",
                upgradePanelTransform.Find("Lucky Block Reward Upgrade"));
            WireUpgradeButton(serialized, "luckyBlockDropChanceButton",
                "luckyBlockDropChanceLabel",
                upgradePanelTransform.Find("Lucky Block Drop Chance Upgrade"));
            WireUpgradeButton(serialized, "npcExperienceButton", "npcExperienceLabel",
                upgradePanelTransform.Find("NPC Experience Upgrade"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureNpcProgressionHud(GameObject sceneRoot,
            NpcProgressionSystem progressionSystem, NpcShop shop, NpcData npcData,
            MiningUiData uiData)
        {
            Transform canvas = sceneRoot.transform.Find(HudCanvasName);
            if (canvas == null || uiData == null)
            {
                return;
            }

            Transform hud = EnsureUiObject(canvas, "NPC Progress HUD", typeof(Image),
                typeof(NpcProgressionHud));
            ConfigureTopLeftRect(hud, uiData.NpcProgressHudPosition, uiData.NpcProgressHudSize);
            Image hudImage = hud.GetComponent<Image>();
            hudImage.color = uiData.NpcProgressPanelColor;
            hudImage.raycastTarget = false;
            ApplyOutline(hud.gameObject, uiData.OutlineColor, uiData.OutlineThickness);

            TextMeshProUGUI levelLabel = EnsureText(hud, "Level");
            ConfigureTopLeftRect(levelLabel.transform, uiData.NpcProgressTextPosition,
                uiData.NpcProgressTextSize);
            levelLabel.text = "CẤP THỢ MỎ: 1";
            levelLabel.fontSize = uiData.NpcProgressTitleFontSize;
            levelLabel.color = uiData.ShopTextColor;
            levelLabel.alignment = TextAlignmentOptions.Left;
            levelLabel.raycastTarget = false;

            TextMeshProUGUI powerLabel = EnsureText(hud, "Power");
            ConfigureTopLeftRect(powerLabel.transform, uiData.NpcPowerTextPosition,
                uiData.NpcPowerTextSize);
            powerLabel.text = "NPC: 0  •  POWER: 1  •  TỔNG DMG: 0";
            powerLabel.fontSize = uiData.NpcProgressInfoFontSize;
            powerLabel.color = uiData.ShopTextColor;
            powerLabel.alignment = TextAlignmentOptions.Left;
            powerLabel.raycastTarget = false;

            Transform bar = EnsureUiObject(hud, "Experience Bar", typeof(Image));
            ConfigureTopLeftRect(bar, uiData.NpcExperienceBarPosition,
                uiData.NpcExperienceBarSize);
            Image barBackground = bar.GetComponent<Image>();
            barBackground.color = uiData.NpcExperienceBarBackgroundColor;
            barBackground.raycastTarget = false;

            Transform fillTransform = EnsureUiObject(bar, "Fill", typeof(Image));
            StretchRect(fillTransform.GetComponent<RectTransform>());
            Image fill = fillTransform.GetComponent<Image>();
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            fill.color = uiData.NpcExperienceBarColor;
            fill.raycastTarget = false;

            TextMeshProUGUI experienceLabel = EnsureText(hud, "Experience");
            ConfigureTopLeftRect(experienceLabel.transform, uiData.NpcExperienceTextPosition,
                uiData.NpcExperienceTextSize);
            experienceLabel.text = "0 / 10 XP";
            experienceLabel.fontSize = uiData.NpcProgressInfoFontSize;
            experienceLabel.color = uiData.ShopTextColor;
            experienceLabel.alignment = TextAlignmentOptions.Center;
            experienceLabel.raycastTarget = false;

            NpcProgressionHud controller = hud.GetComponent<NpcProgressionHud>();
            var serialized = new SerializedObject(controller);
            SetReferenceIfMissing(serialized.FindProperty("progressionSystem"), progressionSystem);
            SetReferenceIfMissing(serialized.FindProperty("npcShop"), shop);
            SetReferenceIfMissing(serialized.FindProperty("npcData"), npcData);
            SetReferenceIfMissing(serialized.FindProperty("uiData"), uiData);
            SetReferenceIfMissing(serialized.FindProperty("levelLabel"), levelLabel);
            SetReferenceIfMissing(serialized.FindProperty("powerLabel"), powerLabel);
            SetReferenceIfMissing(serialized.FindProperty("experienceLabel"), experienceLabel);
            SetReferenceIfMissing(serialized.FindProperty("experienceFill"), fill);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static Button EnsureUpgradeCard(Transform parent, string name, int index,
            string preview, Sprite iconSprite, string iconFallback, MiningUiData uiData)
        {
            Vector2 position = uiData.FirstCardPosition + Vector2.down * uiData.CardSpacing * index;
            Button button = EnsureStyledButton(parent, name, position, uiData.CardSize, uiData.CardColor,
                uiData.CardTextColor, uiData);
            TextMeshProUGUI label = EnsureText(button.transform, "Label");
            label.text = preview;
            label.alignment = TextAlignmentOptions.Left;
            label.margin = uiData.CardTextMargin;
            EnsureHudIcon(button.transform, "Icon", uiData.UpgradeCardIconPosition,
                uiData.UpgradeCardIconSize, iconSprite, iconFallback,
                uiData.UpgradeIconColor, uiData);
            StyleUpgradeCardIcon(button.transform.Find("Icon"), iconSprite, uiData);
            return button;
        }

        private static void ConfigureAudioSettings(GameObject runtime,
            MiningAudioSettingsPanel panelController, MiningAudioManager audioManager,
            MiningUiData uiData)
        {
            Transform canvas = runtime.transform.Find(HudCanvasName);
            if (canvas == null || uiData == null)
            {
                return;
            }

            Transform menuButtonTransform = EnsureUiObject(canvas, "Audio Menu Button",
                typeof(Image), typeof(Button));
            ConfigureTopRightRect(menuButtonTransform, uiData.AudioMenuButtonPosition,
                uiData.AudioMenuButtonSize);
            StyleButton(menuButtonTransform, uiData.NavigationButtonColor, uiData.TitleTextColor,
                uiData.OutlineColor, uiData.OutlineThickness, uiData);
            TextMeshProUGUI menuLabel = EnsureText(menuButtonTransform, "Label");
            StretchRect(menuLabel.rectTransform);
            menuLabel.text = "ÂM THANH";
            menuLabel.fontSize = uiData.NavigationFontSize;
            menuLabel.color = uiData.TitleTextColor;
            menuLabel.alignment = TextAlignmentOptions.Center;

            Transform panel = EnsureUiObject(canvas, "Audio Settings Panel", typeof(Image));
            ConfigureCenteredRect(panel, Vector2.zero, uiData.AudioPanelSize);
            panel.GetComponent<Image>().color = uiData.AudioPanelColor;
            ApplyOutline(panel.gameObject, uiData.OutlineColor, uiData.OutlineThickness);

            Transform header = EnsureUiObject(panel, "Header", typeof(Image));
            ConfigureTopLeftRect(header, Vector2.zero, uiData.AudioHeaderSize);
            header.GetComponent<Image>().color = uiData.AudioHeaderColor;
            ApplyOutline(header.gameObject, uiData.OutlineColor, uiData.OutlineThickness);
            TextMeshProUGUI title = EnsureText(header, "Title");
            StretchRect(title.rectTransform);
            title.text = "CÀI ĐẶT ÂM THANH";
            title.fontSize = uiData.AudioTitleFontSize;
            title.color = uiData.TitleTextColor;
            title.alignment = TextAlignmentOptions.Center;

            Button closeButton = EnsureStyledButton(panel, "Close",
                uiData.AudioCloseButtonPosition, uiData.AudioCloseButtonSize,
                uiData.CloseButtonColor, uiData.TitleTextColor, uiData);
            TextMeshProUGUI closeLabel = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            closeLabel.text = "X";
            closeLabel.fontSize = uiData.NavigationFontSize;

            Slider masterSlider = EnsureAudioSliderRow(panel, "Master", 0, "MASTER",
                uiData, out TextMeshProUGUI masterValue);
            Slider musicSlider = EnsureAudioSliderRow(panel, "Music", 1, "MUSIC",
                uiData, out TextMeshProUGUI musicValue);
            Slider sfxSlider = EnsureAudioSliderRow(panel, "SFX", 2, "SFX",
                uiData, out TextMeshProUGUI sfxValue);

            panel.gameObject.SetActive(true);
            var serialized = new SerializedObject(panelController);
            SetReferenceIfMissing(serialized.FindProperty("audioManager"), audioManager);
            SetReferenceIfMissing(serialized.FindProperty("settingsPanel"), panel.gameObject);
            SetReferenceIfMissing(serialized.FindProperty("panelCoordinator"),
                runtime.GetComponent<MiningUiPanelCoordinator>());
            SetReferenceIfMissing(serialized.FindProperty("openButton"),
                menuButtonTransform.GetComponent<Button>());
            SetReferenceIfMissing(serialized.FindProperty("closeButton"), closeButton);
            SetReferenceIfMissing(serialized.FindProperty("masterSlider"), masterSlider);
            SetReferenceIfMissing(serialized.FindProperty("musicSlider"), musicSlider);
            SetReferenceIfMissing(serialized.FindProperty("sfxSlider"), sfxSlider);
            SetReferenceIfMissing(serialized.FindProperty("masterValueLabel"), masterValue);
            SetReferenceIfMissing(serialized.FindProperty("musicValueLabel"), musicValue);
            SetReferenceIfMissing(serialized.FindProperty("sfxValueLabel"), sfxValue);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureUiPanelCoordinator(GameObject runtime,
            MiningUiPanelCoordinator coordinator, MiningUiData uiData)
        {
            Transform canvas = runtime.transform.Find(HudCanvasName);
            if (canvas == null || coordinator == null)
            {
                return;
            }

            var serialized = new SerializedObject(coordinator);
            SetReferenceIfMissing(serialized.FindProperty("uiData"), uiData);
            SetReferenceIfMissing(serialized.FindProperty("shopPanel"),
                canvas.Find("NPC Shop")?.GetComponent<RectTransform>());
            SetReferenceIfMissing(serialized.FindProperty("rebirthHud"),
                canvas.Find("Rebirth HUD")?.GetComponent<RectTransform>());
            SetReferenceIfMissing(serialized.FindProperty("audioMenuButton"),
                canvas.Find("Audio Menu Button")?.GetComponent<RectTransform>());
            SetReferenceIfMissing(serialized.FindProperty("upgradePanel"),
                canvas.Find("Upgrade Panel")?.GetComponent<RectTransform>());
            SetReferenceIfMissing(serialized.FindProperty("rebirthPanel"),
                canvas.Find("Rebirth Confirmation")?.GetComponent<RectTransform>());
            SetReferenceIfMissing(serialized.FindProperty("audioSettingsPanel"),
                canvas.Find("Audio Settings Panel")?.GetComponent<RectTransform>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureButtonSfx(GameObject runtime, MiningAudioManager audioManager)
        {
            Transform canvas = runtime.transform.Find(HudCanvasName);
            if (canvas == null || audioManager == null)
            {
                return;
            }

            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                MiningButtonSfx player = button.GetComponent<MiningButtonSfx>() ??
                                         button.gameObject.AddComponent<MiningButtonSfx>();
                var serialized = new SerializedObject(player);
                SetReferenceIfMissing(serialized.FindProperty("audioManager"), audioManager);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Slider EnsureAudioSliderRow(Transform panel, string objectName, int index,
            string displayName, MiningUiData uiData, out TextMeshProUGUI valueLabel)
        {
            Vector2 rowPosition = uiData.AudioFirstRowPosition +
                                  Vector2.down * uiData.AudioRowSpacing * index;
            TextMeshProUGUI nameLabel = EnsureText(panel, objectName + " Label");
            ConfigureTopLeftRect(nameLabel.transform, rowPosition, uiData.AudioLabelSize);
            nameLabel.text = displayName;
            nameLabel.fontSize = uiData.AudioLabelFontSize;
            nameLabel.color = uiData.CardTextColor;
            nameLabel.alignment = TextAlignmentOptions.Left;

            Vector2 sliderPosition = rowPosition +
                                     Vector2.right * (uiData.AudioLabelSize.x +
                                                      uiData.AudioColumnSpacing);
            Transform sliderTransform = EnsureUiObject(panel, objectName + " Slider",
                typeof(Image), typeof(Slider));
            ConfigureTopLeftRect(sliderTransform, sliderPosition, uiData.AudioSliderSize);
            Image background = sliderTransform.GetComponent<Image>();
            background.color = uiData.AudioSliderBackgroundColor;

            Transform fillArea = EnsureUiObject(sliderTransform, "Fill Area");
            StretchRect(fillArea.GetComponent<RectTransform>());
            Transform fill = EnsureUiObject(fillArea, "Fill", typeof(Image));
            StretchRect(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = uiData.AudioSliderColor;

            Transform handleArea = EnsureUiObject(sliderTransform, "Handle Slide Area");
            StretchRect(handleArea.GetComponent<RectTransform>());
            Transform handle = EnsureUiObject(handleArea, "Handle", typeof(Image));
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = Vector2.one * (uiData.AudioSliderSize.y +
                                                   uiData.AudioHandleExtraSize);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            handleImage.color = uiData.TitleTextColor;

            Slider slider = sliderTransform.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            Vector2 valuePosition = sliderPosition +
                                    Vector2.right * (uiData.AudioSliderSize.x +
                                                     uiData.AudioColumnSpacing);
            valueLabel = EnsureText(panel, objectName + " Value");
            ConfigureTopLeftRect(valueLabel.transform, valuePosition, uiData.AudioValueSize);
            valueLabel.text = "100%";
            valueLabel.fontSize = uiData.AudioLabelFontSize;
            valueLabel.color = uiData.CardTextColor;
            valueLabel.alignment = TextAlignmentOptions.Center;
            return slider;
        }

        private static void ConfigureRebirthHud(GameObject runtime, MiningRebirthPanel panelController,
            MiningRebirthSystem rebirthSystem, PlayerWallet wallet, MiningUiData uiData)
        {
            Transform canvas = runtime.transform.Find(HudCanvasName);
            if (canvas == null || uiData == null)
            {
                return;
            }

            Transform hud = EnsureUiObject(canvas, "Rebirth HUD", typeof(Image));
            ConfigureTopRightRect(hud, uiData.RebirthHudPosition, uiData.RebirthHudSize);
            hud.GetComponent<Image>().color = uiData.RebirthHudColor;
            ApplyOutline(hud.gameObject, uiData.OutlineColor, uiData.OutlineThickness);

            Transform hudHeader = EnsureUiObject(hud, "Header", typeof(Image));
            ConfigureTopLeftRect(hudHeader, Vector2.zero, uiData.RebirthHudHeaderSize);
            hudHeader.GetComponent<Image>().color = uiData.RebirthHeaderColor;
            ApplyOutline(hudHeader.gameObject, uiData.OutlineColor, uiData.OutlineThickness);
            TextMeshProUGUI hudTitle = EnsureText(hudHeader, "Title");
            StretchRect(hudTitle.rectTransform);
            hudTitle.text = "REBIRTH";
            hudTitle.fontSize = uiData.RebirthTitleFontSize;
            hudTitle.color = uiData.TitleTextColor;
            hudTitle.alignment = TextAlignmentOptions.Center;

            TextMeshProUGUI boostLabel = EnsureText(hud, "Boost");
            ConfigureTopLeftRect(boostLabel.transform, uiData.RebirthBoostPosition,
                uiData.RebirthBoostSize);
            boostLabel.text = "REBIRTH 0  •  x1.00 TIỀN";
            boostLabel.fontSize = uiData.RebirthInfoFontSize;
            boostLabel.color = uiData.CardTextColor;
            boostLabel.alignment = TextAlignmentOptions.Center;

            MicroBar progressBar = EnsureRebirthProgressBar(hud, uiData);
            TextMeshProUGUI progressLabel = EnsureText(hud, "Progress Label");
            ConfigureTopLeftRect(progressLabel.transform, uiData.RebirthProgressPosition,
                uiData.RebirthProgressSize);
            progressLabel.text = "0 / 1,000 TIỀN";
            progressLabel.fontSize = uiData.RebirthInfoFontSize;
            progressLabel.color = uiData.TitleTextColor;
            progressLabel.alignment = TextAlignmentOptions.Center;
            progressLabel.raycastTarget = false;

            Button openButton = EnsureStyledButton(hud, "Open Rebirth",
                uiData.RebirthOpenButtonPosition, uiData.RebirthOpenButtonSize,
                uiData.RebirthHeaderColor, uiData.TitleTextColor, uiData);
            TextMeshProUGUI openLabel = openButton.GetComponentInChildren<TextMeshProUGUI>(true);
            openLabel.text = "REBIRTH";
            openLabel.fontSize = uiData.RebirthInfoFontSize;

            Transform modal = EnsureUiObject(canvas, "Rebirth Confirmation", typeof(Image));
            ConfigureCenteredRect(modal, Vector2.zero, uiData.RebirthModalSize);
            modal.GetComponent<Image>().color = uiData.PanelColor;
            ApplyOutline(modal.gameObject, uiData.OutlineColor, uiData.OutlineThickness);

            Transform modalHeader = EnsureUiObject(modal, "Header", typeof(Image));
            ConfigureTopLeftRect(modalHeader, Vector2.zero, uiData.RebirthModalHeaderSize);
            modalHeader.GetComponent<Image>().color = uiData.RebirthHeaderColor;
            ApplyOutline(modalHeader.gameObject, uiData.OutlineColor, uiData.OutlineThickness);
            TextMeshProUGUI modalTitle = EnsureText(modalHeader, "Title");
            StretchRect(modalTitle.rectTransform);
            modalTitle.text = "REBIRTH!";
            modalTitle.fontSize = uiData.TitleFontSize;
            modalTitle.color = uiData.TitleTextColor;
            modalTitle.alignment = TextAlignmentOptions.Center;

            TextMeshProUGUI warning = EnsureText(modal, "Warning");
            ConfigureTopLeftRect(warning.transform, uiData.RebirthWarningPosition,
                uiData.RebirthWarningSize);
            warning.text = "CẢNH BÁO!\n\nBạn sắp Rebirth! Toàn bộ tiền và mọi nâng cấp hiện tại sẽ bị xóa.";
            warning.fontSize = uiData.RebirthWarningFontSize;
            warning.color = uiData.CardTextColor;
            warning.alignment = TextAlignmentOptions.Center;
            warning.textWrappingMode = TextWrappingModes.Normal;

            TextMeshProUGUI nextBoost = EnsureText(modal, "Next Boost");
            ConfigureTopLeftRect(nextBoost.transform, uiData.RebirthNextBoostPosition,
                uiData.RebirthNextBoostSize);
            nextBoost.text = "Boost vĩnh viễn sau Rebirth: x1.10 tiền";
            nextBoost.fontSize = uiData.RebirthModalTextFontSize;
            nextBoost.color = uiData.CardTextColor;
            nextBoost.alignment = TextAlignmentOptions.Center;

            Button confirmButton = EnsureStyledButton(modal, "Confirm Rebirth",
                uiData.RebirthConfirmButtonPosition, uiData.RebirthModalButtonSize,
                uiData.RebirthConfirmColor, uiData.CardTextColor, uiData);
            TextMeshProUGUI confirmLabel = confirmButton.GetComponentInChildren<TextMeshProUGUI>(true);
            confirmLabel.text = "REBIRTH!";
            confirmLabel.fontSize = uiData.RebirthModalTextFontSize;

            Button cancelButton = EnsureStyledButton(modal, "Cancel Rebirth",
                uiData.RebirthCancelButtonPosition, uiData.RebirthModalButtonSize,
                uiData.RebirthCancelColor, uiData.TitleTextColor, uiData);
            TextMeshProUGUI cancelLabel = cancelButton.GetComponentInChildren<TextMeshProUGUI>(true);
            cancelLabel.text = "ĐỂ SAU";
            cancelLabel.fontSize = uiData.RebirthModalTextFontSize;

            // Keep the modal selectable while editing. MiningRebirthPanel hides it in Awake.
            modal.gameObject.SetActive(true);

            var serialized = new SerializedObject(panelController);
            serialized.FindProperty("rebirthSystem").objectReferenceValue = rebirthSystem;
            serialized.FindProperty("wallet").objectReferenceValue = wallet;
            serialized.FindProperty("confirmationPanel").objectReferenceValue = modal.gameObject;
            SetReferenceIfMissing(serialized.FindProperty("panelCoordinator"),
                runtime.GetComponent<MiningUiPanelCoordinator>());
            serialized.FindProperty("openButton").objectReferenceValue = openButton;
            serialized.FindProperty("confirmButton").objectReferenceValue = confirmButton;
            serialized.FindProperty("cancelButton").objectReferenceValue = cancelButton;
            serialized.FindProperty("progressLabel").objectReferenceValue = progressLabel;
            serialized.FindProperty("boostLabel").objectReferenceValue = boostLabel;
            serialized.FindProperty("warningLabel").objectReferenceValue = warning;
            serialized.FindProperty("nextBoostLabel").objectReferenceValue = nextBoost;
            serialized.FindProperty("progressBar").objectReferenceValue = progressBar;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MicroBar EnsureRebirthProgressBar(Transform parent, MiningUiData uiData)
        {
            Transform existing = parent.Find("Progress Bar");
            if (existing == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiMicroBarPrefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"MicroBar UI prefab is missing at {UiMicroBarPrefabPath}.");
                    return null;
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                if (instance == null)
                {
                    Debug.LogError("Could not create the Rebirth progress MicroBar.");
                    return null;
                }
                instance.name = "Progress Bar";
                existing = instance.transform;
            }

            ConfigureTopLeftRect(existing, uiData.RebirthProgressPosition, uiData.RebirthProgressSize);
            MicroBar bar = existing.GetComponent<MicroBar>();
            if (bar == null)
            {
                Debug.LogError("Rebirth Progress Bar has no MicroBar component.", existing);
                return null;
            }

            var serialized = new SerializedObject(bar);
            SerializedProperty simpleBar = serialized.FindProperty("simpleBar");
            simpleBar.FindPropertyRelative("_adaptiveColor").boolValue = false;
            simpleBar.FindPropertyRelative("_barPrimaryColor").colorValue = uiData.RebirthProgressColor;
            simpleBar.FindPropertyRelative("_ghostBarDamageColor").colorValue =
                uiData.RebirthProgressGhostColor;
            simpleBar.FindPropertyRelative("_ghostBarHealColor").colorValue =
                uiData.RebirthProgressGhostColor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return bar;
        }

        private static void ConfigureTopRightRect(Transform target, Vector2 position, Vector2 size)
        {
            RectTransform rect = target as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ConfigureCenteredRect(Transform target, Vector2 position, Vector2 size)
        {
            RectTransform rect = target as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void StyleUpgradeCardIcon(Transform icon, Sprite iconSprite, MiningUiData uiData)
        {
            Image existingSprite = icon?.Find("Sprite")?.GetComponent<Image>();
            if (icon == null || (iconSprite == null &&
                (existingSprite == null || existingSprite.sprite == null)))
            {
                return;
            }

            Image background = icon.GetComponent<Image>();
            if (background != null)
            {
                background.color = Color.clear;
            }

            Outline outline = icon.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = Color.clear;
                outline.effectDistance = Vector2.zero;
            }

            RectTransform spriteRect = icon.Find("Sprite")?.GetComponent<RectTransform>();
            if (spriteRect != null)
            {
                spriteRect.offsetMin = Vector2.one * uiData.UpgradeCardIconPadding;
                spriteRect.offsetMax = Vector2.one * -uiData.UpgradeCardIconPadding;
            }
        }

        private static void EnsureHudIcon(Transform parent, string name, Vector2 position,
            Vector2 size, Sprite iconSprite, string fallbackText, Color backgroundColor,
            MiningUiData uiData)
        {
            if (parent == null)
            {
                return;
            }

            Transform icon = EnsureUiObject(parent, name, typeof(Image));
            ConfigureTopLeftRect(icon, position, size);
            Image background = icon.GetComponent<Image>();
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            background.type = Image.Type.Simple;
            background.color = backgroundColor;
            background.raycastTarget = false;
            ApplyOutline(icon.gameObject, uiData.OutlineColor, uiData.OutlineThickness);

            Transform spriteTransform = EnsureUiObject(icon, "Sprite", typeof(Image));
            RectTransform spriteRect = spriteTransform.GetComponent<RectTransform>();
            StretchRect(spriteRect);
            spriteRect.offsetMin = Vector2.one * uiData.HudIconPadding;
            spriteRect.offsetMax = Vector2.one * -uiData.HudIconPadding;
            Image spriteImage = spriteTransform.GetComponent<Image>();
            // A sprite assigned directly in the editable Scene UI belongs to the designer.
            // A setup refresh may replace it with a configured GameData sprite, but a null
            // GameData slot must never erase the existing icon.
            if (iconSprite != null || spriteImage.sprite == null)
            {
                spriteImage.sprite = iconSprite;
            }
            spriteImage.color = uiData.IconSymbolColor;
            spriteImage.preserveAspect = true;
            bool hasSprite = spriteImage.sprite != null;
            spriteImage.enabled = hasSprite;
            spriteImage.raycastTarget = false;

            TextMeshProUGUI symbol = EnsureText(icon, "Symbol");
            StretchRect(symbol.rectTransform);
            symbol.text = fallbackText;
            symbol.fontSize = uiData.HudIconFontSize;
            symbol.color = uiData.IconSymbolColor;
            symbol.alignment = TextAlignmentOptions.Center;
            symbol.enabled = !hasSprite;
            symbol.raycastTarget = false;
        }

        private static Button EnsureStyledButton(Transform parent, string name, Vector2 position,
            Vector2 size, Color background, Color textColor, MiningUiData uiData)
        {
            Transform transform = EnsureUiObject(parent, name, typeof(Image), typeof(Button));
            RectTransform rect = transform.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            transform.GetComponent<Image>().color = background;
            ApplyOutline(transform.gameObject, uiData.OutlineColor, uiData.OutlineThickness);

            TextMeshProUGUI label = EnsureText(transform, "Label");
            StretchRect(label.rectTransform);
            label.fontSize = uiData.CardFontSize;
            label.color = textColor;
            label.alignment = TextAlignmentOptions.Center;
            ConfigureSmoothButton(transform, uiData);
            return transform.GetComponent<Button>();
        }

        private static void ConfigureSmoothButton(Transform buttonTransform, MiningUiData uiData)
        {
            if (buttonTransform == null || uiData == null)
            {
                return;
            }

            SmoothButtonPunch animation = buttonTransform.GetComponent<SmoothButtonPunch>();
            if (!uiData.SmoothButtonAnimationEnabled)
            {
                if (animation != null)
                {
                    animation.enabled = false;
                }
                return;
            }

            CenterRectPivot(buttonTransform as RectTransform);

            animation ??= buttonTransform.gameObject.AddComponent<SmoothButtonPunch>();
            animation.enabled = true;
            animation.Configure(uiData.ButtonHoverScale, uiData.ButtonHoverPunchScale,
                uiData.ButtonPressedScale, uiData.ButtonClickBounceScale,
                uiData.ButtonHoverPunchDuration, uiData.ButtonHoverSettleDuration,
                uiData.ButtonPressDuration, uiData.ButtonClickBounceDuration,
                uiData.ButtonClickSettleDuration);
        }

        private static void CenterRectPivot(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            Vector2 centeredPivot = new(0.5f, 0.5f);
            Vector2 pivotDelta = centeredPivot - rect.pivot;
            rect.anchoredPosition += Vector2.Scale(pivotDelta, rect.rect.size);
            rect.pivot = centeredPivot;
        }

        private static Transform EnsureUiObject(Transform parent, string name, params Type[] components)
        {
            Transform result = parent.Find(name);
            if (result == null)
            {
                result = CreateUiObject(name, parent).transform;
            }
            foreach (Type type in components)
            {
                if (result.GetComponent(type) == null)
                {
                    result.gameObject.AddComponent(type);
                }
            }
            return result;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name)
        {
            Transform textTransform = EnsureUiObject(parent, name, typeof(TextMeshProUGUI));
            return textTransform.GetComponent<TextMeshProUGUI>();
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void ApplyOutline(GameObject target, Color color, float thickness)
        {
            Outline outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(thickness, -thickness);
            outline.useGraphicAlpha = true;
        }

        private static void StyleButton(Transform buttonTransform, Color background, Color textColor,
            Color outlineColor, float outlineThickness, MiningUiData uiData)
        {
            if (buttonTransform == null)
            {
                return;
            }
            Image image = buttonTransform.GetComponent<Image>();
            if (image != null)
            {
                image.color = background;
            }
            TextMeshProUGUI label = buttonTransform.Find("Label")?.GetComponent<TextMeshProUGUI>() ??
                                    buttonTransform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = textColor;
            }
            ApplyOutline(buttonTransform.gameObject, outlineColor, outlineThickness);
            ConfigureSmoothButton(buttonTransform, uiData);
        }

        private static string GetUpgradePreview(MiningUpgradeDefinition definition)
        {
            return $"{definition.DisplayName}\n+{definition.PercentPerStack:0.##}%  " +
                   $"[0/{definition.MaximumStacks}]  -  {definition.StartingCost} tiền";
        }

        private static string GetCapacityUpgradePreview(MiningUpgradeDefinition definition)
        {
            return $"{definition.DisplayName}\n+{definition.ValuePerStack:0} thợ mỏ  " +
                   $"[0/{definition.MaximumStacks}]  -  {definition.StartingCost} tiền";
        }

        private static void WireUpgradeButton(SerializedObject serialized, string buttonProperty,
            string labelProperty, Transform buttonTransform)
        {
            SetReferenceIfMissing(serialized.FindProperty(buttonProperty),
                buttonTransform?.GetComponent<Button>());
            if (!string.IsNullOrEmpty(labelProperty))
            {
                SetReferenceIfMissing(serialized.FindProperty(labelProperty),
                    buttonTransform?.Find("Label")?.GetComponent<TextMeshProUGUI>());
            }
        }

        private static void WireExistingHud(Transform canvas, SerializedObject hudSerialized)
        {
            Transform panel = canvas.Find("NPC Shop");
            if (panel == null)
            {
                return;
            }

            hudSerialized.FindProperty("moneyText").objectReferenceValue =
                ConvertToTextMeshPro(panel.Find("Money"), TextAlignmentOptions.Left);
            hudSerialized.FindProperty("npcCountText").objectReferenceValue =
                ConvertToTextMeshPro(panel.Find("NPC Count"), TextAlignmentOptions.Left);
            hudSerialized.FindProperty("statusText").objectReferenceValue =
                ConvertToTextMeshPro(panel.Find("Status"), TextAlignmentOptions.Left);
            Transform buttonTransform = panel.Find("Buy Mining NPC");
            SetReferenceIfMissing(hudSerialized.FindProperty("buyButton"), buttonTransform?.GetComponent<Button>());
            hudSerialized.FindProperty("buyButtonLabel").objectReferenceValue =
                ConvertToTextMeshPro(buttonTransform?.Find("Label"), TextAlignmentOptions.Center);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 position, int size)
        {
            return CreateText(parent, name, position, size, Vector2.zero, Color.white);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 position, int size,
            Vector2 rectSize, Color color)
        {
            GameObject textObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = rectSize;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Left;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 position,
            Vector2 size, int fontSize, out TextMeshProUGUI label)
        {
            GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            buttonObject.GetComponent<Image>().color = Color.white;
            Button button = buttonObject.GetComponent<Button>();
            label = CreateText(buttonObject.transform, "Label", Vector2.zero, fontSize);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.08f, 0.06f, 0.03f);
            return button;
        }

        private static TextMeshProUGUI ConvertToTextMeshPro(Transform target,
            TextAlignmentOptions alignment)
        {
            if (target == null)
            {
                return null;
            }

            TextMeshProUGUI existing = target.GetComponent<TextMeshProUGUI>();
            if (existing != null)
            {
                return existing;
            }

            UnityEngine.UI.Text legacy = target.GetComponent<UnityEngine.UI.Text>();
            string value = legacy != null ? legacy.text : string.Empty;
            Color color = legacy != null ? legacy.color : Color.white;
            float fontSize = legacy != null ? legacy.fontSize : 18f;
            if (legacy != null)
            {
                UnityEngine.Object.DestroyImmediate(legacy, true);
            }

            TextMeshProUGUI converted = target.gameObject.AddComponent<TextMeshProUGUI>();
            converted.text = value;
            converted.color = color;
            converted.fontSize = fontSize;
            converted.alignment = alignment;
            return converted;
        }

        private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
        {
            GameObject result = new(name, typeof(RectTransform));
            result.layer = LayerMask.NameToLayer("UI");
            result.transform.SetParent(parent, false);
            foreach (Type component in components)
            {
                result.AddComponent(component);
            }
            return result;
        }

        private static Transform EnsureChildObject(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new(name);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static bool EnsureEventSystemInScene(Scene scene, bool registerUndo)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<EventSystem>(true) != null)
                {
                    return false;
                }
            }

            GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
            if (registerUndo)
            {
                Undo.RegisterCreatedObjectUndo(eventSystem, "Add Event System");
            }
            return true;
        }

        private static void AddHealthBarIfMissing(GameObject root, Ore ore)
        {
            if (root.GetComponentInChildren<OreHealthBar>(true) != null)
            {
                return;
            }

            GameObject barPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath);
            if (barPrefab == null)
            {
                Debug.LogError($"MicroBar prefab is missing at {HealthBarPrefabPath}.");
                return;
            }

            GameObject barObject = PrefabUtility.InstantiatePrefab(barPrefab, root.transform) as GameObject;
            if (barObject == null)
            {
                Debug.LogError($"Could not add a MicroBar to ore prefab '{root.name}'.");
                return;
            }

            barObject.name = "Ore Health Bar";
            barObject.transform.localPosition = Vector3.zero;
            barObject.transform.localRotation = Quaternion.identity;
            barObject.transform.localScale = Vector3.one * ore.Data.HealthBarScale;
            MicroBar bar = barObject.GetComponent<MicroBar>();
            OreHealthBar binding = barObject.AddComponent<OreHealthBar>();
            binding.Configure(ore, bar, barObject.transform);
        }

        private static void SetReferenceIfMissing(SerializedProperty property, UnityEngine.Object value)
        {
            if (property != null && property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static bool AddRuntimeToSceneIfMissing(Scene scene, bool registerUndo)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<OreSpawner>(true) == null)
                {
                    continue;
                }

                bool changed = ConvertRuntimeRootToSceneGameManager(root, registerUndo);
                Debug.Log(changed
                    ? "Converted the existing runtime into an independent Scene GameManager."
                    : "Scene GameManager already exists. No changes made.", root);
                return changed;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Mining system template is missing at {RuntimePrefabPath}.");
                return false;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError("Could not create the Scene GameManager.");
                return false;
            }

            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (registerUndo)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Add Scene GameManager");
            }

            ConvertRuntimeRootToSceneGameManager(instance, registerUndo);
            return true;
        }

        private static bool ConvertRuntimeRootToSceneGameManager(GameObject root, bool registerUndo)
        {
            bool changed = false;
            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(root);
            if (prefabRoot != null)
            {
                PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely,
                    registerUndo ? InteractionMode.UserAction : InteractionMode.AutomatedAction);
                root = prefabRoot;
                changed = true;
            }

            if (root.name != "GameManager")
            {
                if (registerUndo)
                {
                    Undo.RecordObject(root, "Rename Runtime To GameManager");
                }

                root.name = "GameManager";
                EditorUtility.SetDirty(root);
                changed = true;
            }

            changed |= OrganizeSceneManagers(root, registerUndo);

            return changed;
        }

        private static bool OrganizeSceneManagers(GameObject root, bool registerUndo)
        {
            var layout = new Dictionary<Type, string>
            {
                { typeof(OreSpawner), "Ore System" },
                { typeof(OreClickInput), "Ore System" },
                { typeof(NpcShop), "NPC System" },
                { typeof(MiningUpgradeSystem), "Upgrade System" },
                { typeof(MiningRebirthSystem), "Rebirth System" },
                { typeof(MiningAudioManager), "Audio System" },
                { typeof(MiningHud), "UI Systems" },
                { typeof(MiningUpgradePanel), "UI Systems" },
                { typeof(MiningRebirthPanel), "UI Systems" },
                { typeof(MiningAudioSettingsPanel), "UI Systems" },
                { typeof(MiningUiPanelCoordinator), "UI Systems" },
                { typeof(MiningOrbitCamera), "Camera System" }
            };

            var replacements = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
            var oldComponents = new List<Component>();
            bool changed = false;

            foreach (Component component in root.GetComponents<Component>())
            {
                if (component == null || component is Transform || component is PlayerWallet ||
                    !layout.TryGetValue(component.GetType(), out string groupName))
                {
                    continue;
                }

                Transform group = root.transform.Find(groupName);
                if (group == null)
                {
                    GameObject groupObject = new(groupName);
                    groupObject.transform.SetParent(root.transform, false);
                    if (registerUndo)
                    {
                        Undo.RegisterCreatedObjectUndo(groupObject, $"Create {groupName}");
                    }
                    group = groupObject.transform;
                }

                Component replacement = registerUndo
                    ? Undo.AddComponent(group.gameObject, component.GetType())
                    : group.gameObject.AddComponent(component.GetType());
                EditorUtility.CopySerialized(component, replacement);
                replacements.Add(component, replacement);
                oldComponents.Add(component);
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            RemapSceneReferences(root.gameObject.scene, replacements);
            foreach (Component oldComponent in oldComponents)
            {
                if (registerUndo)
                {
                    Undo.DestroyObjectImmediate(oldComponent);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(oldComponent);
                }
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            EditorUtility.SetDirty(root);
            return true;
        }

        private static void RemapSceneReferences(Scene scene,
            IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> replacements)
        {
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Component component in sceneRoot.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                    {
                        continue;
                    }

                    SerializedObject serialized = new(component);
                    SerializedProperty property = serialized.GetIterator();
                    bool enterChildren = true;
                    bool modified = false;
                    while (property.NextVisible(enterChildren))
                    {
                        enterChildren = false;
                        if (property.propertyType != SerializedPropertyType.ObjectReference ||
                            property.objectReferenceValue == null ||
                            !replacements.TryGetValue(property.objectReferenceValue, out UnityEngine.Object replacement))
                        {
                            continue;
                        }

                        property.objectReferenceValue = replacement;
                        modified = true;
                    }

                    if (modified)
                    {
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(component);
                    }
                }
            }
        }

        private static void EnsureCollider(GameObject root)
        {
            if (root.GetComponent<Collider>() != null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = root.transform.InverseTransformVector(worldBounds.size);
            collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent))
            {
                throw new InvalidOperationException($"Invalid Unity folder path: {path}");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
