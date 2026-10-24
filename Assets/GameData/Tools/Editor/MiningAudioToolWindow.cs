#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>Editor window for wiring and editing mining music/SFX.</summary>
    public sealed class MiningAudioToolWindow : EditorWindow
    {
        private const string AudioDataPath = "Assets/GameData/Audio/MiningAudioData.asset";
        private const string RuntimePrefabPath = "Assets/Prefabs/Systems/MiningRuntime.prefab";

        private MiningAudioData audioData;
        private UnityEditor.Editor audioDataEditor;
        private Vector2 scrollPosition;

        [MenuItem("Mining Simulator/Tools/Audio Manager")]
        public static void Open()
        {
            GetWindow<MiningAudioToolWindow>("Mining Audio");
        }

        private void OnEnable()
        {
            LoadAudioData();
        }

        private void OnDisable()
        {
            ReleaseAudioDataEditor();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("MINING AUDIO MANAGER", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Kéo nhạc nền và các SFX vào từng ô. Volume, pitch, loop và giới hạn âm thanh đào đều chỉnh tại đây.",
                MessageType.Info);

            if (audioData == null)
            {
                EditorGUILayout.HelpBox("Chưa có MiningAudioData. Hãy tạo dữ liệu âm thanh bên dưới.",
                    MessageType.Warning);
            }
            else
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                if (audioDataEditor == null)
                {
                    audioDataEditor = UnityEditor.Editor.CreateEditor(audioData);
                }

                if (audioDataEditor != null)
                {
                    audioDataEditor.OnInspectorGUI();
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Chọn MiningAudioData trong Project"))
                {
                    Selection.activeObject = audioData;
                    EditorGUIUtility.PingObject(audioData);
                }
            }

            EditorGUILayout.Space();
            if (audioData == null && GUILayout.Button("Tạo MiningAudioData"))
            {
                CreateAudioDataIfMissing();
            }

            if (GUILayout.Button("Mở MiningRuntime Prefab"))
            {
                GameObject runtimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimePrefabPath);
                if (runtimePrefab != null)
                {
                    AssetDatabase.OpenAsset(runtimePrefab);
                }
            }
        }

        private void LoadAudioData()
        {
            ReleaseAudioDataEditor();
            audioData = AssetDatabase.LoadAssetAtPath<MiningAudioData>(AudioDataPath);
            Repaint();
        }

        private void CreateAudioDataIfMissing()
        {
            MiningAudioData existing = AssetDatabase.LoadAssetAtPath<MiningAudioData>(AudioDataPath);
            if (existing == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(AudioDataPath) != null)
                {
                    Debug.LogError($"Cannot create MiningAudioData: another asset occupies {AudioDataPath}.");
                    return;
                }
                const string gameDataFolder = "Assets/GameData";
                const string audioFolder = "Assets/GameData/Audio";
                if (!AssetDatabase.IsValidFolder(gameDataFolder))
                    AssetDatabase.CreateFolder("Assets", "GameData");
                if (!AssetDatabase.IsValidFolder(audioFolder))
                    AssetDatabase.CreateFolder(gameDataFolder, "Audio");
                existing = CreateInstance<MiningAudioData>();
                AssetDatabase.CreateAsset(existing, AudioDataPath);
                AssetDatabase.SaveAssets();
            }

            LoadAudioData();
            Selection.activeObject = audioData;
            EditorGUIUtility.PingObject(audioData);
        }

        private void ReleaseAudioDataEditor()
        {
            if (audioDataEditor != null)
            {
                DestroyImmediate(audioDataEditor);
            }

            // Unity objects can compare equal to null after destruction while their managed
            // wrapper is still non-null. Clear it explicitly so the next GUI pass rebuilds it.
            audioDataEditor = null;
        }
    }
}
#endif
