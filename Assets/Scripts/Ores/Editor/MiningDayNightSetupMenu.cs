#if UNITY_EDITOR
using System;
using System.IO;
using Microlight.MicroBar;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Editor
{
    /// <summary>Creates only day/night content. Existing UI, icons and gameplay prefabs are untouched.</summary>
    public static class MiningDayNightSetupMenu
    {
        private const string DataFolder = "Assets/GameData/DayNight";
        private const string DayNightDataPath = DataFolder + "/DayNightData.asset";
        private const string OreDataFolder = "Assets/GameData/Ores";
        private const string PrefabFolder = "Assets/Prefabs/Ores";
        private const string ModelFolder = "Assets/Ores/Models";
        private const string MaterialFolder = "Assets/Ores/Materials";
        private const string HealthBarPrefabPath =
            "Assets/Microlight/MicroBar/Prefabs/SimpleBars/Sprite_SimpleMicroBarSRP.prefab";

        private readonly struct SpecialOreSpec
        {
            public SpecialOreSpec(OreKind kind, string name, string description, int tier,
                int power, int durability, int clickDamage, int reward, Color bodyColor,
                Color auraColor, string modelFile, SpecialOreTheme theme)
            {
                Kind = kind;
                Name = name;
                Description = description;
                Tier = tier;
                Power = power;
                Durability = durability;
                ClickDamage = clickDamage;
                Reward = reward;
                BodyColor = bodyColor;
                AuraColor = auraColor;
                ModelFile = modelFile;
                Theme = theme;
            }

            public OreKind Kind { get; }
            public string Name { get; }
            public string Description { get; }
            public int Tier { get; }
            public int Power { get; }
            public int Durability { get; }
            public int ClickDamage { get; }
            public int Reward { get; }
            public Color BodyColor { get; }
            public Color AuraColor { get; }
            public string ModelFile { get; }
            public SpecialOreTheme Theme { get; }
        }

        private static readonly SpecialOreSpec LightStone = new(
            OreKind.LightStone, "Light Stone",
            "A radiant stone that can appear only during the day.",
            7, 35, 220, 7, 140,
            new Color(0.34f, 0.31f, 0.25f), new Color(1f, 0.62f, 0.08f),
            "light_stone.fbx", SpecialOreTheme.Light);

        private static readonly SpecialOreSpec DarkStone = new(
            OreKind.DarkStone, "Dark Stone",
            "A shadow stone that can appear only during the night.",
            8, 45, 300, 8, 220,
            new Color(0.025f, 0.03f, 0.038f), new Color(0.025f, 0.075f, 0.09f),
            "dark_stone.fbx", SpecialOreTheme.Dark);

        [MenuItem("Mining Simulator/Setup/Create Day Night System And Ores")]
        public static void CreateDayNightSystemAndOres()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(OreDataFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);

            DayNightData dayNightData = GetOrCreateDayNightData();
            OreData lightData = GetOrCreateOreData(LightStone);
            OreData darkData = GetOrCreateOreData(DarkStone);
            GameObject lightPrefab = GetOrCreateOrePrefab(LightStone, lightData, dayNightData);
            GameObject darkPrefab = GetOrCreateOrePrefab(DarkStone, darkData, dayNightData);

            SetReferenceIfMissing(lightData, "prefab", lightPrefab);
            SetReferenceIfMissing(darkData, "prefab", darkPrefab);
            SetReferenceIfMissing(dayNightData, "lightStone", lightData);
            SetReferenceIfMissing(dayNightData, "darkStone", darkData);
            SetupActiveScene(dayNightData);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Day/night system and both timed ores are ready. Existing UI and icons were not changed.");
        }

        [MenuItem("Mining Simulator/Setup/Rebuild Day Night Ore Visuals")]
        public static void RebuildDayNightOreVisuals()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            DayNightData dayNightData = GetOrCreateDayNightData();
            OreData lightData = GetOrCreateOreData(LightStone);
            OreData darkData = GetOrCreateOreData(DarkStone);
            GameObject lightPrefab = RebuildOrePrefab(LightStone, lightData, dayNightData);
            GameObject darkPrefab = RebuildOrePrefab(DarkStone, darkData, dayNightData);
            SetReference(lightData, "prefab", lightPrefab);
            SetReference(darkData, "prefab", darkPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Light/Dark Stone models and shader auras rebuilt. UI, icons and gameplay balance were untouched.");
        }

        private static DayNightData GetOrCreateDayNightData()
        {
            DayNightData data = AssetDatabase.LoadAssetAtPath<DayNightData>(DayNightDataPath);
            if (data != null)
            {
                return data;
            }

            data = ScriptableObject.CreateInstance<DayNightData>();
            AssetDatabase.CreateAsset(data, DayNightDataPath);
            return data;
        }

        private static OreData GetOrCreateOreData(SpecialOreSpec spec)
        {
            string path = $"{OreDataFolder}/{spec.Name}.asset";
            OreData data = AssetDatabase.LoadAssetAtPath<OreData>(path);
            if (data != null)
            {
                return data;
            }

            data = ScriptableObject.CreateInstance<OreData>();
            AssetDatabase.CreateAsset(data, path);
            var serialized = new SerializedObject(data);
            serialized.FindProperty("kind").enumValueIndex = (int)spec.Kind;
            serialized.FindProperty("displayName").stringValue = spec.Name;
            serialized.FindProperty("description").stringValue = spec.Description;
            serialized.FindProperty("tier").intValue = spec.Tier;
            serialized.FindProperty("miningPowerRequired").intValue = spec.Power;
            serialized.FindProperty("durability").intValue = spec.Durability;
            serialized.FindProperty("clickDamage").intValue = spec.ClickDamage;
            serialized.FindProperty("baseSellValue").intValue = spec.Reward;
            serialized.FindProperty("maximumMiningNpcs").intValue = 5;
            serialized.FindProperty("npcStandDistance").floatValue = 1.75f;
            serialized.FindProperty("rarity").enumValueIndex = (int)OreRarity.Rare;
            serialized.FindProperty("healthBarWorldOffset").vector3Value = new Vector3(0f, 0.35f, 0f);
            serialized.FindProperty("mapColor").colorValue = spec.AuraColor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject GetOrCreateOrePrefab(
            SpecialOreSpec spec, OreData oreData, DayNightData dayNightData)
        {
            string prefabPath = $"{PrefabFolder}/{spec.Name}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                return existing;
            }

            return BuildOrePrefab(spec, oreData, dayNightData, prefabPath);
        }

        private static GameObject RebuildOrePrefab(
            SpecialOreSpec spec, OreData oreData, DayNightData dayNightData)
        {
            return BuildOrePrefab(spec, oreData, dayNightData,
                $"{PrefabFolder}/{spec.Name}.prefab");
        }

        private static GameObject BuildOrePrefab(
            SpecialOreSpec spec, OreData oreData, DayNightData dayNightData, string prefabPath)
        {

            string modelPath = $"{ModelFolder}/{spec.ModelFile}";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError($"Cannot create {spec.Name}: model is missing at {modelPath}.");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                return null;
            }

            try
            {
                PrefabUtility.UnpackPrefabInstance(instance,
                    PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                instance.name = spec.Name;
                Ore ore = instance.AddComponent<Ore>();
                ore.SetData(oreData);
                AssignMaterials(instance, spec);
                EnsureCollider(instance);
                AddAura(instance, spec, dayNightData);
                AddHealthBar(instance, ore);
                return PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void AssignMaterials(GameObject root, SpecialOreSpec spec)
        {
            Material body = GetOrCreateMaterial(spec.Name + " Body", spec.BodyColor, Color.black);
            Material glow = GetOrCreateMaterial(spec.Name + " Glow", spec.AuraColor, spec.AuraColor * 2.5f);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string materialName = materials[i] != null ? materials[i].name : string.Empty;
                    bool isGlow = renderer.name.IndexOf("Shard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  renderer.name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  materialName.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0;
                    materials[i] = isGlow ? glow : body;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, Color emission)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureCollider(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(bounds.center);
            Vector3 size = root.transform.InverseTransformVector(bounds.size);
            collider.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }

        private static void AddAura(GameObject root, SpecialOreSpec spec, DayNightData dayNightData)
        {
            GameObject auraObject = new("Special Aura");
            auraObject.transform.SetParent(root.transform, false);

            Light auraLight = null;
            var auraRenderers = new System.Collections.Generic.List<Renderer>();
            if (spec.Theme == SpecialOreTheme.Light)
            {
                auraLight = auraObject.AddComponent<Light>();
                auraLight.type = LightType.Point;
                auraLight.shadows = LightShadows.None;
                auraLight.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                float haloScale = dayNightData.LightHaloScale;
                Material haloMaterial = GetOrCreateAuraMaterial(
                    "Light Stone Halo", "Mining Simulator/Light Stone Halo",
                    dayNightData.LightStoneAuraColor, dayNightData.LightHaloOuterColor,
                    dayNightData.LightHaloOpacity, 0f);
                auraRenderers.Add(CreateAuraShell(auraObject.transform, "Warm Halo Inner",
                    new Vector3(1.65f, 1.38f, 1.65f) * haloScale, haloMaterial));
                auraRenderers.Add(CreateAuraShell(auraObject.transform, "Warm Halo Outer",
                    new Vector3(2.10f, 1.70f, 2.10f) * haloScale, haloMaterial));
            }
            else
            {
                Material shadowMaterial = GetOrCreateAuraMaterial(
                    "Dark Stone Shadow Aura", "Mining Simulator/Dark Stone Aura",
                    dayNightData.DarkStoneAuraColor, dayNightData.DarkAuraOuterColor,
                    dayNightData.DarkAuraOpacity, dayNightData.DarkAuraFlowSpeed);
                float shadowScale = dayNightData.DarkAuraScale;
                auraRenderers.Add(CreateAuraShell(auraObject.transform, "Shadow Mist Low",
                    new Vector3(2.10f, 0.98f, 2.10f) * shadowScale, shadowMaterial));
                Renderer middle = CreateAuraShell(auraObject.transform, "Shadow Mist Middle",
                    new Vector3(1.86f, 1.42f, 1.86f) * shadowScale, shadowMaterial);
                middle.transform.localRotation = Quaternion.Euler(0f, 37f, 0f);
                auraRenderers.Add(middle);
                Renderer high = CreateAuraShell(auraObject.transform, "Shadow Mist High",
                    new Vector3(1.55f, 1.86f, 1.55f) * shadowScale, shadowMaterial);
                high.transform.localRotation = Quaternion.Euler(0f, -29f, 0f);
                auraRenderers.Add(high);
            }

            SpecialOreAura aura = root.AddComponent<SpecialOreAura>();
            var serialized = new SerializedObject(aura);
            serialized.FindProperty("data").objectReferenceValue = dayNightData;
            serialized.FindProperty("theme").enumValueIndex = (int)spec.Theme;
            serialized.FindProperty("auraRoot").objectReferenceValue = auraObject.transform;
            serialized.FindProperty("auraLight").objectReferenceValue = auraLight;
            serialized.FindProperty("auraParticles").objectReferenceValue = null;
            SerializedProperty renderersProperty = serialized.FindProperty("auraRenderers");
            renderersProperty.arraySize = auraRenderers.Count;
            for (int i = 0; i < auraRenderers.Count; i++)
            {
                renderersProperty.GetArrayElementAtIndex(i).objectReferenceValue = auraRenderers[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Renderer CreateAuraShell(
            Transform parent, string name, Vector3 scale, Material material)
        {
            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = name;
            shell.transform.SetParent(parent, false);
            shell.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            shell.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(shell.GetComponent<Collider>());
            Renderer renderer = shell.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Material GetOrCreateAuraMaterial(
            string name, string shaderName, Color innerColor, Color outerColor,
            float opacity, float flowSpeed)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Missing shader: {shaderName}");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_AuraColor", innerColor);
            material.SetColor("_OuterColor", outerColor);
            material.SetFloat("_Opacity", opacity);
            if (material.HasProperty("_FlowSpeed")) material.SetFloat("_FlowSpeed", flowSpeed);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddHealthBar(GameObject root, Ore ore)
        {
            GameObject healthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath);
            if (healthBarPrefab == null)
            {
                Debug.LogWarning("MicroBar health bar prefab was not found. Special ores still remain mineable.");
                return;
            }

            GameObject barObject = PrefabUtility.InstantiatePrefab(healthBarPrefab, root.transform) as GameObject;
            if (barObject == null)
            {
                return;
            }

            barObject.name = "Ore Health Bar";
            barObject.transform.localPosition = Vector3.zero;
            MicroBar bar = barObject.GetComponent<MicroBar>();
            OreHealthBar binding = barObject.AddComponent<OreHealthBar>();
            binding.Configure(ore, bar, barObject.transform);
        }

        private static void SetupActiveScene(DayNightData data)
        {
            Scene scene = SceneManager.GetActiveScene();
            DayNightSystem system = UnityEngine.Object.FindFirstObjectByType<DayNightSystem>(
                FindObjectsInactive.Include);
            if (system == null)
            {
                GameObject systemObject = new("Day Night System");
                SceneManager.MoveGameObjectToScene(systemObject, scene);
                Undo.RegisterCreatedObjectUndo(systemObject, "Create Day Night System");
                system = systemObject.AddComponent<DayNightSystem>();
            }

            Light sun = null;
            foreach (Light candidate in UnityEngine.Object.FindObjectsByType<Light>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.type == LightType.Directional)
                {
                    sun = candidate;
                    break;
                }
            }

            Undo.RecordObject(system, "Configure Day Night System");
            var systemSerialized = new SerializedObject(system);
            systemSerialized.FindProperty("data").objectReferenceValue = data;
            systemSerialized.FindProperty("sun").objectReferenceValue = sun;
            systemSerialized.ApplyModifiedProperties();

            OreSpawner spawner = UnityEngine.Object.FindFirstObjectByType<OreSpawner>(
                FindObjectsInactive.Include);
            if (spawner != null)
            {
                Undo.RecordObject(spawner, "Connect Day Night Ore Spawning");
                var spawnerSerialized = new SerializedObject(spawner);
                spawnerSerialized.FindProperty("dayNightSystem").objectReferenceValue = system;
                spawnerSerialized.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("No OreSpawner exists in the active scene. Add one, then run this setup again.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = system.gameObject;
        }

        private static void SetReferenceIfMissing(
            UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null || value == null)
            {
                return;
            }

            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        private static void SetReference(
            UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null || value == null)
            {
                return;
            }

            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && property.objectReferenceValue != value)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent))
            {
                throw new InvalidOperationException($"Invalid Unity folder path: {path}");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
