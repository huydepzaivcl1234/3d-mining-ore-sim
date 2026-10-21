using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using GameData.Player.Script;

namespace GameData.Player.Editor
{
    /// <summary>Retargets the already admitted pistol action into small, editable IK/mechanism curves.</summary>
    public static class PistolReloadBaker
    {
        private const string Folder = "Assets/GameData/Player/Weapons/";
        [MenuItem("Chest Defense/Player/Bake Pistol Reload Targets")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Bake reload in Edit Mode.");
            var source = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ChestDefense/FPS/Generated/Pistol_Reload.anim");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChestDefense/FPS/Models/Pistol_FPS.fbx");
            var data = AssetDatabase.LoadAssetAtPath<PistolWeaponData>(Folder + "PistolWeaponData.asset");
            if (!source || !model || !data) throw new InvalidOperationException("Source pistol clip, rig or weapon data is missing.");
            var rig = UnityEngine.Object.Instantiate(model);
            rig.hideFlags = HideFlags.HideAndDontSave;
            foreach (var animator in rig.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            try
            {
                Transform weapon = rig.transform.Find("RootNode/Armature/UpperArm.R.001/LowerArm.R.001/Hand.R.001/Root");
                Transform magazine = weapon ? weapon.Find("Magazine") : null;
                if (!weapon || !magazine) throw new InvalidOperationException("Source weapon/magazine paths changed.");
                source.SampleAnimation(rig, 0f);
                var barrel = weapon.Find("Barrel"); var barrelEnd = barrel ? barrel.Find("Barrel_end") : null;
                var magazineEnd = magazine.Find("Magazine_end");
                if (!barrelEnd || !magazineEnd) throw new InvalidOperationException("Source barrel/magazine axis markers are missing.");
                // Source Root +Z is not the barrel axis. Resolve a canonical +Z barrel/+Y magazine
                // frame so retargeted removal goes to the supporting side rather than through the gun hand.
                Vector3 up = Quaternion.Inverse(weapon.rotation) * (magazineEnd.position - magazine.position).normalized;
                Vector3 forward = Quaternion.Inverse(weapon.rotation) * (barrelEnd.position - barrel.position).normalized;
                forward = Vector3.ProjectOnPlane(forward, up).normalized;
                Quaternion axes = Quaternion.LookRotation(forward, up);
                Vector3 initialWeaponPosition = weapon.position;
                Quaternion initialWeaponRotation = weapon.rotation * axes;
                // The imported generic rig contains FBX unit scale on ancestors. Retarget in meters,
                // not source-bone local units, then orient into the weapon's axes.
                Vector3 initialMagazinePosition = Quaternion.Inverse(initialWeaponRotation) * (magazine.position - weapon.position);
                Quaternion initialMagazineRotation = Quaternion.Inverse(initialWeaponRotation) * magazine.rotation;
                var curves = new Dictionary<string, AnimationCurve>();
                int samples = Mathf.CeilToInt(source.length * 60f);
                for (int i = 0; i <= samples; i++)
                {
                    float time = source.length * i / samples;
                    source.SampleAnimation(rig, time);
                    Vector3 offset = Quaternion.Inverse(initialWeaponRotation) * (weapon.position - initialWeaponPosition);
                    Quaternion sourceBasis = weapon.rotation * axes;
                    Quaternion rotation = Quaternion.Inverse(initialWeaponRotation) * sourceBasis;
                    Vector3 mag = Quaternion.Inverse(sourceBasis) * (magazine.position - weapon.position) - initialMagazinePosition;
                    Quaternion magRotation = Quaternion.Inverse(sourceBasis) * magazine.rotation * Quaternion.Inverse(initialMagazineRotation);
                    if (i == 0 || i == samples) { offset = mag = Vector3.zero; rotation = magRotation = Quaternion.identity; }
                    RecordVector(curves, "WeaponOffset", "m_LocalPosition", time, Vector3.ClampMagnitude(offset * .6f, .04f));
                    RecordQuaternion(curves, "WeaponOffset", time, rotation);
                    RecordVector(curves, "MagazineOffset", "m_LocalPosition", time, Vector3.ClampMagnitude(mag * .45f, .24f));
                    RecordQuaternion(curves, "MagazineOffset", time, magRotation);
                }
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "PistolReloadTargets.anim");
                bool newClip = !clip;
                if (newClip) clip = new AnimationClip();
                else { Undo.RecordObject(clip, "Rebake reload targets"); clip.ClearCurves(); }
                clip.name = "PistolReloadTargets"; clip.frameRate = 60f;
                foreach (var pair in curves)
                {
                    int separator = pair.Key.IndexOf('|');
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(pair.Key.Substring(0, separator), typeof(Transform), pair.Key.Substring(separator + 1)), pair.Value);
                }
                ControlCurve(clip, "x", source.length, new[] {0f,.07f,.14f,.83f,.95f,1f}, new[] {0f,0f,1f,1f,0f,0f});
                ControlCurve(clip, "y", source.length, new[] {0f,.08f,.15f,.8f,.97f,1f}, new[] {0f,0f,1f,1f,0f,0f});
                ControlCurve(clip, "z", source.length, new[] {0f,.72f,.77f,.84f,.91f,1f}, new[] {0f,0f,1f,1f,0f,0f});
                clip.EnsureQuaternionContinuity();
                var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                if (newClip) AssetDatabase.CreateAsset(clip, Folder + "PistolReloadTargets.anim");
                EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);
                var profile = AssetDatabase.LoadAssetAtPath<PistolReloadProfile>(Folder + "PistolReloadProfile.asset");
                if (!profile) { profile = ScriptableObject.CreateInstance<PistolReloadProfile>(); AssetDatabase.CreateAsset(profile, Folder + "PistolReloadProfile.asset"); }
                Undo.RecordObject(profile, "Assign reload clip"); profile.sourceClip = source; profile.targetClip = clip;
                profile.reloadSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ChestDefense/FPS/Audio/Reload.wav");
                EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
                Undo.RecordObject(data, "Enable pistol reload animation"); data.reloadProfile = profile; data.reloadSeconds = source.length;
                EditorUtility.SetDirty(data); AssetDatabase.SaveAssetIfDirty(data);
                CreateAuthoringRig(clip);
            }
            finally { UnityEngine.Object.DestroyImmediate(rig); }
        }

        private static void Key(Dictionary<string, AnimationCurve> curves, string path, string attribute, float time, float value)
        {
            string key = path + "|" + attribute;
            if (!curves.TryGetValue(key, out var curve)) { curve = new AnimationCurve(); curves.Add(key, curve); }
            curve.AddKey(new Keyframe(time, value));
        }
        private static void RecordVector(Dictionary<string, AnimationCurve> curves, string path, string attribute, float time, Vector3 v)
        { Key(curves,path,attribute+".x",time,v.x); Key(curves,path,attribute+".y",time,v.y); Key(curves,path,attribute+".z",time,v.z); }
        private static void RecordQuaternion(Dictionary<string, AnimationCurve> curves, string path, float time, Quaternion q)
        { Key(curves,path,"m_LocalRotation.x",time,q.x); Key(curves,path,"m_LocalRotation.y",time,q.y); Key(curves,path,"m_LocalRotation.z",time,q.z); Key(curves,path,"m_LocalRotation.w",time,q.w); }
        private static void ControlCurve(AnimationClip clip, string axis, float duration, float[] times, float[] values)
        {
            var curve = new AnimationCurve();
            for (int i=0;i<times.Length;i++) curve.AddKey(new Keyframe(times[i]*duration,values[i],0f,0f));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Control",typeof(Transform),"m_LocalPosition."+axis),curve);
        }
        private static void CreateAuthoringRig(AnimationClip clip)
        {
            string path = Folder + "PistolReloadTargets.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            Undo.RecordObject(controller, "Configure reload authoring controller");
            if (controller.layers.Length == 0) controller.AddLayer("Reload");
            var machine = controller.layers[0].stateMachine;
            var state = machine.defaultState;
            if (!state) state = machine.AddState("Reload");
            Undo.RecordObject(state, "Assign reload authoring motion");
            state.motion = clip; machine.defaultState = state;
            EditorUtility.SetDirty(state); EditorUtility.SetDirty(machine); EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);
            var root = new GameObject("Pistol Reload Authoring Rig");
            try
            {
                foreach(string name in new[]{"WeaponOffset","MagazineOffset","Control"})new GameObject(name).transform.SetParent(root.transform,false);
                root.AddComponent<Animator>().runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"PistolReloadAuthoring.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
