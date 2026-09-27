using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiningSimulator.Ores.Editor
{
    public static class WeaponVfxSetup
    {
        private const string Folder = "Assets/FX/WeaponFeedback";
        [MenuItem("Mining Simulator/Setup/Create And Assign Weapon VFX %&#v")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Exit Play Mode before creating VFX."); return; }
            var shader = Shader.Find("Mining Simulator/Weapon Glow");
            if (shader == null) { Debug.LogError("Weapon Glow shader missing; import the shader first."); return; }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/SoftGlow.asset");
            if (texture == null)
            {
                texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "SoftGlow", wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float r = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).magnitude;
                    texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - r), 1.7f)));
                }
                texture.Apply();
                AssetDatabase.CreateAsset(texture, Folder + "/SoftGlow.asset");
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/WeaponGlow.mat");
            if (material == null)
            {
                material = new Material(shader) { name = "WeaponGlow" };
                material.SetTexture("_MainTex", texture);
                AssetDatabase.CreateAsset(material, Folder + "/WeaponGlow.mat");
            }
            var punch = Prefab("PunchStrike", false, false, new Color(1, 0.62f, 0.16f), material);
            var punchImpact = Prefab("PunchImpact", true, false, new Color(1, 0.72f, 0.22f), material);
            var sword = Prefab("SwordStrike", false, true, new Color(0.35f, 0.78f, 1), material);
            var swordImpact = Prefab("SwordImpact", true, true, new Color(0.45f, 0.85f, 1), material);
            Assign("Fists", punch, punchImpact);
            Assign("Sword", sword, swordImpact);
            Selection.activeObject = punch;
            Debug.Log("WEAPON VFX READY: editable Punch/Sword strike and impact prefabs in " + Folder + ". No scene or Animator changes.");
        }
        private static void Assign(string name, GameObject strike, GameObject impact)
        {
            var data = AssetDatabase.LoadAssetAtPath<WeaponAttackData>("Assets/GameData/Weapons/" + name + ".asset");
            if (data == null) { Debug.LogWarning("Create Fists And Sword Data first."); return; }
            Undo.RecordObject(data, "Assign weapon VFX");
            // Keep an authored replacement; only fill missing references.
            if (data.strikeVfxPrefab == null) data.strikeVfxPrefab = strike;
            if (data.impactVfxPrefab == null) data.impactVfxPrefab = impact;
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
        }
        private static GameObject Prefab(string name, bool impact, bool sword, Color tint, Material material)
        {
            string path = Folder + "/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject(name);
            try
            {
                var feedback = root.AddComponent<WeaponStrikeVfx>();
                feedback.impact = impact;
                feedback.sword = sword;
                feedback.duration = impact ? 0.24f : sword ? 0.22f : 0.16f;
                feedback.radius = impact ? 0.28f : sword ? 2.5f : 1.75f;
                for (int i = 0; i < 2; i++)
                {
                    var obj = new GameObject(i == 0 ? "Soft glow" : "White hot core");
                    obj.transform.SetParent(root.transform, false);
                    var line = obj.AddComponent<LineRenderer>();
                    line.sharedMaterial = material;
                    line.useWorldSpace = false;
                    line.positionCount = impact ? 49 : 41;
                    line.loop = impact;
                    line.numCapVertices = 4;
                    line.numCornerVertices = 4;
                    line.widthMultiplier = i == 0 ? (impact ? 0.06f : 0.22f) : (impact ? 0.018f : 0.045f);
                    line.widthCurve = impact ? AnimationCurve.Linear(0, 1, 1, 1) : new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.35f, 1), new Keyframe(0.85f, 0.6f), new Keyframe(1, 0));
                    line.startColor = line.endColor = i == 0 ? tint : new Color(1, 0.98f, 0.9f);
                    line.shadowCastingMode = ShadowCastingMode.Off;
                    line.receiveShadows = false;
                    for (int p = 0; p < line.positionCount; p++)
                    {
                        float f = p / (float)(line.positionCount - 1);
                        float a = (impact ? f * 360 : Mathf.Lerp(-55, 55, f)) * Mathf.Deg2Rad;
                        line.SetPosition(p, impact ? new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * feedback.radius : sword ? new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * feedback.radius : Vector3.forward * feedback.radius * f);
                    }
                }
                if (impact)
                {
                    var obj = new GameObject("Contact sparks");
                    obj.transform.SetParent(root.transform, false);
                    var particles = obj.AddComponent<ParticleSystem>();
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = particles.main;
                    main.loop = false;
                    main.duration = 0.3f;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
                    main.startColor = tint;
                    main.maxParticles = 20;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.playOnAwake = true;
                    var emission = particles.emission;
                    emission.rateOverTime = 0;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0, 12) });
                    var shape = particles.shape;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 70;
                    shape.radius = 0.025f;
                    var size = particles.sizeOverLifetime;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
                    var renderer = particles.GetComponent<ParticleSystemRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.renderMode = ParticleSystemRenderMode.Stretch;
                    renderer.lengthScale = 2;
                    renderer.velocityScale = 0.12f;
                }
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
