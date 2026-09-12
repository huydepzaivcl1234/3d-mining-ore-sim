#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>Adds an assignable SpriteRenderer slot beside the existing reward popup text.</summary>
    public static class MiningRewardPopupIconSetupMenu
    {
        private const string PopupPrefabPath = "Assets/Prefabs/UI/OreRewardPopup.prefab";
        private const string UiDataPath = "Assets/GameData/UI/MiningUiData.asset";
        private const string IconObjectName = "Coin Icon";

        [MenuItem("Mining Simulator/Setup/Add Coin Icon Slot To Reward Popup")]
        public static void AddCoinIconSlot()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
            MiningUiData uiData = AssetDatabase.LoadAssetAtPath<MiningUiData>(UiDataPath);
            if (prefab == null || uiData == null)
            {
                Debug.LogError("Reward popup prefab or MiningUiData is missing.");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(PopupPrefabPath);
            try
            {
                OreRewardPopup popup = contents.GetComponent<OreRewardPopup>();
                if (popup == null)
                {
                    Debug.LogError("OreRewardPopup component is missing from the reward popup prefab.");
                    return;
                }

                Transform iconTransform = contents.transform.Find(IconObjectName);
                SpriteRenderer iconRenderer;
                bool created = iconTransform == null;
                if (created)
                {
                    GameObject iconObject = new(IconObjectName, typeof(SpriteRenderer));
                    iconObject.layer = contents.layer;
                    iconObject.transform.SetParent(contents.transform, false);
                    iconTransform = iconObject.transform;
                    iconRenderer = iconObject.GetComponent<SpriteRenderer>();
                    iconTransform.localPosition = uiData.RewardPopupIconLocalPosition;
                    iconTransform.localRotation = Quaternion.identity;
                    iconTransform.localScale = Vector3.one * uiData.RewardPopupIconScale;
                    iconRenderer.color = uiData.RewardPopupIconColor;
                    iconRenderer.sortingOrder = uiData.CanvasSortingOrder + 1;
                }
                else
                {
                    iconRenderer = iconTransform.GetComponent<SpriteRenderer>();
                    if (iconRenderer == null)
                    {
                        Debug.LogError($"'{IconObjectName}' exists but has no SpriteRenderer. " +
                                       "Rename that object, then run this menu again.");
                        return;
                    }
                }

                var serializedPopup = new SerializedObject(popup);
                SerializedProperty iconProperty = serializedPopup.FindProperty("coinIcon");
                if (iconProperty != null)
                {
                    iconProperty.objectReferenceValue = iconRenderer;
                    serializedPopup.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(contents, PopupPrefabPath);
                AssetDatabase.SaveAssets();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
                Debug.Log(created
                    ? "Coin Icon slot added. Open OreRewardPopup prefab and assign your coin Sprite."
                    : "Coin Icon reference repaired. Your assigned Sprite and transform were preserved.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
#endif
