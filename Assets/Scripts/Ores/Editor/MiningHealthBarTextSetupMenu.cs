#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>Adds one reusable TextMeshPro health readout to the Ore/Lucky MicroBar prefab.</summary>
    public static class MiningHealthBarTextSetupMenu
    {
        private const string HealthBarPrefabPath =
            "Assets/Microlight/MicroBar/Prefabs/SimpleBars/Sprite_SimpleMicroBarSRP.prefab";
        private const string HealthTextName = "Health Value";

        [MenuItem("Mining Simulator/Setup/Add Health Numbers To Ore And Lucky Bars")]
        public static void AddHealthNumbers()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Health bar prefab is missing at {HealthBarPrefabPath}.");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(HealthBarPrefabPath);
            try
            {
                Transform existing = contents.transform.Find(HealthTextName);
                if (existing != null && existing.GetComponent<TextMeshPro>() != null)
                {
                    Debug.Log("Ore and Lucky Block health numbers are already installed.");
                    return;
                }

                if (existing != null)
                {
                    Debug.LogError($"'{HealthTextName}' already exists in the health bar prefab " +
                                   "but is not a TextMeshPro object. Rename it, then run this menu again.");
                    return;
                }

                GameObject textObject = new(HealthTextName, typeof(TextMeshPro));
                textObject.layer = contents.layer;
                textObject.transform.SetParent(contents.transform, false);

                TextMeshPro label = textObject.GetComponent<TextMeshPro>();
                label.text = "100 / 100";
                label.alignment = TextAlignmentOptions.Center;
                label.fontStyle = FontStyles.Bold;
                label.color = Color.white;
                label.outlineColor = new Color32(15, 18, 24, 255);
                label.outlineWidth = 0.25f;
                label.enableWordWrapping = false;
                label.enableAutoSizing = true;
                label.fontSizeMin = 0.35f;
                label.fontSizeMax = 1.5f;
                label.overflowMode = TextOverflowModes.Overflow;

                RectTransform rect = label.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition3D = new Vector3(0f, 0f, -0.03f);
                rect.sizeDelta = new Vector2(1.85f, 0.24f);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;

                Renderer textRenderer = label.GetComponent<Renderer>();
                if (textRenderer != null)
                {
                    textRenderer.sortingOrder = 120;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, HealthBarPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Added centered compact HP numbers to Ore and Lucky Block health bars.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
#endif
