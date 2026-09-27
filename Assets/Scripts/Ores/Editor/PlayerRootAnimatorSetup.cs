using System;
using System.Linq;
using StarterAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    public static class PlayerRootAnimatorSetup
    {
        [MenuItem("Mining Simulator/Setup/Move Player Animator To Root")]
        public static void MoveToRoot()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Player Animator", "Stop Play Mode first.", "OK");
                return;
            }
            var selected = Selection.activeGameObject;
            var player = selected ? selected.GetComponentInParent<ThirdPersonController>() : null;
            if (!player || EditorUtility.IsPersistent(player))
            {
                EditorUtility.DisplayDialog("Player Animator", "Select Player or its model in the scene / Prefab Mode first.", "OK");
                return;
            }
            var root = player.GetComponent<Animator>();
            var children = player.GetComponentsInChildren<Animator>(true)
                .Where(a => a.gameObject != player.gameObject).ToArray();
            if (children.Length > 1)
            {
                EditorUtility.DisplayDialog("Player Animator", "Multiple child Animators found. Keep only the intended model under Player before migrating.", "OK");
                return;
            }
            var source = children.FirstOrDefault();
            var avatar = source && source.avatar ? source.avatar : root ? root.avatar : null;
            var controller = root && root.runtimeAnimatorController ? root.runtimeAnimatorController
                : source ? source.runtimeAnimatorController : null;
            if (!avatar || !avatar.isValid || !avatar.isHuman || !controller)
            {
                EditorUtility.DisplayDialog("Player Animator", "Assign the model's valid Humanoid Avatar and a controller before migrating. The existing root controller takes priority.", "OK");
                return;
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Move Player Animator To Root");
            try
            {
                if (!root) root = Undo.AddComponent<Animator>(player.gameObject);
                Undo.RecordObject(root, "Configure root Animator");
                root.avatar = avatar;
                root.runtimeAnimatorController = controller;
                root.applyRootMotion = false;
                root.fireEvents = true;
                root.enabled = true;
                if (source)
                {
                    root.updateMode = source.updateMode;
                    root.cullingMode = source.cullingMode;
                    foreach (var component in player.GetComponentsInChildren<Component>(true))
                    {
                        if (!component || component == source) continue;
                        var serialized = new SerializedObject(component);
                        var property = serialized.GetIterator();
                        bool changed = false;
                        while (property.Next(true))
                        {
                            if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != source) continue;
                            if (!changed) Undo.RecordObject(component, "Rebind root Animator");
                            property.objectReferenceValue = root;
                            changed = true;
                        }
                        if (changed)
                        {
                            serialized.ApplyModifiedProperties();
                            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                        }
                    }
                    Undo.DestroyObjectImmediate(source);
                }
                foreach (var relay in player.GetComponentsInChildren<MonoBehaviour>(true))
                    if (relay && relay.GetType().Name == "PlayerAnimationAudioRelay")
                        Undo.DestroyObjectImmediate(relay);
                PrefabUtility.RecordPrefabInstancePropertyModifications(root);
                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
                Selection.activeGameObject = player.gameObject;
                Undo.CollapseUndoOperations(group);
                Debug.Log("Player Animator moved to root. Footstep / landing clips unchanged. Save the scene or prefab. Walk/run clips need OnFootstep events; landing needs OnLand.", player);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception);
            }
        }
    }
}
