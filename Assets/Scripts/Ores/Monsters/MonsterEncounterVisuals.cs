using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Per-spawn boss aura and expiry. Never grants kill rewards or edits shared materials.</summary>
    [DisallowMultipleComponent]
    public sealed class MonsterEncounterVisuals : MonoBehaviour
    {
        private MushroomMonster monster;
        private MonsterRewardData settings;
        private Color color;
        private float deadline, dissolveStarted;
        private bool dissolving;
        private ParticleSystem aura;
        private Material auraMaterial;
        private Texture2D auraTexture;
        private readonly List<Material> dissolveMaterials = new();
        private static readonly int Amount = Shader.PropertyToID("_DissolveAmount");
        public float RemainingSeconds => deadline > 0f ? Mathf.Max(0f, deadline - Time.time) : 0f;

        public void Configure(MushroomMonster owner, MonsterRewardData data, MonsterBossSettings boss)
        {
            monster = owner; settings = data;
            float seconds = boss != null ? Mathf.Max(1f, boss.combatSeconds) : Mathf.Max(0f, data.combatSeconds);
            deadline = seconds > 0f ? Time.time + seconds : 0f;
            color = boss != null ? boss.effectColor : data.dissolveColor;
            if (boss != null) CreateAura();
        }

        public void CancelExpiry()
        {
            deadline = 0f;
            if (aura != null) aura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (monster == null || settings == null) return;
            if (!dissolving)
            {
                if (deadline <= 0f || Time.time < deadline || monster.Health.Health <= 0f) return;
                monster.BeginDespawn();
                if (!monster.IsDespawning) return;
                dissolving = true;
                dissolveStarted = Time.time;
                if (aura != null) aura.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                PrepareDissolve();
                // Still clean up if the owning world/spawner is disabled during the fade.
                Destroy(gameObject, Mathf.Max(.1f, settings.dissolveSeconds));
            }
            float progress = Mathf.Clamp01((Time.time - dissolveStarted) / Mathf.Max(.1f, settings.dissolveSeconds));
            foreach (var material in dissolveMaterials) material.SetFloat(Amount, progress);
            if (progress >= 1f) Destroy(gameObject);
        }

        private void PrepareDissolve()
        {
            Shader shader = settings.dissolveShader != null ? settings.dissolveShader : Shader.Find("Mining Simulator/Monster Dissolve");
            if (shader == null) return;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer)) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null) continue;
                    var material = new Material(shader);
                    Texture map = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
                    material.SetTexture("_BaseMap", map);
                    if (source.HasProperty("_BaseMap"))
                    {
                        material.SetTextureScale("_BaseMap", source.GetTextureScale("_BaseMap"));
                        material.SetTextureOffset("_BaseMap", source.GetTextureOffset("_BaseMap"));
                    }
                    material.SetColor("_BaseColor", source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
                    material.SetColor("_EdgeColor", color);
                    dissolveMaterials.Add(material);
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private void CreateAura()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) return;
            var go = new GameObject("Boss Aura");
            go.transform.SetParent(transform, false);
            var motor = GetComponent<CharacterController>();
            go.transform.localPosition = Vector3.up * (motor != null ? motor.height * .5f : .5f);
            aura = go.AddComponent<ParticleSystem>();
            aura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = aura.main;
            main.playOnAwake = false; main.loop = true;
            main.startLifetime = 1.3f; main.startSpeed = .22f;
            main.startSize = new ParticleSystem.MinMaxCurve(.045f, .09f);
            main.startColor = color; main.maxParticles = 32;
            var emission = aura.emission; emission.rateOverTime = 12f;
            var shape = aura.shape; shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = motor != null ? motor.radius * 1.2f : .5f;
            var overLife = aura.colorOverLifetime; overLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(0f, 1f) });
            overLife.color = gradient;
            auraTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[256];
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                float alpha = Mathf.Clamp01(1f - new Vector2((x - 7.5f) / 7.5f, (y - 7.5f) / 7.5f).magnitude);
                pixels[y * 16 + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
            auraTexture.SetPixels(pixels); auraTexture.Apply();
            auraMaterial = new Material(shader);
            auraMaterial.SetTexture("_BaseMap", auraTexture);
            auraMaterial.SetFloat("_Surface", 1f);
            auraMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            auraMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            auraMaterial.SetFloat("_ZWrite", 0f);
            auraMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            auraMaterial.renderQueue = 3000;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = auraMaterial;
            aura.Play();
        }

        private void OnDestroy()
        {
            foreach (var material in dissolveMaterials) if (material != null) Destroy(material);
            if (auraMaterial != null) Destroy(auraMaterial);
            if (auraTexture != null) Destroy(auraTexture);
        }
    }
}
