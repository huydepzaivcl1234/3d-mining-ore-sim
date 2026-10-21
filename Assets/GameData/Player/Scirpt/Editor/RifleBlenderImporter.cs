using System;
using System.IO;
using System.Linq;
using GameData.Player.Script;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameData.Player.Editor
{
    /// <summary>Explicit, repeatable import. Never runs automatically or saves the user's scene.</summary>
    public static class RifleBlenderImporter
    {
        [MenuItem("Chest Defense/Player/Apply Blender Rifle Export")]
        public static void ApplySelected()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                path = EditorUtility.OpenFilePanel("Choose Rifle_Unity_Idle.json inside Assets", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path)) Debug.Log(Apply(path));
        }

        public static string Apply(string jsonPath)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before importing.");
            string absolute = Path.GetFullPath(jsonPath);
            string assetRoot = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(assetRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Copy the exported JSON and FBX into Assets first.");
            string folder = "Assets/" + Path.GetRelativePath(Application.dataPath, Path.GetDirectoryName(absolute)).Replace('\\', '/');
            var data = JObject.Parse(File.ReadAllText(absolute));
            if ((int)data["version"] != 1) throw new InvalidOperationException("Unsupported Blender export version.");
            string fbx = folder + "/" + (string)data["fbx"];
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerWeaponProgression>();
            if (!player) throw new InvalidOperationException("Open the gameplay scene with PlayerWeaponProgression.");
            var presentation = player.GetComponent<PistolPresentation>();
            if (Mathf.Abs(presentation.ViewCamera.fieldOfView - (float)data["fov"]) > .01f)
                throw new InvalidOperationException("Unity camera FOV must match the exported Blender vertical FOV: " + data["fov"]);
            var serialized = new SerializedObject(player);
            var loadout = serialized.FindProperty("rifle");
            var gun = (Transform)loadout.FindPropertyRelative("gun").objectReferenceValue;
            var pose = (PistolHandPose)loadout.FindPropertyRelative("pose").objectReferenceValue;
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gun.gameObject);
            if (string.IsNullOrEmpty(prefabPath)) throw new InvalidOperationException("The rifle must be a prefab instance.");
            AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            var settings = importer.defaultClipAnimations;
            foreach (var setting in settings) setting.loopTime = true;
            importer.clipAnimations = settings;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var clip = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            var preview = EditorSceneManager.NewPreviewScene();
            GameObject source = null, body = null, contents = null;
            int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Apply authored Blender rifle");
            try
            {
                source = UnityEngine.Object.Instantiate(model);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(source, preview);
                float time = Mathf.Clamp((float)data["referenceTime"], 0, clip.length);
                clip.SampleAnimation(source, time);
                var sourceAnimator = source.GetComponent<Animator>();
                if (!sourceAnimator || !sourceAnimator.isHuman) throw new InvalidOperationException("Exported rifle rig is not a valid Humanoid.");
                Vector3 rightPosition = V(data["rightHandPosition"]), leftPosition = V(data["leftHandPosition"]);
                float sourceError = Vector3.Distance(sourceAnimator.GetBoneTransform(HumanBodyBones.RightHand).position, rightPosition);
                if (sourceError > .002f) throw new InvalidOperationException("FBX and JSON do not describe the same pose: " + sourceError);
                body = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(player.GetComponent<Animator>().avatar)));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(body, preview);
                var target = body.GetComponent<Animator>();
                var fingerAxes = new System.Collections.Generic.Dictionary<HumanBodyBones, Vector3>();
                for (var id = HumanBodyBones.LeftThumbProximal; id <= HumanBodyBones.RightLittleDistal; id++)
                {
                    var bone = target.GetBoneTransform(id); if (!bone) continue;
                    Vector3 direction = bone.childCount > 0 ? bone.GetChild(0).position - bone.position : bone.position - bone.parent.position;
                    fingerAxes[id] = bone.InverseTransformDirection(direction).normalized;
                }
                clip.SampleAnimation(body, time);
                var right = target.GetBoneTransform(HumanBodyBones.RightHand);
                var left = target.GetBoneTransform(HumanBodyBones.LeftHand);
                Quaternion rightRotation = data["rightPalmRotation"] != null ? Q(data["rightPalmRotation"]) * Quaternion.Inverse(PalmFrame(target, false)) * right.rotation : right.rotation;
                Quaternion leftRotation = data["leftPalmRotation"] != null ? Q(data["leftPalmRotation"]) * Quaternion.Inverse(PalmFrame(target, true)) * left.rotation : left.rotation;
                var chest = target.GetBoneTransform(HumanBodyBones.UpperChest) ?? target.GetBoneTransform(HumanBodyBones.Chest);
                Vector3 gunPosition = V(data["gunPosition"]); Quaternion gunRotation = Q(data["gunRotation"]);
                Matrix4x4 gunWorld = Matrix4x4.TRS(gunPosition, gunRotation, Vector3.one);
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                foreach (string name in data["parts"].Values<string>())
                {
                    Transform authored = source.GetComponentsInChildren<Transform>().Single(t => t.name == name);
                    Transform part = contents.transform.Find(name);
                    if (!part) throw new InvalidOperationException("Missing rifle part: " + name);
                    Matrix4x4 local = gunWorld.inverse * authored.localToWorldMatrix;
                    part.localPosition = local.GetColumn(3); part.localRotation = local.rotation; part.localScale = local.lossyScale;
                    part.GetComponent<MeshFilter>().sharedMesh = authored.GetComponent<MeshFilter>().sharedMesh;
                    // Reuse existing game materials. Geometry/transforms come from Blender.
                }
                SetMarker(contents.transform, "Muzzle", gunWorld.inverse.MultiplyPoint3x4(V(data["muzzlePosition"])));
                SetMarker(contents.transform, "AimPoint", gunWorld.inverse.MultiplyPoint3x4(V(data["aimPosition"])));
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                Undo.RecordObject(pose, "Import rifle camera and hand grip");
                Quaternion cameraRotation = Q(data["cameraRotation"]);
                pose.useAnimationPose = pose.useAuthoredSupport = pose.valid = true;
                pose.animationReferencePosition = chest.parent.InverseTransformPoint(right.position);
                pose.animationReferenceRotation = Quaternion.Inverse(chest.parent.rotation) * right.rotation;
                pose.animationWristEuler = (Quaternion.Inverse(cameraRotation) * rightRotation).eulerAngles;
                pose.handsPosition = Quaternion.Inverse(cameraRotation) * (rightPosition - V(data["cameraPosition"]));
                pose.handsEuler = Vector3.zero; pose.viewDistance = 0;
                pose.supportPosition = Quaternion.Inverse(rightRotation) * (leftPosition - rightPosition);
                pose.supportEuler = (Quaternion.Inverse(rightRotation) * leftRotation).eulerAngles;
                if (data["fingers"] != null)
                {
                    right.rotation = rightRotation; left.rotation = leftRotation;
                    var fingers = new System.Collections.Generic.List<PistolHandPose.BonePose>();
                    foreach (var entry in data["fingers"])
                    {
                        var id = (HumanBodyBones)Enum.Parse(typeof(HumanBodyBones), (string)entry["bone"]);
                        var bone = target.GetBoneTransform(id); if (!bone || !fingerAxes.ContainsKey(id)) continue;
                        Quaternion palm = Q(data[id.ToString().StartsWith("Left") ? "leftPalmRotation" : "rightPalmRotation"]);
                        Vector3 desired = palm * V(entry["direction"]);
                        bone.rotation = Quaternion.FromToRotation(bone.TransformDirection(fingerAxes[id]), desired) * bone.rotation;
                        fingers.Add(new PistolHandPose.BonePose { bone = id, position = bone.localPosition, rotation = bone.localRotation });
                    }
                    pose.bones = fingers.ToArray(); pose.useAuthoredFingers = true;
                }
                pose.gunLocalPosition = Quaternion.Inverse(rightRotation) * (gunPosition - rightPosition);
                pose.gunLocalEuler = (Quaternion.Inverse(rightRotation) * gunRotation).eulerAngles;
                pose.gunScale = 1; pose.separateAimGunOffsets = false;
                pose.hasAimSettings = true;
                pose.aimPointLocalPosition = gunWorld.inverse.MultiplyPoint3x4(V(data["aimPosition"]));
                EditorUtility.SetDirty(pose); AssetDatabase.SaveAssetIfDirty(pose);
                var controller = (AnimatorController)player.GetComponent<Animator>().runtimeAnimatorController;
                var layer = controller.layers.Single(l => l.name == loadout.FindPropertyRelative("animationLayer").stringValue);
                var tree = layer.stateMachine.states.Select(s => s.state.motion).OfType<BlendTree>().Single();
                Undo.RecordObject(tree, "Use exported rifle idle clip");
                var children = tree.children;
                if (children.Length != 1) throw new InvalidOperationException("Rifle layer must contain its single authored idle child.");
                children[0].motion = clip; tree.children = children;
                EditorUtility.SetDirty(tree); EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
                layer.avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
                EditorUtility.SetDirty(layer.avatarMask); AssetDatabase.SaveAssetIfDirty(layer.avatarMask);
                // Existing scene references stay valid; revert only mesh-part overrides to their new prefab values.
                foreach (string name in data["parts"].Values<string>().Concat(new[] { "Muzzle", "AimPoint" }))
                {
                    var part = gun.Find(name); if (!part) continue;
                    var defaults = contents.transform.Find(name);
                    Undo.RecordObject(part, "Sync Blender rifle part");
                    part.localPosition = defaults.localPosition; part.localRotation = defaults.localRotation; part.localScale = defaults.localScale;
                    var mesh = part.GetComponent<MeshFilter>();
                    if (mesh) { Undo.RecordObject(mesh, "Sync Blender rifle mesh"); mesh.sharedMesh = defaults.GetComponent<MeshFilter>().sharedMesh; PrefabUtility.RecordPrefabInstancePropertyModifications(mesh); }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(part);
                }
                return "Applied " + data["parts"].Count() + " Blender rifle parts, camera framing, idle and both grips. Scene NOT saved; review then Ctrl+S.";
            }
            finally
            {
                if (contents) PrefabUtility.UnloadPrefabContents(contents);
                if (source) UnityEngine.Object.DestroyImmediate(source);
                if (body) UnityEngine.Object.DestroyImmediate(body);
                EditorSceneManager.ClosePreviewScene(preview); Undo.CollapseUndoOperations(undo);
            }
        }
        static Vector3 V(JToken a) => new Vector3((float)a[0], (float)a[1], (float)a[2]);
        static Quaternion Q(JToken a) => new Quaternion((float)a[0], (float)a[1], (float)a[2], (float)a[3]);
        static Quaternion PalmFrame(Animator animator, bool left)
        {
            var wrist = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            Vector3 forward = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal).position - wrist.position;
            Vector3 across = animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal).position - animator.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal).position;
            return Quaternion.LookRotation(forward, Vector3.Cross(forward, across));
        }
        static void SetMarker(Transform root, string name, Vector3 position)
        {
            var marker = root.Find(name);
            if (!marker) { marker = new GameObject(name).transform; marker.SetParent(root, false); }
            marker.localPosition = position; marker.localRotation = Quaternion.identity;
        }
    }
}
