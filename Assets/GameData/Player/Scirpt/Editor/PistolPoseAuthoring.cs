using System;
using System.Collections.Generic;
using System.Globalization;
using GameData.Player.Script;
using UnityEditor;
using UnityEngine;

namespace GameData.Player.Editor
{
    [InitializeOnLoad]
    public static class PistolPoseAuthoring
    {
        private const string PreviewKey = "PistolHandPose.OwnsPreview";
        static PistolPoseAuthoring()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode) StopPreview();
            };
        }

        public static PistolHandPose Save(PistolPresentation component)
        {
            var so = new SerializedObject(component);
            var preset = so.FindProperty("savedPose").objectReferenceValue as PistolHandPose;
            if (preset == null)
            {
                string path = EditorUtility.SaveFilePanelInProject("Lưu pose tay", "PistolHandPose", "asset", "Chọn nơi lưu preset pose tay.");
                if (string.IsNullOrEmpty(path)) return null;
                preset = ScriptableObject.CreateInstance<PistolHandPose>();
                AssetDatabase.CreateAsset(preset, AssetDatabase.GenerateUniqueAssetPath(path));
                so.FindProperty("savedPose").objectReferenceValue = preset;
                so.ApplyModifiedProperties();
            }
            Capture(component, preset);
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssetIfDirty(preset);
            Debug.Log("Đã lưu pose tay: " + AssetDatabase.GetAssetPath(preset), component);
            return preset;
        }

        public static void Capture(PistolPresentation component, PistolHandPose preset)
        {
            var animator = component.GetComponent<Animator>();
            if (!animator || !animator.isHuman) throw new InvalidOperationException("Humanoid Animator required.");
            Undo.RecordObject(preset, "Save pistol hand pose");
            var captured = new List<PistolHandPose.BonePose>();
            foreach (HumanBodyBones id in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (!PistolHandPose.IsArmBone(id)) continue;
                Transform bone = component.GetPoseBone(id);
                if (bone) captured.Add(new PistolHandPose.BonePose { bone = id, position = bone.localPosition, rotation = bone.localRotation });
            }
            preset.bones = captured.ToArray();
            if (preset.bones.Length == 0) throw new InvalidOperationException("Không tìm thấy xương tay để lưu pose.");
            var so = new SerializedObject(component);
            preset.handsPosition = so.FindProperty("handsPosition").vector3Value;
            preset.handsEuler = so.FindProperty("handsEuler").vector3Value;
            preset.viewDistance = component.ViewDistance;
            preset.rightElbowHint = so.FindProperty("rightElbowHint").vector3Value;
            preset.leftElbowHint = so.FindProperty("leftElbowHint").vector3Value;
            var gun = so.FindProperty("gun").objectReferenceValue as Transform;
            bool captureGunTransform = !Application.isPlaying && !component.EditingAimPose && gun;
            preset.gunLocalPosition = captureGunTransform ? gun.localPosition : so.FindProperty("gunLocalPosition").vector3Value;
            preset.gunLocalEuler = captureGunTransform ? gun.localEulerAngles : so.FindProperty("gunLocalEuler").vector3Value;
            preset.gunScale = gun ? gun.localScale.x : so.FindProperty("gunScale").floatValue;
            preset.valid = true;
            preset.hasAimSettings = true;
            preset.aimHandsPosition = so.FindProperty("aimHandsPosition").vector3Value;
            preset.aimHandsEuler = so.FindProperty("aimHandsEuler").vector3Value;
            preset.separateAimGunOffsets = so.FindProperty("separateAimGunOffsets").boolValue;
            preset.aimGunLocalPosition = so.FindProperty("aimGunLocalPosition").vector3Value;
            preset.aimGunLocalEuler = so.FindProperty("aimGunLocalEuler").vector3Value;
            var point = so.FindProperty("aimPoint").objectReferenceValue as Transform;
            preset.aimPointLocalPosition = point ? point.localPosition : Vector3.zero;
        }

        public static void Preview(PistolPresentation component)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Thoát Play Mode để chỉnh từng xương mà Animator không ghi đè.", component);
                return;
            }
            var preset = new SerializedObject(component).FindProperty("savedPose").objectReferenceValue as PistolHandPose;
            if (!preset || !preset.valid || preset.bones == null || preset.bones.Length == 0) return;
            Undo.RecordObject(component, "Load saved hand pose offsets");
            component.LoadSavedPose();
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            AnimationMode.StartAnimationMode(); SessionState.SetBool(PreviewKey, true);
            var animator = component.GetComponent<Animator>();
            AnimationMode.BeginSampling();
            try
            {
                foreach (var pose in preset.bones)
                {
                    if (!PistolHandPose.IsArmBone(pose.bone)) continue;
                    Transform bone = component.GetPoseBone(pose.bone); if (!bone) continue;
                    Track(bone, animator.transform, "m_LocalPosition", bone.localPosition);
                    Track(bone, animator.transform, "m_LocalRotation", new Vector4(bone.localRotation.x, bone.localRotation.y, bone.localRotation.z, bone.localRotation.w));
                    bone.localPosition = pose.position; bone.localRotation = pose.rotation;
                }
            }
            finally { AnimationMode.EndSampling(); }
            SceneView.RepaintAll();
        }

        private static void Track(Transform bone, Transform root, string property, Vector4 value)
        {
            string path = AnimationUtility.CalculateTransformPath(bone, root);
            int count = property == "m_LocalPosition" ? 3 : 4;
            for (int i = 0; i < count; i++)
            {
                string name = property + "." + "xyzw"[i];
                AnimationMode.AddPropertyModification(EditorCurveBinding.FloatCurve(path, typeof(Transform), name),
                    new PropertyModification { target = bone, propertyPath = name, value = value[i].ToString("R", CultureInfo.InvariantCulture) }, true);
            }
        }

        public static void StopPreview()
        {
            if (!SessionState.GetBool(PreviewKey, false)) return;
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            SessionState.SetBool(PreviewKey, false);
        }
    }
}
