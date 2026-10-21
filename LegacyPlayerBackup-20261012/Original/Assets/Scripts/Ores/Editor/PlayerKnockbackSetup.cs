#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class PlayerKnockbackSetup
{
    [MenuItem("Mining Simulator/Setup/Player Death Ragdoll")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play Mode first.");
        var player = GameObject.Find("Player");
        if (player == null) throw new System.InvalidOperationException("Player missing.");
        if (player.GetComponent<PlayerKnockbackRagdoll>() == null) Undo.AddComponent<PlayerKnockbackRagdoll>(player);
        EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log("Death ragdoll configured; no Death/Get Up animation is required. Save when ready.", player);
    }
}
#endif
