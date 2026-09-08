#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Editor
{
    [CustomEditor(typeof(MiningGameManager))]
    public sealed class MiningGameManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Runtime Money Test", EditorStyles.boldLabel);

            MiningGameManager manager = (MiningGameManager)target;
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to use the money test buttons. The configured amount remains editable above.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(manager.Wallet == null))
            {
                if (GUILayout.Button("Set Money To Test Amount"))
                {
                    manager.SetTestMoney();
                }

                if (GUILayout.Button("Add Test Amount"))
                {
                    manager.AddTestMoney();
                }

                if (GUILayout.Button("Reset Money To 0"))
                {
                    manager.ResetTestMoney();
                }
            }

            if (manager.Wallet == null)
            {
                EditorGUILayout.HelpBox("MiningGameManager has no PlayerWallet reference.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Current Money",
                    MiningMoneyFormatter.Format(manager.Wallet.CurrentMoney));
            }

            if (GUI.changed)
            {
                Repaint();
            }
        }
    }
}
#endif
