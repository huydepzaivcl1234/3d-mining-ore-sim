#if UNITY_EDITOR
using System.IO;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Editor
{
    /// <summary>Creates Lucky Block data and a real editable scene system without a runtime prefab.</summary>
    public static class LuckyBlockSetupMenu
    {
        private const string DataFolder = "Assets/GameData/LuckyBlocks";
        private const string DataPath = DataFolder + "/LuckyBlockData.asset";
        private const string GoldModelPath = "Assets/Ores/Models/gold_lucky_block.fbx";
        private const string DiamondModelPath = "Assets/Ores/Models/diamond_lucky_block.fbx";
        private const string RainbowModelPath = "Assets/Ores/Models/rainbow_lucky_block.fbx";
        private const string UiDataPath = "Assets/GameData/UI/MiningUiData.asset";
        private const string RewardPopupPath = "Assets/Prefabs/UI/OreRewardPopup.prefab";
        private const string HealthBarPrefabPath =
            "Assets/Microlight/MicroBar/Prefabs/SimpleBars/Sprite_SimpleMicroBarSRP.prefab";

        [MenuItem("Mining Simulator/Setup/Create or Update Lucky Blocks")]
        public static void CreateOrUpdateLuckyBlocks()
        {
            EnsureFolder(DataFolder);
            AssetDatabase.Refresh();
            LuckyBlockData data = CreateOrUpdateData();
            SetupActiveScene(data);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Lucky Block setup complete. Edit all drop, size, reward and rarity values in LuckyBlockData.");
        }

        private static LuckyBlockData CreateOrUpdateData()
        {
            LuckyBlockData data = AssetDatabase.LoadAssetAtPath<LuckyBlockData>(DataPath);
            bool isNew = data == null;
            if (isNew)
            {
                data = ScriptableObject.CreateInstance<LuckyBlockData>();
                AssetDatabase.CreateAsset(data, DataPath);
            }

            GameObject goldModel = AssetDatabase.LoadAssetAtPath<GameObject>(GoldModelPath);
            GameObject diamondModel = AssetDatabase.LoadAssetAtPath<GameObject>(DiamondModelPath);
            GameObject rainbowModel = AssetDatabase.LoadAssetAtPath<GameObject>(RainbowModelPath);
            if (goldModel == null || diamondModel == null || rainbowModel == null)
            {
                Debug.LogError("One or more Lucky Block FBX models are missing from Assets/Ores/Models.");
                return data;
            }

            var serialized = new SerializedObject(data);
            SerializedProperty variants = serialized.FindProperty("variants");
            if (isNew || variants.arraySize == 0)
            {
                variants.arraySize = 3;
                ConfigureVariant(variants.GetArrayElementAtIndex(0), LuckyBlockType.Gold,
                    "Gold Lucky Block", goldModel, 70f, 10, 1, 100, 1f, true);
                ConfigureVariant(variants.GetArrayElementAtIndex(1), LuckyBlockType.Diamond,
                    "Diamond Lucky Block", diamondModel, 25f, 20, 2, 500, 1f, true);
                ConfigureVariant(variants.GetArrayElementAtIndex(2), LuckyBlockType.Rainbow,
                    "Rainbow Lucky Block", rainbowModel, 5f, 35, 3, 1500, 1f, true);
            }
            else
            {
                AssignMissingModel(variants, LuckyBlockType.Gold, goldModel);
                AssignMissingModel(variants, LuckyBlockType.Diamond, diamondModel);
                AssignMissingModel(variants, LuckyBlockType.Rainbow, rainbowModel);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void ConfigureVariant(SerializedProperty property, LuckyBlockType type,
            string displayName, GameObject model, float chancePercent, int durability, int clickDamage,
            int reward, float sizeMultiplier, bool overwrite)
        {
            property.FindPropertyRelative("type").enumValueIndex = (int)type;
            if (!overwrite && property.FindPropertyRelative("model").objectReferenceValue != null)
            {
                return;
            }

            property.FindPropertyRelative("displayName").stringValue = displayName;
            property.FindPropertyRelative("model").objectReferenceValue = model;
            property.FindPropertyRelative("selectionChancePercent").floatValue = chancePercent;
            property.FindPropertyRelative("durability").intValue = durability;
            property.FindPropertyRelative("clickDamage").intValue = clickDamage;
            property.FindPropertyRelative("moneyReward").intValue = reward;
            property.FindPropertyRelative("sizeMultiplier").floatValue = sizeMultiplier;
        }

        private static void AssignMissingModel(SerializedProperty variants, LuckyBlockType type,
            GameObject model)
        {
            for (int i = 0; i < variants.arraySize; i++)
            {
                SerializedProperty variant = variants.GetArrayElementAtIndex(i);
                if (variant.FindPropertyRelative("type").enumValueIndex != (int)type)
                {
                    continue;
                }

                SerializedProperty modelProperty = variant.FindPropertyRelative("model");
                if (modelProperty.objectReferenceValue == null)
                {
                    modelProperty.objectReferenceValue = model;
                }
                return;
            }
        }

        private static void SetupActiveScene(LuckyBlockData data)
        {
            OreSpawner spawner = Object.FindFirstObjectByType<OreSpawner>(FindObjectsInactive.Include);
            PlayerWallet wallet = Object.FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            MiningUpgradeSystem upgradeSystem = Object.FindFirstObjectByType<MiningUpgradeSystem>(
                FindObjectsInactive.Include);
            if (spawner == null || wallet == null)
            {
                Debug.LogError("Lucky Block setup requires the existing OreSpawner and PlayerWallet in the active scene.");
                return;
            }

            LuckyBlockDropSystem system = Object.FindFirstObjectByType<LuckyBlockDropSystem>(
                FindObjectsInactive.Include);
            if (system == null)
            {
                Transform root = wallet.transform.root;
                GameObject systemObject = new("Lucky Block System");
                SceneManager.MoveGameObjectToScene(systemObject, SceneManager.GetActiveScene());
                systemObject.transform.SetParent(root, false);
                Undo.RegisterCreatedObjectUndo(systemObject, "Create Lucky Block System");
                system = systemObject.AddComponent<LuckyBlockDropSystem>();
            }

            Undo.RecordObject(system, "Configure Lucky Block System");
            var serialized = new SerializedObject(system);
            SetReference(serialized, "data", data);
            SetReference(serialized, "wallet", wallet);
            SetReferenceIfMissing(serialized, "upgradeSystem", upgradeSystem);
            SetReference(serialized, "oreSpawnData", spawner.SpawnData);
            SetReference(serialized, "spawnAreaOrigin", spawner.transform);
            SetReferenceIfMissing(serialized, "droppedBlockParent", system.transform);
            SetReferenceIfMissing(serialized, "uiData",
                AssetDatabase.LoadAssetAtPath<MiningUiData>(UiDataPath));
            GameObject popupObject = AssetDatabase.LoadAssetAtPath<GameObject>(RewardPopupPath);
            SetReferenceIfMissing(serialized, "rewardPopupPrefab",
                popupObject != null ? popupObject.GetComponent<OreRewardPopup>() : null);
            SetReferenceIfMissing(serialized, "healthBarPrefab",
                AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath));
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(system);
            EditorSceneManager.MarkSceneDirty(system.gameObject.scene);
            Selection.activeGameObject = system.gameObject;
        }

        private static void SetReference(SerializedObject serialized, string name, Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetReferenceIfMissing(SerializedObject serialized, string name,
            Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null && property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
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
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
