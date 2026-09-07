#if UNITY_EDITOR
using System;
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
        private const string DataFolder = "Assets/GameData/Ores";
        private const string PrefabFolder = "Assets/Prefabs/Ores";
        private const string NpcPrefabFolder = "Assets/Prefabs/NPC";
        private const string SystemPrefabFolder = "Assets/Prefabs/Systems";
        private const string UiPrefabFolder = "Assets/Prefabs/UI";
        private const string NpcPrefabPath = NpcPrefabFolder + "/MiningNpc.prefab";
        private const string RuntimePrefabPath = SystemPrefabFolder + "/MiningRuntime.prefab";
        private const string RewardPopupPrefabPath = UiPrefabFolder + "/OreRewardPopup.prefab";
        private const string HudCanvasName = "Mining HUD Canvas";
        private const string HealthBarPrefabPath =
            "Assets/Microlight/MicroBar/Prefabs/SimpleBars/Sprite_SimpleMicroBarSRP.prefab";
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
            OreRewardPopup rewardPopupPrefab = CreateOrUpdateRewardPopupPrefab(uiData);
            MiningNpc npcPrefab = CreateOrUpdateNpcPrefab(npcData);
            CreateOrUpdateRuntimePrefab(npcPrefab, gameData, npcData, spawnData, upgradeData, uiData,
                audioData, rewardPopupPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Mining ore setup complete: {StarterOres.Length} independent OreData assets and prefabs.");
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
            MiningAudioData audioData, OreRewardPopup rewardPopupPrefab)
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
                MiningAudioManager audioManager = runtime.GetComponent<MiningAudioManager>() ??
                                                  runtime.AddComponent<MiningAudioManager>();
                MiningGameManager gameManager = runtime.GetComponent<MiningGameManager>() ??
                                                runtime.AddComponent<MiningGameManager>();
                MiningOrbitCamera orbitCamera = runtime.GetComponent<MiningOrbitCamera>() ??
                                                runtime.AddComponent<MiningOrbitCamera>();

                Transform audioRoot = EnsureChildObject(runtime.transform, "Audio");
                Transform musicSourceObject = EnsureChildObject(audioRoot, "Music Source");
                Transform sfxSourceObject = EnsureChildObject(audioRoot, "SFX Source");
                AudioSource musicSource = musicSourceObject.GetComponent<AudioSource>() ??
                                          musicSourceObject.gameObject.AddComponent<AudioSource>();
                AudioSource sfxSource = sfxSourceObject.GetComponent<AudioSource>() ??
                                        sfxSourceObject.gameObject.AddComponent<AudioSource>();

                var walletSerialized = new SerializedObject(wallet);
                SetReferenceIfMissing(walletSerialized.FindProperty("gameData"), gameData);
                walletSerialized.ApplyModifiedPropertiesWithoutUndo();

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
                shopSerialized.ApplyModifiedPropertiesWithoutUndo();

                var hudSerialized = new SerializedObject(hud);
                SetReferenceIfMissing(hudSerialized.FindProperty("wallet"), wallet);
                SetReferenceIfMissing(hudSerialized.FindProperty("npcShop"), shop);
                SetReferenceIfMissing(hudSerialized.FindProperty("gameData"), gameData);
                CreateEditableHudIfMissing(runtime, hudSerialized, uiData);
                hudSerialized.ApplyModifiedPropertiesWithoutUndo();

                ConfigureUpgradePanel(runtime, upgradePanel, upgradeSystem, wallet, upgradeData, uiData);

                var audioSerialized = new SerializedObject(audioManager);
                SetReferenceIfMissing(audioSerialized.FindProperty("audioData"), audioData);
                SetReferenceIfMissing(audioSerialized.FindProperty("oreSpawner"), spawner);
                SetReferenceIfMissing(audioSerialized.FindProperty("npcShop"), shop);
                SetReferenceIfMissing(audioSerialized.FindProperty("upgradeSystem"), upgradeSystem);
                SetReferenceIfMissing(audioSerialized.FindProperty("upgradePanel"), upgradePanel);
                SetReferenceIfMissing(audioSerialized.FindProperty("musicSource"), musicSource);
                SetReferenceIfMissing(audioSerialized.FindProperty("sfxSource"), sfxSource);
                audioSerialized.ApplyModifiedPropertiesWithoutUndo();

                var gameManagerSerialized = new SerializedObject(gameManager);
                SetReferenceIfMissing(gameManagerSerialized.FindProperty("wallet"), wallet);
                SetReferenceIfMissing(gameManagerSerialized.FindProperty("oreSpawner"), spawner);
                SetReferenceIfMissing(gameManagerSerialized.FindProperty("npcShop"), shop);
                SetReferenceIfMissing(gameManagerSerialized.FindProperty("upgradeSystem"), upgradeSystem);
                SetReferenceIfMissing(gameManagerSerialized.FindProperty("hud"), hud);
                SetReferenceIfMissing(gameManagerSerialized.FindProperty("upgradePanel"), upgradePanel);
                SetReferenceIfMissing(gameManagerSerialized.FindProperty("audioManager"), audioManager);
                SetReferenceIfMissing(gameManagerSerialized.FindProperty("orbitCamera"), orbitCamera);
                gameManagerSerialized.ApplyModifiedPropertiesWithoutUndo();

                var cameraSerialized = new SerializedObject(orbitCamera);
                SetReferenceIfMissing(cameraSerialized.FindProperty("gameData"), gameData);
                cameraSerialized.ApplyModifiedPropertiesWithoutUndo();

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
            panelRect.sizeDelta = uiData.PanelSize;
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

            Button backButton = EnsureStyledButton(upgradePanelTransform, "Back", uiData.BackButtonPosition,
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
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static void StyleUpgradeCardIcon(Transform icon, Sprite iconSprite, MiningUiData uiData)
        {
            if (icon == null || iconSprite == null)
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
            spriteImage.sprite = iconSprite;
            spriteImage.color = uiData.IconSymbolColor;
            spriteImage.preserveAspect = true;
            spriteImage.enabled = iconSprite != null;
            spriteImage.raycastTarget = false;

            TextMeshProUGUI symbol = EnsureText(icon, "Symbol");
            StretchRect(symbol.rectTransform);
            symbol.text = fallbackText;
            symbol.fontSize = uiData.HudIconFontSize;
            symbol.color = uiData.IconSymbolColor;
            symbol.alignment = TextAlignmentOptions.Center;
            symbol.enabled = iconSprite == null;
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

            animation ??= buttonTransform.gameObject.AddComponent<SmoothButtonPunch>();
            animation.enabled = true;
            animation.Configure(uiData.ButtonHoverScale, uiData.ButtonHoverPunchScale,
                uiData.ButtonPressedScale, uiData.ButtonClickBounceScale,
                uiData.ButtonHoverPunchDuration, uiData.ButtonHoverSettleDuration,
                uiData.ButtonPressDuration, uiData.ButtonClickBounceDuration,
                uiData.ButtonClickSettleDuration);
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
                if (root.GetComponentInChildren<OreSpawner>(true) != null)
                {
                    Debug.Log("MiningRuntime already exists in the active scene. No changes made.");
                    return false;
                }
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Mining runtime prefab is missing at {RuntimePrefabPath}.");
                return false;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError("Could not instantiate MiningRuntime in the active scene.");
                return false;
            }

            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (registerUndo)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Add Mining Runtime");
            }
            return true;
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
