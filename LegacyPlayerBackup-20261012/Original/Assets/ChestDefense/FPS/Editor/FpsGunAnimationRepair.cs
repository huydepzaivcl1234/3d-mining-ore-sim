#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ChestDefense.Fps.Editor
{
    // Bake only the gun-only presentation into a consistent idle reference frame.
    // The vendor FBX remains untouched; magazine, bolt and slide tracks are preserved.
    public static class FpsGunAnimationRepair
    {
        const string Folder = "Assets/ChestDefense/FPS/Generated";
        [MenuItem("Chest Defense/FPS/4. Repair supplied gun clips and alignment")]
        public static void Repair()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            foreach (string name in new[] { "Pistol", "Rifle" })
            {
                var data = AssetDatabase.LoadAssetAtPath<FpsWeaponData>(Folder + "/" + name + ".asset");
                var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/ChestDefense/FPS/Models/" + name + "_FPS.fbx")
                    .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__") && !c.name.Contains("RootNode")).ToArray();
                data.shoot = Normalize(data, clips.First(c => c.name.Contains("Shoot")), name + "_Shoot");
                data.reload = Normalize(data, clips.First(c => c.name.Contains("Reload")), name + "_Reload");
                Align(data, name == "Rifle");
                data.minimumShotInterval = name == "Rifle" ? .125f : .35f;
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
                AssetDatabase.SaveAssetIfDirty(data.shoot); AssetDatabase.SaveAssetIfDirty(data.reload);
            }
            CreateHudMaterial();
            FpsSightChecks.Align();
        }
        static void CreateHudMaterial()
        {
            string folder = "Assets/ChestDefense/FPS/Resources";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/ChestDefense/FPS", "Resources");
            var theme = Resources.Load<MiningSimulator.Ores.TowerUiTheme>("TowerUiTheme");
            var font = theme != null ? theme.bodyFont : null;
            if (font == null) throw new InvalidOperationException("FPS HUD requires the existing tower UI font.");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile SSD.shader");
            if (shader == null) throw new InvalidOperationException("Missing project TMP SSD shader.");
            string path = folder + "/FpsHudText.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(font.material) { name = "FpsHudText", shader = shader };
                AssetDatabase.CreateAsset(material, path);
            }
            else { material.shader = shader; material.SetTexture("_MainTex", font.atlasTexture); EditorUtility.SetDirty(material); }
            AssetDatabase.SaveAssetIfDirty(material);
        }
        static AnimationClip Normalize(FpsWeaponData data, AnimationClip source, string name)
        {
            var go = UnityEngine.Object.Instantiate(data.viewPrefab.gameObject);
            go.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var rig = go.GetComponent<FpsGunView>().rig;
                data.idle.SampleAnimation(rig.gameObject, 0);
                var root = rig.GetComponentsInChildren<Transform>(true).First(t => t.name == "Root");
                string path = AnimationUtility.CalculateTransformPath(root, rig.transform);
                Vector3 referencePosition = rig.transform.InverseTransformPoint(root.position);
                Quaternion referenceRotation = Quaternion.Inverse(rig.transform.rotation) * root.rotation;
                Matrix4x4 fixedParent = rig.transform.worldToLocalMatrix * root.parent.localToWorldMatrix;
                Quaternion fixedParentRotation = Quaternion.Inverse(rig.transform.rotation) * root.parent.rotation;
                var copy = UnityEngine.Object.Instantiate(source); copy.name = name;
                var ancestors = root.parent;
                while (ancestors != rig.transform)
                {
                    string ancestorPath = AnimationUtility.CalculateTransformPath(ancestors, rig.transform);
                    SetConstant(copy, ancestorPath, "m_LocalPosition", ancestors.localPosition, source.length);
                    SetConstant(copy, ancestorPath, "m_LocalScale", ancestors.localScale, source.length);
                    SetConstant(copy, ancestorPath, "m_LocalRotation", ancestors.localRotation, source.length);
                    ancestors = ancestors.parent;
                }
                source.SampleAnimation(rig.gameObject, 0);
                Vector3 startPosition = rig.transform.InverseTransformPoint(root.position);
                Quaternion correction = referenceRotation * Quaternion.Inverse(Quaternion.Inverse(rig.transform.rotation) * root.rotation);
                var curves = Enumerable.Range(0, 7).Select(_ => new AnimationCurve()).ToArray();
                int steps = Mathf.Max(2, Mathf.CeilToInt(source.length * 60));
                Quaternion previous = Quaternion.identity;
                for (int i = 0; i <= steps; i++)
                {
                    float time = source.length * i / steps;
                    source.SampleAnimation(rig.gameObject, time);
                    Vector3 position = referencePosition + correction * (rig.transform.InverseTransformPoint(root.position) - startPosition);
                    position = fixedParent.inverse.MultiplyPoint3x4(position);
                    Quaternion rotation = Quaternion.Inverse(fixedParentRotation) * correction * Quaternion.Inverse(rig.transform.rotation) * root.rotation;
                    if (i > 0 && Quaternion.Dot(previous, rotation) < 0)
                        rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                    previous = rotation;
                    float[] values = { position.x, position.y, position.z, rotation.x, rotation.y, rotation.z, rotation.w };
                    for (int j = 0; j < 7; j++) curves[j].AddKey(time, values[j]);
                }
                string[] properties = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
                for (int j = 0; j < 7; j++) AnimationUtility.SetEditorCurve(copy, EditorCurveBinding.FloatCurve(path, typeof(Transform), properties[j]), curves[j]);
                copy.EnsureQuaternionContinuity();
                string assetPath = Folder + "/" + name + ".anim";
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (existing == null) { AssetDatabase.CreateAsset(copy, assetPath); return copy; }
                EditorUtility.CopySerialized(copy, existing); UnityEngine.Object.DestroyImmediate(copy); EditorUtility.SetDirty(existing); return existing;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        static void SetConstant(AnimationClip clip, string path, string property, Vector3 value, float duration)
        {
            for (int i = 0; i < 3; i++) AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property + "." + "xyz"[i]), AnimationCurve.Constant(0, duration, value[i]));
        }
        static void SetConstant(AnimationClip clip, string path, string property, Quaternion value, float duration)
        {
            for (int i = 0; i < 4; i++) AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property + "." + "xyzw"[i]), AnimationCurve.Constant(0, duration, value[i]));
        }
        static void Align(FpsWeaponData data, bool rifle)
        {
            string prefabPath = AssetDatabase.GetAssetPath(data.viewPrefab);
            var go = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var view = go.GetComponent<FpsGunView>();
                data.idle.SampleAnimation(view.rig.gameObject, 0);
                foreach (var renderer in view.rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    // Vendor bounds are attached to an animated upper arm. A hidden arm's
                    // reload motion otherwise culls the visible gun while its barrel is in view.
                    renderer.rootBone = view.rig.transform;
                    renderer.updateWhenOffscreen = true;
                }
                var root = view.rig.GetComponentsInChildren<Transform>(true).First(t => t.name == "Root");
                if (rifle)
                {
                    // Anchor to the animated weapon, not to the non-animated view root.
                    view.muzzle.SetParent(root, true);
                }
                var barrel = rifle ? root : view.rig.GetComponentsInChildren<Transform>(true).First(t => t.name == "Barrel");
                // AKM's root bone axes point into the stock/down, opposite the visible barrel/up.
                Vector3 forward = go.transform.InverseTransformDirection(rifle ? -barrel.up : barrel.up);
                Vector3 up = go.transform.InverseTransformDirection(rifle ? -barrel.right : -barrel.forward);
                Quaternion rotation = Quaternion.Inverse(Quaternion.LookRotation(forward, up));
                Vector3 tip = go.transform.InverseTransformPoint(view.muzzle.position);
                data.hipEuler = data.aimEuler = rotation.eulerAngles;
                data.hipPosition = new Vector3(.26f, rifle ? -.18f : -.145f, rifle ? .82f : .55f) - rotation * tip;
                data.aimPosition = new Vector3(rifle ? 0 : -.004f, rifle ? -.015f : .01f, rifle ? .82f : .55f) - rotation * tip;
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
    }
}
#endif
