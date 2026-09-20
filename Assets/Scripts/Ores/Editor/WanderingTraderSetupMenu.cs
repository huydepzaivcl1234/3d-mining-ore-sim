using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Creates the editable, scene-owned trader settings object without changing UI layout.</summary>
    public static class WanderingTraderSetupMenu
    {
        private const string MenuPath = "Mining Simulator/Setup/Create Or Select Wandering Trader In Scene";

        [MenuItem(MenuPath)]
        private static void CreateOrSelect()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Exit Play Mode before creating the permanent Wandering Trader System.");
                return;
            }

            WanderingTraderSystem system = Object.FindFirstObjectByType<WanderingTraderSystem>(
                FindObjectsInactive.Include);
            if (system == null)
            {
                GameObject systemObject = new GameObject("Wandering Trader System");
                Undo.RegisterCreatedObjectUndo(systemObject, "Create Wandering Trader System");
                system = Undo.AddComponent<WanderingTraderSystem>(systemObject);
                NpcShop shop = Object.FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
                system.Configure(shop);
                EditorUtility.SetDirty(system);
                EditorSceneManager.MarkSceneDirty(systemObject.scene);
            }

            WanderingTraderAgent trader = Object.FindFirstObjectByType<WanderingTraderAgent>(
                FindObjectsInactive.Include);
            if (trader == null && system.TraderModelPrefab != null)
            {
                GameObject traderObject = (GameObject)PrefabUtility.InstantiatePrefab(system.TraderModelPrefab);
                traderObject.name = "Wandering Trader";
                Undo.RegisterCreatedObjectUndo(traderObject, "Create Wandering Trader");
                trader = traderObject.GetComponent<WanderingTraderAgent>() ??
                    Undo.AddComponent<WanderingTraderAgent>(traderObject);
                if (traderObject.GetComponent<Collider>() == null)
                {
                    CapsuleCollider collider = Undo.AddComponent<CapsuleCollider>(traderObject);
                    collider.center = new Vector3(0f, 0.9f, 0f);
                    collider.height = 1.8f;
                    collider.radius = 0.35f;
                }
            }
            if (trader != null)
            {
                system.AssignSceneTrader(trader);
                EditorUtility.SetDirty(system);
                EditorSceneManager.MarkSceneDirty(system.gameObject.scene);
            }

            Selection.activeGameObject = trader != null ? trader.gameObject : system.gameObject;
            EditorGUIUtility.PingObject(Selection.activeGameObject);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateCreateOrSelect()
        {
            return !Application.isPlaying;
        }
    }
}
