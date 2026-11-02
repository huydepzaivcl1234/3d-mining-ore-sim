using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Camera-only visual culling. Never disables AI, collision, pickup or damage clocks.</summary>
    [DisallowMultipleComponent]
    public sealed class WorldActorVisibility : MonoBehaviour
    {
        private static readonly List<WorldActorVisibility> actors = new();
        private static readonly Plane[] planes = new Plane[6];
        private Renderer[] renderers;
        private bool[] enabledBeforeCull;
        private Canvas[] canvases;
        private bool[] canvasStates;
        private Animator[] animators;
        private AnimatorCullingMode[] animatorModes;
        private bool[] offscreenSkinning;
        private Bounds localBounds;
        private bool cached, hidden, animationCulled;
        public bool IsVisible => !hidden;
        public static void SetRendererVisible(Renderer renderer, bool visible)
        {
            var owner = renderer.GetComponentInParent<WorldActorVisibility>();
            if (owner && owner.cached && owner.hidden)
            {
                for (int i = 0; i < owner.renderers.Length; i++)
                    if (owner.renderers[i] == renderer) { owner.enabledBeforeCull[i] = visible; renderer.enabled = false; return; }
            }
            renderer.enabled = visible;
        }
        public static void Attach(GameObject owner) { if (!owner.GetComponent<WorldActorVisibility>()) owner.AddComponent<WorldActorVisibility>(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() { actors.Clear(); }
        private void OnEnable() { if (!actors.Contains(this)) actors.Add(this); }
        private void OnDisable() { SetHidden(false, false); actors.Remove(this); }
        private void Cache()
        {
            if (cached) return;
            cached = true;
            renderers = GetComponentsInChildren<Renderer>(true);
            enabledBeforeCull = new bool[renderers.Length]; offscreenSkinning = new bool[renderers.Length];
            localBounds = new Bounds(Vector3.zero, Vector3.one * .1f);
            foreach (var renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer || renderer.GetComponent<TMPro.TMP_Text>()) continue;
                var b = renderer.bounds;
                EncapsulateLocal(b);
            }
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] is SkinnedMeshRenderer skin) offscreenSkinning[i] = skin.updateWhenOffscreen;
            canvases = GetComponentsInChildren<Canvas>(true); canvasStates = new bool[canvases.Length];
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.WorldSpace) continue;
                var corners = new Vector3[4]; ((RectTransform)canvas.transform).GetWorldCorners(corners);
                foreach (var point in corners) localBounds.Encapsulate(transform.InverseTransformPoint(point));
            }
            animators = GetComponentsInChildren<Animator>(true); animatorModes = new AnimatorCullingMode[animators.Length];
            for (int i = 0; i < animators.Length; i++) animatorModes[i] = animators[i].cullingMode;
        }
        private void EncapsulateLocal(Bounds bounds)
        {
            for (int i = 0; i < 8; i++)
                localBounds.Encapsulate(transform.InverseTransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
        }
        private Bounds WorldBounds(float padding)
        {
            var m = transform.localToWorldMatrix; var e = localBounds.extents;
            var x = m.MultiplyVector(new Vector3(e.x, 0, 0)); var y = m.MultiplyVector(new Vector3(0, e.y, 0)); var z = m.MultiplyVector(new Vector3(0, 0, e.z));
            return new Bounds(m.MultiplyPoint3x4(localBounds.center), new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x), Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)) * 2 + Vector3.one * padding * 2);
        }
        public static void Cull(Camera view, GameplayPerformanceData data)
        {
            if (!view || !view.isActiveAndEnabled) return;
            if (data && !data.cullOffscreenActors) { RestoreAll(); return; }
            GeometryUtility.CalculateFrustumPlanes(view, planes);
            foreach (var actor in actors)
            {
                if (!actor || !actor.isActiveAndEnabled) continue;
                actor.Cache();
                actor.SetHidden(!GeometryUtility.TestPlanesAABB(planes, actor.WorldBounds(data ? Mathf.Max(0, data.actorBoundsPadding) : .2f)), !data || data.cullOffscreenAnimation);
            }
        }
        public static void RestoreAll() { foreach (var actor in actors) if (actor) actor.SetHidden(false, false); }
        private void SetHidden(bool value, bool cullAnimation)
        {
            if (!cached) return;
            bool nextAnimationCulled = value && cullAnimation;
            if (nextAnimationCulled != animationCulled)
            {
                for (int i = 0; i < animators.Length; i++)
                    if (animators[i]) animators[i].cullingMode = nextAnimationCulled ? AnimatorCullingMode.CullUpdateTransforms : animatorModes[i];
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = nextAnimationCulled ? false : offscreenSkinning[i];
                animationCulled = nextAnimationCulled;
            }
            if (value == hidden) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i]; if (!renderer) continue;
                if (value) { enabledBeforeCull[i] = renderer.enabled; renderer.enabled = false; }
                else renderer.enabled = enabledBeforeCull[i];
            }
            for (int i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i]; if (!canvas || canvas.renderMode != RenderMode.WorldSpace) continue;
                if (value) { canvasStates[i] = canvas.enabled; canvas.enabled = false; }
                else canvas.enabled = canvasStates[i];
            }
            hidden = value;
        }
    }
}
