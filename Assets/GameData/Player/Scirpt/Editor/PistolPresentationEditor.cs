using GameData.Player.Script;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace GameData.Player.Editor
{
    /// <summary>Scene handles edit offsets, not bones that Animator overwrites.</summary>
    [CustomEditor(typeof(PistolPresentation)), CanEditMultipleObjects]
    public sealed class PistolPresentationEditor : UnityEditor.Editor
    {
        private bool previousToolsHidden;
        private void OnEnable() => previousToolsHidden = Tools.hidden;
        private void OnDisable() => Tools.hidden = previousToolsHidden;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            root.Add(new HelpBox("Chỉnh xương trong Animation Preview rồi bấm Lưu pose tay. Preset là asset riêng, không mất khi Stop. Clip FBX gốc không bị sửa.", HelpBoxMessageType.Info));
            var save = new Button(() => PistolPoseAuthoring.Save((PistolPresentation)target)) { text = "Lưu pose tay" };
            var preview = new Button(() => PistolPoseAuthoring.Preview((PistolPresentation)target)) { text = "Xem / chỉnh pose đã lưu trong Scene" };
            var stop = new Button(PistolPoseAuthoring.StopPreview) { text = "Thoát preview pose" };
            save.SetEnabled(targets.Length == 1); preview.SetEnabled(targets.Length == 1);
            root.Add(save); root.Add(preview); root.Add(stop);
            return root;
        }

        private void OnSceneGUI()
        {
            var component = target as PistolPresentation;
            var camera = component != null ? component.ViewCamera : null;
            if (camera == null)
            {
                Tools.hidden = previousToolsHidden;
                return;
            }
            bool moving = Tools.current == Tool.Move;
            bool rotating = Tools.current == Tool.Rotate;
            bool combined = Tools.current == Tool.Transform;
            Tools.hidden = moving || rotating || combined ? true : previousToolsHidden;
            bool gunEdit = component.EditingGun && component.Gun && component.Gun.parent;
            Vector3 position = gunEdit ? component.Gun.parent.TransformPoint(component.EditableGunPosition) :
                camera.transform.TransformPoint(component.EditingAimPose ? component.AimHandsPosition : component.HandsPosition);
            Quaternion rotation = gunEdit ? component.Gun.parent.rotation * Quaternion.Euler(component.EditableGunEuler) :
                camera.transform.rotation * Quaternion.Euler(component.EditingAimPose ? component.AimHandsEuler : component.HandsEuler);
            Color previousColor = Handles.color;
            Handles.color = Color.cyan;
            Handles.Label(position + camera.transform.up * .035f, (gunEdit ? "GUN" : component.EditingAimPose ? "AIM PLACEMENT" : "HAND GRIP") + " - W move / E rotate");
            Handles.DrawWireDisc(position, rotation * Vector3.forward, HandleUtility.GetHandleSize(position) * .035f);
            EditorGUI.BeginChangeCheck();
            if (moving)
                position = Handles.PositionHandle(position, Tools.pivotRotation == PivotRotation.Local ? rotation : Quaternion.identity);
            else if (rotating)
                rotation = Handles.RotationHandle(rotation, position);
            else if (combined)
                Handles.TransformHandle(ref position, ref rotation);
            if (EditorGUI.EndChangeCheck())
                SetWorldPose((PistolPresentation)target, position, rotation, moving || combined, rotating || combined);
            Handles.color = previousColor;
        }

        public static void SetWorldPose(PistolPresentation component, Vector3 position, Quaternion rotation,
            bool move, bool rotate)
        {
            if (component == null) return;
            var camera = component.ViewCamera;
            if (camera == null || (!move && !rotate)) return;
            Undo.RecordObject(component, "Drag pistol hand pose");
            if (component.EditingGun && component.Gun && component.Gun.parent)
            {
                Undo.RecordObject(component.Gun, "Drag pistol gun");
                var parent = component.Gun.parent;
                component.SetGunOffsets(move ? parent.InverseTransformPoint(position) : component.EditableGunPosition,
                    rotate ? (Quaternion.Inverse(parent.rotation) * rotation).eulerAngles : component.EditableGunEuler, component.EditingAimPose);
            }
            else
                component.SetHandPoseOffsets(
                    move ? camera.transform.InverseTransformPoint(position) : component.EditingAimPose ? component.AimHandsPosition : component.HandsPosition,
                    rotate ? (Quaternion.Inverse(camera.transform.rotation) * rotation).eulerAngles : component.EditingAimPose ? component.AimHandsEuler : component.HandsEuler,
                    component.EditingAimPose);
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            SceneView.RepaintAll();
        }
    }
}
