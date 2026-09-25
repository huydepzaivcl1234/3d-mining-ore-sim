#if UNITY_EDITOR
using MiningSimulator.Ores;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Editor
{
    /// <summary>Configures the two existing chest prefabs and an editable scene spawner.</summary>
    public static class MiningChestSetupMenu
    {
        private const string WoodPath = "Assets/Prefabs/Ores/Chest/ChestV1.prefab";
        private const string ItemPath = "Assets/Prefabs/Ores/Chest/ChestV2.prefab";
        private const string DatabasePath = "Assets/GameData/Items/MiningItemDatabase.asset";
        private const string HealthBarPath = "Assets/Microlight/MicroBar/Prefabs/SimpleBars/Sprite_SimpleMicroBarSRP.prefab";
        private const string MoneyPopupPath = "Assets/Prefabs/UI/OreRewardPopup.prefab";
        private const string UiDataPath = "Assets/GameData/UI/MiningUiData.asset";

        [MenuItem("Mining Simulator/Tools/Setup Independent Chests")]
        private static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Chest setup", "Stop Play Mode first.", "OK");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WoodPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(ItemPath) == null)
            {
                EditorUtility.DisplayDialog("Chest setup", "ChestV1 or ChestV2 is missing under Assets/Prefabs/Ores/Chest. No assets changed.", "OK");
                return;
            }

            MiningChest wood = ConfigurePrefab(WoodPath, "ChestV1", MiningChest.ChestKind.WoodMoney);
            MiningChest item = ConfigurePrefab(ItemPath, "ChestV2", MiningChest.ChestKind.ItemRoll);
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene))
            {
                Debug.LogWarning("Open the gameplay scene to add the editable Chest System spawner.");
                return;
            }

            GameObject system = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Chest System") { system = root; break; }
            if (system == null)
            {
                system = new GameObject("Chest System");
                Undo.RegisterCreatedObjectUndo(system, "Create independent chest system");
                SceneManager.MoveGameObjectToScene(system, scene);
            }
            MiningChestSpawner spawner = system.GetComponent<MiningChestSpawner>();
            if (spawner == null) spawner = Undo.AddComponent<MiningChestSpawner>(system);
            var fields = new SerializedObject(spawner);
            AssignIfEmpty(fields, "wallet", FindInScene<PlayerWallet>(scene));
            AssignIfEmpty(fields, "itemSystem", FindInScene<MiningItemSystem>(scene));
            SerializedProperty chests = fields.FindProperty("chests");
            if (chests != null && chests.arraySize == 0)
            {
                chests.arraySize = 2;
                chests.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue = wood;
                chests.GetArrayElementAtIndex(0).FindPropertyRelative("weight").floatValue = 1f;
                chests.GetArrayElementAtIndex(1).FindPropertyRelative("prefab").objectReferenceValue = item;
                chests.GetArrayElementAtIndex(1).FindPropertyRelative("weight").floatValue = 1f;
            }
            fields.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = system;
            Debug.Log("Chest System ready. Configure drop chances and area on this scene object; item/quantity weights on the ChestV2 prefab. Save the scene yourself.", system);
        }

        private static MiningChest ConfigurePrefab(string path, string name, MiningChest.ChestKind kind)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                MiningChest chest = root.GetComponent<MiningChest>();
                bool created = chest == null;
                if (created) chest = root.AddComponent<MiningChest>();
                Transform lid = FindChild(root.transform, name + "_Top");
                Transform hinge = root.transform.Find("Chest Lid Hinge");
                if (lid != null && hinge == null)
                {
                    var pivot = new GameObject("Chest Lid Hinge");
                    hinge = pivot.transform;
                    hinge.SetParent(root.transform, false);
                    Renderer lidRenderer = lid.GetComponent<Renderer>();
                    if (lidRenderer != null)
                    {
                        Bounds bounds = lidRenderer.bounds;
                        hinge.localPosition = root.transform.InverseTransformPoint(
                            new Vector3(bounds.center.x, bounds.center.y, bounds.min.z));
                    }
                    else hinge.localPosition = lid.localPosition;
                    lid.SetParent(hinge, true);
                }

                Transform rewardAnchor = root.transform;
                Transform iconTransform = rewardAnchor.Find("Chest Reward Icon");
                if (iconTransform == null) iconTransform = FindChild(root.transform, "Chest Reward Icon");
                if (iconTransform == null)
                {
                    iconTransform = new GameObject("Chest Reward Icon", typeof(SpriteRenderer)).transform;
                    iconTransform.SetParent(rewardAnchor, false);
                    iconTransform.localPosition = new Vector3(0f, 1.5f, 0f);
                    iconTransform.localScale = Vector3.one * .5f;
                    iconTransform.GetComponent<SpriteRenderer>().sortingOrder = 20;
                    iconTransform.gameObject.SetActive(false);
                }

                Transform textTransform = root.transform.Find("Chest Reward Text");
                if (textTransform == null)
                {
                    textTransform = new GameObject("Chest Reward Text", typeof(TextMeshPro)).transform;
                    textTransform.SetParent(root.transform, false);
                    textTransform.localPosition = new Vector3(0f, 1.4f, 0f);
                    textTransform.localScale = Vector3.one * .2f;
                    var text = textTransform.GetComponent<TextMeshPro>();
                    text.text = "+100";
                    text.fontSize = 5f;
                    text.alignment = TextAlignmentOptions.Center;
                    text.color = new Color(1f, .82f, .28f);
                    textTransform.gameObject.SetActive(false);
                }

                Collider collider = root.GetComponent<Collider>();
                if (collider == null) collider = root.AddComponent<BoxCollider>();
                var fields = new SerializedObject(chest);
                if (created) fields.FindProperty("kind").enumValueIndex = (int)kind;
                AssignIfEmpty(fields, "lidHinge", hinge);
                AssignIfEmpty(fields, "lockModel", FindChild(root.transform, name + "_Lock"));
                AssignIfEmpty(fields, "lootModel", FindChild(root.transform, name + "_Loot"));
                AssignIfEmpty(fields, "lidRewardIcon", iconTransform.GetComponent<SpriteRenderer>());
                AssignIfEmpty(fields, "rewardText", textTransform.GetComponent<TextMeshPro>());
                AssignIfEmpty(fields, "hitCollider", collider);
                GameObject barPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPath);
                if (barPrefab != null)
                    AssignIfEmpty(fields, "healthBarPrefab", barPrefab.GetComponent<Microlight.MicroBar.MicroBar>());
                if (kind == MiningChest.ChestKind.WoodMoney)
                {
                    GameObject popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MoneyPopupPath);
                    if (popupPrefab != null)
                        AssignIfEmpty(fields, "moneyPopupPrefab", popupPrefab.GetComponent<OreRewardPopup>());
                    AssignIfEmpty(fields, "moneyPopupUiData", AssetDatabase.LoadAssetAtPath<MiningUiData>(UiDataPath));
                }
                if (kind == MiningChest.ChestKind.ItemRoll) PopulateInitialItemTable(fields);
                fields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<MiningChest>();
        }

        private static void PopulateInitialItemTable(SerializedObject fields)
        {
            SerializedProperty chances = fields.FindProperty("itemChances");
            if (chances == null || chances.arraySize != 0) return;
            MiningItemDatabase database = AssetDatabase.LoadAssetAtPath<MiningItemDatabase>(DatabasePath);
            if (database == null) return;
            foreach (MiningItemData item in database.Items)
            {
                if (item == null) continue;
                int index = chances.arraySize;
                chances.InsertArrayElementAtIndex(index);
                SerializedProperty entry = chances.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("item").objectReferenceValue = item;
                entry.FindPropertyRelative("weight").floatValue = 1f;
            }
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private static void AssignIfEmpty(SerializedObject fields, string name, Object value)
        {
            SerializedProperty property = fields.FindProperty(name);
            if (property != null && property.objectReferenceValue == null && value != null)
                property.objectReferenceValue = value;
        }
    }
}
#endif
