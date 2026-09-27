using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    public static class PlayerActionFeedbackSetup
    {
        [MenuItem("Mining Simulator/Setup/Configure Player Jump Attack Feedback")]
        public static void Configure()
        {
            var data = AssetDatabase.LoadAssetAtPath<MiningPlayerStatsData>("Assets/GameData/Player/PlayerStatsData.asset");
            var whoosh = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameData/Audio/whoosh.mp3");
            if (data == null || whoosh == null)
            {
                Debug.LogError("PlayerStatsData or existing whoosh.mp3 is missing. Assign Jump Sfx / Attack Sfx on your PlayerStatsData asset.");
                return;
            }
            Undo.RecordObject(data, "Configure player action feedback");
            if (data.jumpSfx == null) data.jumpSfx = whoosh;
            if (data.attackSfx == null) data.attackSfx = whoosh;
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
        }
    }
}
