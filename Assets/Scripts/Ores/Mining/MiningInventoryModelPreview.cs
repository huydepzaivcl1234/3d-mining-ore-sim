using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Owns a render-only character copy, never clones gameplay, colliders, audio or VFX.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningInventoryModelPreview : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private RawImage output;
        [SerializeField] private Vector2Int resolution = new(512, 768);
        [Range(0, 31), Tooltip("Dedicated preview-only layer; no ProjectSettings changes are required.")]
        [SerializeField] private int previewLayer = 31;
        [SerializeField] private Vector3 stagePosition = new(0f, -10000f, 0f);
        [Range(10f, 70f), SerializeField] private float fieldOfView = 32f;
        [SerializeField] private float modelYaw;
        [Min(1f), SerializeField] private float framingPadding = 1.12f;
        [SerializeField] private Color background = Color.black;
        [SerializeField] private string idleState = "Base Layer.Idle Walk Run Blend";
        private GameObject stage;
        private RenderTexture texture;
        private readonly Dictionary<Transform, Transform> copies = new();

        private void OnEnable() { if (Application.isPlaying) BuildPreview(); }
        private void OnDisable() => ReleasePreview();
        private void OnDestroy() => ReleasePreview();

        public void RebuildPreview() { ReleasePreview(); if (isActiveAndEnabled && Application.isPlaying) BuildPreview(); }

        private void BuildPreview()
        {
            if (player == null || visualRoot == null || output == null) return;
            stage = new GameObject("Inventory Render-Only Stage") { hideFlags = HideFlags.DontSave };
            stage.transform.position = stagePosition;
            var model = new GameObject(player.name);
            model.transform.SetParent(stage.transform, false);
            model.layer = previewLayer;
            CopyTransforms(visualRoot, model.transform);
            foreach (Renderer source in visualRoot.GetComponentsInChildren<Renderer>(false))
            {
                if (!source.enabled || !copies.TryGetValue(source.transform, out Transform node)) continue;
                Renderer renderCopy = null;
                if (source is SkinnedMeshRenderer skin)
                {
                    var copy = node.gameObject.AddComponent<SkinnedMeshRenderer>();
                    copy.sharedMesh = skin.sharedMesh;
                    copy.localBounds = skin.localBounds;
                    var bones = new Transform[skin.bones.Length];
                    for (int i = 0; i < bones.Length; i++)
                        if (skin.bones[i] != null) copies.TryGetValue(skin.bones[i], out bones[i]);
                    copy.bones = bones;
                    if (skin.rootBone != null && copies.TryGetValue(skin.rootBone, out Transform root)) copy.rootBone = root;
                    if (skin.sharedMesh != null)
                        for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++) copy.SetBlendShapeWeight(i, skin.GetBlendShapeWeight(i));
                    copy.updateWhenOffscreen = true;
                    renderCopy = copy;
                }
                else if (source is MeshRenderer && source.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    node.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    renderCopy = node.gameObject.AddComponent<MeshRenderer>();
                }
                if (renderCopy == null) continue;
                renderCopy.sharedMaterials = source.sharedMaterials;
                renderCopy.shadowCastingMode = ShadowCastingMode.Off;
                renderCopy.receiveShadows = false;
            }
            model.transform.localRotation = Quaternion.Euler(0f, modelYaw, 0f);
            var sourceAnimator = player.GetComponent<Animator>();
            if (sourceAnimator != null && sourceAnimator.avatar != null && sourceAnimator.avatar.isHuman)
            {
                var animator = model.AddComponent<Animator>();
                animator.avatar = sourceAnimator.avatar;
                animator.runtimeAnimatorController = sourceAnimator.runtimeAnimatorController;
                animator.applyRootMotion = false;
                animator.fireEvents = false; // Footsteps/equipment events must never run in a portrait.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (animator.runtimeAnimatorController != null)
                {
                    foreach (var p in animator.parameters)
                    {
                        if (p.type == AnimatorControllerParameterType.Float) animator.SetFloat(p.nameHash, 0f);
                        else if (p.type == AnimatorControllerParameterType.Bool) animator.SetBool(p.nameHash, p.name == "Grounded");
                    }
                    for (int i = 1; i < animator.layerCount; i++) animator.SetLayerWeight(i, 0f);
                    if (animator.HasState(0, Animator.StringToHash(idleState))) animator.Play(idleState, 0, 0f);
                    animator.Update(0f);
                }
            }
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { ReleasePreview(); return; }
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            texture = new RenderTexture(Mathf.Max(16, resolution.x), Mathf.Max(16, resolution.y), 24)
                { name = "Inventory Character Preview", hideFlags = HideFlags.DontSave };
            texture.Create();
            var cameraObject = new GameObject("Inventory Preview Camera");
            cameraObject.transform.SetParent(stage.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.cullingMask = 1 << previewLayer;
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = texture;
            camera.allowHDR = false;
            float tangent = Mathf.Tan(fieldOfView * Mathf.Deg2Rad * .5f);
            float aspect = (float)texture.width / texture.height;
            float distance = Mathf.Max(bounds.extents.y / tangent, bounds.extents.x / (tangent * aspect)) * framingPadding + bounds.extents.z;
            camera.transform.position = bounds.center + Vector3.forward * distance;
            camera.transform.LookAt(bounds.center);
            var lamp = new GameObject("Inventory Preview Light");
            lamp.transform.SetParent(stage.transform, false);
            lamp.transform.rotation = Quaternion.Euler(35f, 155f, 0f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.cullingMask = 1 << previewLayer;
            light.shadows = LightShadows.None;
            output.texture = texture;
        }

        private void CopyTransforms(Transform source, Transform parent)
        {
            var copy = new GameObject(source.name);
            copy.layer = previewLayer;
            copy.transform.SetParent(parent, false);
            copy.transform.localPosition = source.localPosition;
            copy.transform.localRotation = source.localRotation;
            copy.transform.localScale = source.localScale;
            copy.SetActive(source.gameObject.activeSelf);
            copies.Add(source, copy.transform);
            foreach (Transform child in source) CopyTransforms(child, copy.transform);
        }

        private void ReleasePreview()
        {
            if (output != null) output.texture = null;
            if (stage != null) Destroy(stage);
            if (texture != null) { texture.Release(); Destroy(texture); }
            stage = null;
            texture = null;
            copies.Clear();
        }
    }
}
