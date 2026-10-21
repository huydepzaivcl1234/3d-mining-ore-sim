using UnityEngine;

namespace GameData.Player.Script
{
    /// <summary>Bounded visual-only tracer/impact pool; damage remains immediate hitscan.</summary>
    public sealed class PistolShotVfx
    {
        private sealed class Slot
        {
            public LineRenderer line;
            public ParticleSystem impact;
            public Vector3 start, end, normal;
            public float age, distance;
            public bool active, hit, impacted;
        }
        private readonly Slot[] slots = new Slot[12];
        private readonly PistolWeaponData data;
        private readonly MiningSimulator.Ores.MiningPlayerStats stats;
        private float Speed => Mathf.Max(1f, data.tracerSpeed * (stats ? stats.GunBulletSpeedMultiplier : 1f));
        private readonly Transform root;
        private int cursor;
        public int TracersPlayed { get; private set; }
        public int ImpactsPlayed { get; private set; }
        public int ActiveTracers { get { int count = 0; foreach (var slot in slots) if (slot != null && slot.active) count++; return count; } }

        public PistolShotVfx(PistolWeaponData data, MiningSimulator.Ores.MiningPlayerStats stats = null)
        {
            this.data = data;
            this.stats = stats;
            if (!data || !data.shotVfxMaterial) return;
            root = new GameObject("Pistol Shot VFX Pool").transform;
            root.gameObject.hideFlags = HideFlags.DontSave;
            for (int i = 0; i < slots.Length; i++)
            {
                var go = new GameObject("Tracer " + i); go.transform.SetParent(root, false);
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = data.shotVfxMaterial; line.positionCount = 2;
                line.useWorldSpace = true; line.numCapVertices = 3; line.enabled = false;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
                var effect = new GameObject("Impact " + i); effect.transform.SetParent(root, false);
                var particle = effect.AddComponent<ParticleSystem>();
                var main = particle.main; main.playOnAwake = false; main.loop = false;
                main.startLifetime = .24f; main.startSpeed = 2.5f; main.startSize = .06f;
                main.gravityModifier = .3f; main.maxParticles = 16; main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = particle.emission; emission.enabled = false;
                var shape = particle.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 55f; shape.radius = .01f;
                var renderer = particle.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = data.shotVfxMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                var color = particle.colorOverLifetime; color.enabled = true;
                var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                    new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); color.color = gradient;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                slots[i] = new Slot { line = line, impact = particle };
            }
        }

        public void Play(Vector3 start, Vector3 end, Vector3 normal, bool hit)
        {
            if (!root) return;
            var slot = slots[cursor]; cursor = (cursor + 1) % slots.Length;
            slot.impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            slot.start = start; slot.end = end; slot.normal = normal;
            slot.distance = Vector3.Distance(start, end); slot.age = 0; slot.hit = hit; slot.impacted = false; slot.active = true;
            slot.line.startWidth = data.tracerWidth; slot.line.endWidth = data.tracerWidth * .5f;
            slot.line.startColor = slot.line.endColor = data.tracerColor;
            slot.line.SetPosition(0, start); slot.line.SetPosition(1, Vector3.Lerp(start, end, Mathf.Min(1f, .05f / Mathf.Max(.001f, slot.distance))));
            slot.line.enabled = true; TracersPlayed++;
        }

        public void Step(float dt)
        {
            if (!root || dt <= 0f) return;
            foreach (var slot in slots)
            {
                if (!slot.active) continue;
                slot.age += dt;
                float head = Mathf.Min(slot.distance, slot.age * Speed);
                Vector3 direction = (slot.end - slot.start).normalized;
                slot.line.SetPosition(0, slot.start + direction * Mathf.Max(0f, head - data.tracerLength));
                slot.line.SetPosition(1, slot.start + direction * head);
                if (head >= slot.distance && !slot.impacted)
                {
                    slot.impacted = true;
                    if (slot.hit)
                    {
                        slot.impact.transform.SetPositionAndRotation(slot.end + slot.normal * .012f, Quaternion.LookRotation(slot.normal.sqrMagnitude > .001f ? slot.normal : Vector3.up));
                        var emit = new ParticleSystem.EmitParams { startColor = data.impactColor };
                        slot.impact.Play(); slot.impact.Emit(emit, 10); ImpactsPlayed++;
                    }
                }
                if (slot.age >= Mathf.Max(.06f, slot.distance / Speed + .03f))
                { slot.active = false; slot.line.enabled = false; }
            }
        }

        public void Clear()
        {
            if (!root) return;
            foreach (var slot in slots) { slot.active = false; slot.line.enabled = false; slot.impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        }
        public void Dispose() { if (root) Object.Destroy(root.gameObject); }
    }
}
