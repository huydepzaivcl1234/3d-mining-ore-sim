using UnityEditor;
using UnityEngine;

// One-time scene migration for patch imports; never saves a designer's dirty scene.
public static class PlayerSlashCleanup
{
    [MenuItem("Mining Simulator/Cleanup/Remove obsolete player slash objects")]
    private static void RemoveObsoleteObjects()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        int removed = 0;
        foreach (var player in Object.FindObjectsByType<PlayerCombatInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            for (int i = player.transform.childCount - 1; i >= 0; i--)
            {
                var child = player.transform.GetChild(i);
                if (!child.name.StartsWith("Slash VFX", System.StringComparison.Ordinal) ||
                    child.GetComponent<ParticleSystem>() == null) continue;
                Undo.DestroyObjectImmediate(child.gameObject);
                removed++;
            }
        Debug.Log($"Removed {removed} obsolete player slash objects. Save the scene when ready; Undo is available.");
    }
}
