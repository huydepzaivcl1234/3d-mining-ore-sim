#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>
    /// Compact composition-root Inspector. Gameplay values remain owned by their GameData assets.
    /// </summary>
    [CustomEditor(typeof(MiningGameManager))]
    public sealed class MiningGameManagerEditor : UnityEditor.Editor
    {
        private bool showCore = true;
        private bool showPresentation;

        private void OnEnable()
        {
            CollapseSiblingComponents();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Mining Game Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Manager này chỉ nối và điều phối hệ thống runtime. Mọi chỉ số gameplay/UI/audio vẫn chỉnh trong GameData tương ứng.",
                MessageType.Info);

            showCore = EditorGUILayout.BeginFoldoutHeaderGroup(showCore, "Core Runtime References");
            if (showCore)
            {
                DrawReference("wallet", "Wallet");
                DrawReference("oreSpawner", "Ore Spawner");
                DrawReference("npcShop", "NPC Shop");
                DrawReference("upgradeSystem", "Upgrade System");
                DrawReference("rebirthSystem", "Rebirth System");
                DrawReference("drillStation", "Drill Station");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            showPresentation = EditorGUILayout.BeginFoldoutHeaderGroup(
                showPresentation, "Presentation References");
            if (showPresentation)
            {
                DrawReference("hud", "HUD");
                DrawReference("upgradePanel", "Upgrade Panel");
                DrawReference("rebirthPanel", "Rebirth Panel");
                DrawReference("audioManager", "Audio Manager");
                DrawReference("audioSettingsPanel", "Audio Settings Panel");
                DrawReference("panelCoordinator", "Panel Coordinator");
                DrawReference("orbitCamera", "Orbit Camera");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6f);
            DrawConfigurationStatus((MiningGameManager)target);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Play Mode Money Testing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("testMoneyAmount"),
                new GUIContent("Test Money Amount"));

            serializedObject.ApplyModifiedProperties();
            DrawMoneyTesting((MiningGameManager)target);
        }

        private void DrawReference(string propertyName, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
        }

        private static void DrawConfigurationStatus(MiningGameManager manager)
        {
            if (manager.IsConfigured)
            {
                EditorGUILayout.HelpBox("Core configuration is ready.", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(
                "Một hoặc nhiều reference runtime đang thiếu. Mở các nhóm phía trên để gắn đúng object; GameData không được hiển thị tại đây.",
                MessageType.Warning);
        }

        private static void DrawMoneyTesting(MiningGameManager manager)
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Vào Play Mode để dùng nút test tiền.", MessageType.None);
                return;
            }

            using (new EditorGUI.DisabledScope(manager.Wallet == null))
            {
                if (GUILayout.Button("Set Money To Test Amount")) manager.SetTestMoney();
                if (GUILayout.Button("Add Test Amount")) manager.AddTestMoney();
                if (GUILayout.Button("Reset Money To 0")) manager.ResetTestMoney();
            }

            if (manager.Wallet != null)
            {
                EditorGUILayout.LabelField("Current Money",
                    MiningMoneyFormatter.Format(manager.Wallet.CurrentMoney));
            }
        }

        private void CollapseSiblingComponents()
        {
            MiningGameManager manager = target as MiningGameManager;
            if (manager == null)
            {
                return;
            }

            foreach (Component component in manager.GetComponents<Component>())
            {
                if (component == null || component == manager || component is Transform)
                {
                    continue;
                }

                InternalEditorUtility.SetIsInspectorExpanded(component, false);
            }
        }
    }
}
#endif
