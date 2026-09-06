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
        private const string DataFolder = "Assets/GameData/Ores";
        private const string PrefabFolder = "Assets/Prefabs/Ores";
        private const string NpcPrefabFolder = "Assets/Prefabs/NPC";
        private const string SystemPrefabFolder = "Assets/Prefabs/Systems";
        private const string NpcPrefabPath = NpcPrefabFolder + "/MiningNpc.prefab";
        private const string RuntimePrefabPath = SystemPrefabFolder + "/MiningRuntime.prefab";
        private const string HudCanvasName = "Mining HUD Canvas";
        private const string HealthBarPrefabPath =
            "Assets/Microlight/MicroBar/Prefabs/SimpleBars/Sprite_SimpleMicroBarSRP.prefab";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        private readonly struct OreSpec
        {
            public OreSpec(OreKind kind, string name, string description, int tier,
                int miningPower, int durability, int clickDamage, int sellValue,
                Color mapColor, string modelFile)
            {
                Kind = kind;
                Name = name;
                Description = description;
                Tier = tier;
                MiningPower = miningPower;
                Durability = durability;
                ClickDamage = clickDamage;
                SellValue = sellValue;
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
            public Color MapColor { get; }
            public string ModelFile { get; }
        }

        private static readonly OreSpec[] StarterOres =
        {
            new(OreKind.Stone, "Stone", "Common stone. The first material a miner can break.",
                1, 1, 10, 1, 1, new Color(0.48f, 0.52f, 0.56f), "stone_tier1.fbx"),
            new(OreKind.Coal, "Coal", "Dark fuel ore unlocked after basic stone mining.",
                2, 3, 20, 2, 4, new Color(0.10f, 0.12f, 0.14f), "coal_tier2.fbx"),
            new(OreKind.Copper, "Copper", "Valuable metallic ore used for stronger upgrades.",
                3, 6, 35, 3, 9, new Color(0.82f, 0.32f, 0.08f), "copper_tier3.fbx")
        };

        [MenuItem("Mining Simulator/Setup/Create or Update Starter Ores")]
        public static void CreateOrUpdateStarterOres()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(NpcDataFolder);
            EnsureFolder(SpawnDataFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(NpcPrefabFolder);
            EnsureFolder(SystemPrefabFolder);

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
            MiningNpc npcPrefab = CreateOrUpdateNpcPrefab(npcData);
            CreateOrUpdateRuntimePrefab(npcPrefab, gameData, npcData, spawnData);

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
                float[] defaultWeights = { 60f, 30f, 10f };
                for (int index = 0; index < dataAssets.Length; index++)
                {
                    SerializedProperty entry = table.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("ore").objectReferenceValue = dataAssets[index];
                    entry.FindPropertyRelative("spawnWeight").floatValue =
                        index < defaultWeights.Length ? defaultWeights[index] : 1f;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(spawnData);
            }
            return spawnData;
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
                npcObject.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
                MiningNpc miningNpc = npcObject.AddComponent<MiningNpc>();
                ConfigureNpcPrefab(npcObject, miningNpc, npcData);

                GameObject helmet = GameObject.CreatePrimitive(PrimitiveType.Cube);
                helmet.name = "Miner Helmet";
                helmet.transform.SetParent(npcObject.transform, false);
                helmet.transform.localPosition = new Vector3(0f, 0.86f, 0.08f);
                helmet.transform.localScale = new Vector3(0.9f, 0.18f, 0.92f);
                UnityEngine.Object.DestroyImmediate(helmet.GetComponent<Collider>());

                GameObject pickaxeHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickaxeHandle.name = "Pickaxe Handle";
                pickaxeHandle.transform.SetParent(npcObject.transform, false);
                pickaxeHandle.transform.localPosition = new Vector3(0.65f, 0f, 0f);
                pickaxeHandle.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
                pickaxeHandle.transform.localScale = new Vector3(0.08f, 0.85f, 0.08f);
                UnityEngine.Object.DestroyImmediate(pickaxeHandle.GetComponent<Collider>());

                GameObject pickaxeHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickaxeHead.name = "Pickaxe Head";
                pickaxeHead.transform.SetParent(pickaxeHandle.transform, false);
                pickaxeHead.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                pickaxeHead.transform.localScale = new Vector3(3.8f, 0.18f, 0.65f);
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
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private static void CreateOrUpdateRuntimePrefab(MiningNpc npcPrefab, MiningGameData gameData,
            NpcData npcData, OreSpawnData spawnData)
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
                MiningOrbitCamera orbitCamera = runtime.GetComponent<MiningOrbitCamera>() ??
                                                runtime.AddComponent<MiningOrbitCamera>();

                var walletSerialized = new SerializedObject(wallet);
                SetReferenceIfMissing(walletSerialized.FindProperty("gameData"), gameData);
                walletSerialized.ApplyModifiedPropertiesWithoutUndo();

                var serialized = new SerializedObject(spawner);
                SetReferenceIfMissing(serialized.FindProperty("wallet"), wallet);
                SetReferenceIfMissing(serialized.FindProperty("spawnData"), spawnData);
                serialized.ApplyModifiedPropertiesWithoutUndo();

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
                CreateEditableHudIfMissing(runtime, hudSerialized);
                hudSerialized.ApplyModifiedPropertiesWithoutUndo();

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

        private static void CreateEditableHudIfMissing(GameObject runtime, SerializedObject hudSerialized)
        {
            Transform existingCanvas = runtime.transform.Find(HudCanvasName);
            if (existingCanvas != null)
            {
                WireExistingHud(existingCanvas, hudSerialized);
                return;
            }

            GameObject canvasObject = new(HudCanvasName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.transform.SetParent(runtime.transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = CreateUiObject("NPC Shop", canvasObject.transform, typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(330f, 190f);
            panel.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.06f, 0.94f);

            TextMeshProUGUI moneyText = CreateText(panel.transform, "Money", new Vector2(18f, -16f), 26);
            TextMeshProUGUI npcCountText = CreateText(panel.transform, "NPC Count", new Vector2(18f, -52f), 21);
            Button buyButton = CreateButton(panel.transform, new Vector2(18f, -88f), out TextMeshProUGUI buyLabel);
            TextMeshProUGUI statusText = CreateText(panel.transform, "Status", new Vector2(18f, -151f), 17);
            statusText.color = new Color(1f, 0.82f, 0.28f);
            statusText.text = "Chuột phải: xoay • WASD: di chuyển";

            hudSerialized.FindProperty("moneyText").objectReferenceValue = moneyText;
            hudSerialized.FindProperty("npcCountText").objectReferenceValue = npcCountText;
            hudSerialized.FindProperty("statusText").objectReferenceValue = statusText;
            hudSerialized.FindProperty("buyButton").objectReferenceValue = buyButton;
            hudSerialized.FindProperty("buyButtonLabel").objectReferenceValue = buyLabel;
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
            GameObject textObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(294f, 32f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            return text;
        }

        private static Button CreateButton(Transform parent, Vector2 position, out TextMeshProUGUI label)
        {
            GameObject buttonObject = CreateUiObject("Buy Mining NPC", parent, typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(294f, 54f);

            buttonObject.GetComponent<Image>().color = new Color(0.95f, 0.57f, 0.1f);
            Button button = buttonObject.GetComponent<Button>();
            label = CreateText(buttonObject.transform, "Label", Vector2.zero, 22);
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
            barObject.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            barObject.transform.localRotation = Quaternion.identity;
            barObject.transform.localScale = Vector3.one * 0.65f;
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
