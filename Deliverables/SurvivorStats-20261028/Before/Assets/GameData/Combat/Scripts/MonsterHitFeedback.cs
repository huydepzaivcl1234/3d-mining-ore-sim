using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Visual-only pulse: never scales the movement motor or interrupts an attack.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(MiningCharacterHealth))]
    public sealed class MonsterHitFeedback : MonoBehaviour
    {
        private MiningCharacterHealth health;
        private Transform visual;
        private Vector3 baseScale;
        private sealed class Slot
        {
            public Renderer renderer;
            public Material material;
            public int index, colorProperty;
            public Color color, emission;
            public readonly MaterialPropertyBlock original = new();
        }
        private readonly List<Slot> slots = new();
        private MaterialPropertyBlock block;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
        private float elapsed;
        private bool pulsing, initialized;
        private void Awake() { health = GetComponent<MiningCharacterHealth>(); block = new MaterialPropertyBlock(); }
        private void OnEnable() => health.Damaged += Pulse;
        private void OnDisable() { health.Damaged -= Pulse; Restore(); }
        private void Pulse()
        {
            var data = MonsterHitFeedbackData.Current;
            if (!data || !data.enabled || health.Health <= 0 || pulsing) return;
            if (!initialized)
            {
                var candidates = GetComponentsInChildren<Renderer>(true);
                var materials = new List<Material>();
                var animator = GetComponentInChildren<Animator>();
                if (animator && animator.transform != transform) visual = animator.transform;
                foreach (var renderer in candidates)
                {
                    if (!(renderer is SkinnedMeshRenderer || renderer is MeshRenderer) || renderer.GetComponent<TMPro.TMP_Text>()) continue;
                    if (!visual && renderer.transform != transform)
                    {
                        visual = renderer.transform;
                        while (visual.parent && visual.parent != transform) visual = visual.parent;
                    }
                    renderer.GetSharedMaterials(materials);
                    for (int i = 0; i < materials.Count; i++)
                    {
                        var material = materials[i]; if (!material) continue;
                        int property = material.HasProperty(BaseColor) ? BaseColor : ColorId;
                        if (material.HasProperty(property)) slots.Add(new Slot { renderer = renderer, material = material, index = i, colorProperty = property });
                    }
                }
                if (visual) baseScale = visual.localScale;
                initialized = true;
            }
            foreach (var slot in slots)
            {
                if (!slot.renderer) continue;
                slot.renderer.GetPropertyBlock(slot.original, slot.index);
                slot.renderer.GetPropertyBlock(block);
                slot.color = slot.original.HasColor(slot.colorProperty) ? slot.original.GetColor(slot.colorProperty) :
                    block.HasColor(slot.colorProperty) ? block.GetColor(slot.colorProperty) : slot.material.GetColor(slot.colorProperty);
                slot.emission = slot.original.HasColor(EmissionColor) ? slot.original.GetColor(EmissionColor) :
                    block.HasColor(EmissionColor) ? block.GetColor(EmissionColor) : slot.material.HasProperty(EmissionColor) ? slot.material.GetColor(EmissionColor) : Color.black;
            }
            elapsed = 0; pulsing = true;
        }
        private void LateUpdate()
        {
            if (!pulsing) return;
            var data = MonsterHitFeedbackData.Current;
            if (!data || !data.enabled || health.Health <= 0) { Restore(); return; }
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, data.duration));
            // Rapid effect ticks must not continually restart the flash at zero brightness.
            float strength = 1f - Mathf.SmoothStep(0, 1, progress);
            float squash = Mathf.Sin(progress * Mathf.PI) * data.squash;
            if (visual) visual.localScale = Vector3.Scale(baseScale, new Vector3(1 + squash, 1 - squash, 1 + squash));
            Tint(strength * data.flashStrength, data.flashColor);
            if (progress >= 1) Restore();
        }
        private void Tint(float strength, Color color)
        {
            foreach (var slot in slots)
            {
                if (!slot.renderer) continue;
                slot.renderer.GetPropertyBlock(block, slot.index);
                block.SetColor(slot.colorProperty, Color.Lerp(slot.color, color, strength));
                if (slot.material.HasProperty(EmissionColor))
                {
                    block.SetColor(EmissionColor, slot.emission + color * (strength * MonsterHitFeedbackData.Current.emissionIntensity));
                    if (slot.material.HasProperty(EmissionMap)) block.SetTexture(EmissionMap, Texture2D.whiteTexture);
                }
                slot.renderer.SetPropertyBlock(block, slot.index);
            }
        }
        private void Restore()
        {
            if (!pulsing) return;
            if (visual) visual.localScale = baseScale;
            // Empty blocks must become null again, otherwise the hit permanently
            // opts an otherwise batchable renderer out of the SRP Batcher.
            foreach (var slot in slots)
                if (slot.renderer) slot.renderer.SetPropertyBlock(slot.original.isEmpty ? null : slot.original, slot.index);
            pulsing = false;
        }
    }
}
