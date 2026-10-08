using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MiningUpgradeSceneAuthoring
{
    [MenuItem("Mining Simulator/Setup/Make SVG Upgrades Scene Editable")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogWarning("Stop Play Mode before authoring upgrade UI."); return; }
        var station = Object.FindFirstObjectByType<MiningUpgradeStation>(FindObjectsInactive.Include);
        var owner = Object.FindFirstObjectByType<MiningUpgradePanel>(FindObjectsInactive.Include);
        var system = Object.FindFirstObjectByType<MiningUpgradeSystem>(FindObjectsInactive.Include);
        var wallet = Object.FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
        var coordinator = Object.FindFirstObjectByType<MiningUiPanelCoordinator>(FindObjectsInactive.Include);
        if (station == null || owner == null || system == null) { Debug.LogError("Upgrade station/presenter/system missing."); return; }
        var serialized = new SerializedObject(owner);
        var panel = serialized.FindProperty("upgradePanel").objectReferenceValue as GameObject;
        if (panel == null)
        {
            var item = Object.FindFirstObjectByType<JuicyUpgradeItem>(FindObjectsInactive.Include);
            if (item != null) panel = item.transform.parent.gameObject;
        }
        if (panel == null) { Debug.LogError("Existing Upgrade Panel is missing."); return; }
        Undo.RegisterFullObjectHierarchyUndo(station.gameObject, "Author SVG upgrade view");
        Undo.RegisterFullObjectHierarchyUndo(panel, "Author SVG upgrade cards");
        Undo.RecordObject(owner, "Bind upgrade panel");
        serialized.FindProperty("upgradePanel").objectReferenceValue = panel;
        serialized.ApplyModifiedProperties();
        var carousel = panel.GetComponent<MiningUpgradeCarousel>();
        if (carousel == null) carousel = Undo.AddComponent<MiningUpgradeCarousel>(panel);
        carousel.Initialize(system, wallet, station.UpgradeOrder, station.Close);
        station.Initialize(owner, (RectTransform)panel.transform, coordinator);
        station.ShowAuthoringPreview();
        foreach (var item in carousel.GetComponentsInChildren<JuicyUpgradeItem>(true)) item.Refresh();
        EditorUtility.SetDirty(station); EditorUtility.SetDirty(carousel);
        EditorSceneManager.MarkSceneDirty(station.gameObject.scene);
        Selection.activeGameObject = panel;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log("SVG upgrade UI is now authored in the Scene. Save your Scene to keep it. Existing view edits are preserved in Play Mode.");
    }
}
