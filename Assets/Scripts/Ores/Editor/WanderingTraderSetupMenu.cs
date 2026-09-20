using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Creates the editable, scene-owned trader settings object without changing UI layout.</summary>
    public static class WanderingTraderSetupMenu
    {
        private const string MenuPath = "Mining Simulator/Setup/Create Or Select Wandering Trader System";

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

            Selection.activeGameObject = system.gameObject;
            EditorGUIUtility.PingObject(system.gameObject);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateCreateOrSelect()
        {
            return !Application.isPlaying;
        }
    }
}
