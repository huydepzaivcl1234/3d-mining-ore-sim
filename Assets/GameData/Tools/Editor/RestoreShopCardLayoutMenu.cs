#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>One-time repair for the authored Shop Card layout.</summary>
    public static class RestoreShopCardLayoutMenu
    {
        [MenuItem("Mining Simulator/Fix/Restore Shop Card (1920x1080)")]
        private static void Restore()
        {
            RectTransform shopCard = FindShopCard();
            if (shopCard == null)
            {
                Debug.LogError("Could not find Shop Panel/Shop Card in the open scene.");
                return;
            }

            Undo.RecordObject(shopCard, "Restore Shop Card Layout");
            shopCard.anchorMin = new Vector2(0.5f, 0.5f);
            shopCard.anchorMax = new Vector2(0.5f, 0.5f);
            shopCard.pivot = new Vector2(0.5f, 0.5f);
            shopCard.anchoredPosition = Vector2.zero;
            shopCard.sizeDelta = new Vector2(1920f, 1080f);

            EditorUtility.SetDirty(shopCard);
            EditorSceneManager.MarkSceneDirty(shopCard.gameObject.scene);
            Selection.activeGameObject = shopCard.gameObject;
            Debug.Log("Restored Shop Card to centered 1920x1080. Save the scene to keep it.", shopCard);
        }

        private static RectTransform FindShopCard()
        {
            foreach (RectTransform candidate in Object.FindObjectsByType<RectTransform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == "Shop Card" && candidate.parent != null &&
                    candidate.parent.name == "Shop Panel")
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
#endif
