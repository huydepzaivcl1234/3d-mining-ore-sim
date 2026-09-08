#if UNITY_EDITOR
using System.Collections.Generic;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>One safe Inspector surface for every existing mining system and data asset.</summary>
    [CustomEditor(typeof(MiningGameManager))]
    public sealed class MiningGameManagerEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, bool> systemFoldouts = new();
        private readonly Dictionary<string, bool> dataFoldouts = new();
        private bool showGameData = true;
        private bool showSystemSettings;

        private UnityEditor.Editor sharedDataEditor;
        private UnityEditor.Editor npcDataEditor;
        private UnityEditor.Editor spawnDataEditor;
        private UnityEditor.Editor upgradeDataEditor;
        private UnityEditor.Editor panelUiDataEditor;
        private UnityEditor.Editor rewardUiDataEditor;
        private UnityEditor.Editor audioDataEditor;
        private UnityEditor.Editor rebirthDataEditor;
        private UnityEditor.Editor drillDataEditor;

        private UnityEditor.Editor walletEditor;
        private UnityEditor.Editor spawnerEditor;
        private UnityEditor.Editor npcShopEditor;
        private UnityEditor.Editor upgradeSystemEditor;
        private UnityEditor.Editor rebirthSystemEditor;
        private UnityEditor.Editor drillStationEditor;
        private UnityEditor.Editor hudEditor;
        private UnityEditor.Editor upgradePanelEditor;
        private UnityEditor.Editor rebirthPanelEditor;
        private UnityEditor.Editor audioManagerEditor;
        private UnityEditor.Editor audioSettingsEditor;
        private UnityEditor.Editor coordinatorEditor;
        private UnityEditor.Editor cameraEditor;

        public override void OnInspectorGUI()
        {
            MiningGameManager manager = target as MiningGameManager;
            if (manager == null)
            {
                return;
            }

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            DrawDrillReferenceFinder(manager);

            DrawMoneyTesting(manager);
            EditorGUILayout.Space(8f);

            showGameData = EditorGUILayout.BeginFoldoutHeaderGroup(showGameData,
                "Unified Game Data");
            if (showGameData)
            {
                EditorGUILayout.HelpBox(
                    "Edit all balancing data here. Values remain owned by their existing ScriptableObjects, so no project architecture or references are duplicated.",
                    MessageType.Info);
                DrawDataAsset("Shared Game Data", manager.Wallet, "gameData", ref sharedDataEditor);
                DrawDataAsset("NPC Data", manager.NpcShop, "npcData", ref npcDataEditor);
                DrawDataAsset("Ore Spawn Data", manager.OreSpawner, "spawnData", ref spawnDataEditor);
                DrawDataAsset("Upgrade Data", manager.UpgradeSystem, "upgradeData", ref upgradeDataEditor);
                DrawDataAsset("Panel UI Data", manager.PanelCoordinator, "uiData", ref panelUiDataEditor);
                DrawDataAsset("Ore Reward UI Data", manager.OreSpawner, "uiData", ref rewardUiDataEditor);
                DrawDataAsset("Audio Data", manager.AudioManager, "audioData", ref audioDataEditor);
                DrawDataAsset("Rebirth Data", manager.RebirthSystem, "rebirthData", ref rebirthDataEditor);
                DrawDataAsset("Drill Data", manager.DrillStation, "drillData", ref drillDataEditor);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6f);
            showSystemSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSystemSettings,
                "All System Components");
            if (showSystemSettings)
            {
                EditorGUILayout.HelpBox(
                    "These are the same existing components, shown inline so you do not need to select different GameObjects.",
                    MessageType.None);
                DrawComponent("Wallet", manager.Wallet, ref walletEditor);
                DrawComponent("Ore Spawner", manager.OreSpawner, ref spawnerEditor);
                DrawComponent("NPC Shop", manager.NpcShop, ref npcShopEditor);
                DrawComponent("Upgrade System", manager.UpgradeSystem, ref upgradeSystemEditor);
                DrawComponent("Rebirth System", manager.RebirthSystem, ref rebirthSystemEditor);
                DrawComponent("Drill Station", manager.DrillStation, ref drillStationEditor);
                DrawComponent("HUD", manager.Hud, ref hudEditor);
                DrawComponent("Upgrade Panel", manager.UpgradePanel, ref upgradePanelEditor);
                DrawComponent("Rebirth Panel", manager.RebirthPanel, ref rebirthPanelEditor);
                DrawComponent("Audio Manager", manager.AudioManager, ref audioManagerEditor);
                DrawComponent("Audio Settings Panel", manager.AudioSettingsPanel,
                    ref audioSettingsEditor);
                DrawComponent("Panel Coordinator", manager.PanelCoordinator, ref coordinatorEditor);
                DrawComponent("Orbit Camera", manager.OrbitCamera, ref cameraEditor);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private static void DrawMoneyTesting(MiningGameManager manager)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Runtime Money Test", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to use the money buttons. Test Money Amount remains editable above.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(manager.Wallet == null))
            {
                if (GUILayout.Button("Set Money To Test Amount")) manager.SetTestMoney();
                if (GUILayout.Button("Add Test Amount")) manager.AddTestMoney();
                if (GUILayout.Button("Reset Money To 0")) manager.ResetTestMoney();
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
        }

        private void DrawDataAsset(string label, Component owner, string fieldName,
            ref UnityEditor.Editor cachedEditor)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool expanded = dataFoldouts.TryGetValue(label, out bool current) && current;
            expanded = EditorGUILayout.Foldout(expanded, label, true);
            dataFoldouts[label] = expanded;
            if (!expanded)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            if (owner == null)
            {
                DestroyCachedEditor(ref cachedEditor);
                EditorGUILayout.HelpBox($"Assign the related component in MiningGameManager to edit {label}.",
                    MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            SerializedObject ownerSerialized = new(owner);
            ownerSerialized.Update();
            SerializedProperty dataProperty = ownerSerialized.FindProperty(fieldName);
            if (dataProperty == null)
            {
                DestroyCachedEditor(ref cachedEditor);
                EditorGUILayout.HelpBox($"Could not find '{fieldName}' on {owner.GetType().Name}.",
                    MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(dataProperty, new GUIContent("Data Asset"));
            if (EditorGUI.EndChangeCheck())
            {
                ownerSerialized.ApplyModifiedProperties();
            }

            Object dataAsset = dataProperty.objectReferenceValue;
            if (dataAsset != null)
            {
                UnityEditor.Editor.CreateCachedEditor(dataAsset, null, ref cachedEditor);
                EditorGUI.indentLevel++;
                cachedEditor?.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
            else
            {
                DestroyCachedEditor(ref cachedEditor);
                EditorGUILayout.HelpBox("No data asset assigned.", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawDrillReferenceFinder(MiningGameManager manager)
        {
            if (manager.DrillStation != null)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "Drill Station is not assigned. You can assign it above or safely find the active station in the current scene.",
                MessageType.Info);
            if (!GUILayout.Button("Find Missing Drill Station In Scene"))
            {
                return;
            }

            MiningDrillStation station = Object.FindFirstObjectByType<MiningDrillStation>();
            if (station == null)
            {
                Debug.LogWarning("No active MiningDrillStation was found in the current scene.", manager);
                return;
            }

            Undo.RecordObject(manager, "Assign Mining Drill Station");
            serializedObject.Update();
            serializedObject.FindProperty("drillStation").objectReferenceValue = station;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);
        }

        private void DrawComponent(string label, Component component,
            ref UnityEditor.Editor cachedEditor)
        {
            bool expanded = systemFoldouts.TryGetValue(label, out bool current) && current;
            expanded = EditorGUILayout.Foldout(expanded, label, true);
            systemFoldouts[label] = expanded;
            if (!expanded)
            {
                return;
            }

            if (component == null)
            {
                DestroyCachedEditor(ref cachedEditor);
                EditorGUILayout.HelpBox($"{label} is not assigned in MiningGameManager.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            UnityEditor.Editor.CreateCachedEditor(component, null, ref cachedEditor);
            cachedEditor?.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }

        private void OnDisable()
        {
            DestroyCachedEditor(ref sharedDataEditor);
            DestroyCachedEditor(ref npcDataEditor);
            DestroyCachedEditor(ref spawnDataEditor);
            DestroyCachedEditor(ref upgradeDataEditor);
            DestroyCachedEditor(ref panelUiDataEditor);
            DestroyCachedEditor(ref rewardUiDataEditor);
            DestroyCachedEditor(ref audioDataEditor);
            DestroyCachedEditor(ref rebirthDataEditor);
            DestroyCachedEditor(ref drillDataEditor);
            DestroyCachedEditor(ref walletEditor);
            DestroyCachedEditor(ref spawnerEditor);
            DestroyCachedEditor(ref npcShopEditor);
            DestroyCachedEditor(ref upgradeSystemEditor);
            DestroyCachedEditor(ref rebirthSystemEditor);
            DestroyCachedEditor(ref drillStationEditor);
            DestroyCachedEditor(ref hudEditor);
            DestroyCachedEditor(ref upgradePanelEditor);
            DestroyCachedEditor(ref rebirthPanelEditor);
            DestroyCachedEditor(ref audioManagerEditor);
            DestroyCachedEditor(ref audioSettingsEditor);
            DestroyCachedEditor(ref coordinatorEditor);
            DestroyCachedEditor(ref cameraEditor);
        }

        private static void DestroyCachedEditor(ref UnityEditor.Editor cachedEditor)
        {
            if (cachedEditor != null)
            {
                Object.DestroyImmediate(cachedEditor);
                cachedEditor = null;
            }
        }
    }
}
#endif
