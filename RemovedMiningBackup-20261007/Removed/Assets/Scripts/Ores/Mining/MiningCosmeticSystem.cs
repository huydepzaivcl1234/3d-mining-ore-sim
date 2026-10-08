using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Stores owned tool skins and applies the equipped skin to every miner.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningCosmeticSystem : MonoBehaviour
    {
        private const string SaveKey = "MiningSimulator.OwnedCosmetics.v1";

        [Serializable] private sealed class CosmeticSave { public List<string> owned = new(); public string equipped; }

        private readonly HashSet<string> owned = new(StringComparer.Ordinal);
        private readonly Dictionary<MiningNpc, GameObject> appliedVisuals = new();
        private CosmeticSave save;
        private MiningCosmeticData equipped;

        public event Action Changed;

        private void Awake()
        {
            Load();
        }

        private void Update()
        {
            if (equipped != null) ApplyToAllMiners();
        }

        public bool IsOwned(MiningCosmeticData cosmetic) => cosmetic != null && owned.Contains(cosmetic.CosmeticId);

        /// <summary>Registers a cosmetic offered by the current shop so a saved equip can be restored.</summary>
        public void Register(MiningCosmeticData cosmetic)
        {
            if (cosmetic == null || equipped != null || !IsOwned(cosmetic) ||
                !string.Equals(save.equipped, cosmetic.CosmeticId, StringComparison.Ordinal)) return;
            equipped = cosmetic;
            ApplyToAllMiners();
        }

        public bool TryPurchaseAndEquip(MiningCosmeticData cosmetic)
        {
            if (cosmetic == null) return false;
            owned.Add(cosmetic.CosmeticId);
            Equip(cosmetic);
            return true;
        }

        public void Equip(MiningCosmeticData cosmetic)
        {
            if (!IsOwned(cosmetic)) return;
            equipped = cosmetic;
            save.equipped = cosmetic.CosmeticId;
            Save();
            ApplyToAllMiners();
            Changed?.Invoke();
        }

        private void Load()
        {
            save = new CosmeticSave();
            if (PlayerPrefs.HasKey(SaveKey))
            {
                try { save = JsonUtility.FromJson<CosmeticSave>(PlayerPrefs.GetString(SaveKey)) ?? save; }
                catch (ArgumentException) { save = new CosmeticSave(); }
            }
            foreach (string id in save.owned)
                if (!string.IsNullOrWhiteSpace(id)) owned.Add(id);
        }

        private void Save()
        {
            save.owned = new List<string>(owned);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        private void ApplyToAllMiners()
        {
            MiningNpc[] miners = FindObjectsByType<MiningNpc>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (MiningNpc miner in miners) ApplyToMiner(miner);
            foreach (MiningNpc miner in new List<MiningNpc>(appliedVisuals.Keys))
                if (miner == null || Array.IndexOf(miners, miner) < 0) appliedVisuals.Remove(miner);
        }

        private void ApplyToMiner(MiningNpc miner)
        {
            if (miner == null || equipped == null || appliedVisuals.ContainsKey(miner)) return;
            Transform pivot = miner.ToolPivot;
            if (pivot == null) return;
            GameObject root = new($"{equipped.DisplayName} Skin");
            root.transform.SetParent(pivot, false);
            root.transform.localPosition = equipped.ToolLocalPosition;
            root.transform.localRotation = Quaternion.Euler(equipped.ToolLocalEulerAngles);
            root.transform.localScale = equipped.ToolLocalScale;

            if (equipped.ToolPrefab != null)
            {
                Instantiate(equipped.ToolPrefab, root.transform, false);
            }
            else
            {
                foreach (Renderer renderer in pivot.GetComponentsInChildren<Renderer>(true))
                {
                    MaterialPropertyBlock properties = new();
                    renderer.GetPropertyBlock(properties);
                    properties.SetColor("_BaseColor", equipped.Tint);
                    properties.SetColor("_Color", equipped.Tint);
                    renderer.SetPropertyBlock(properties);
                }
            }

            ParticleSystem particles = equipped.ParticlePrefab != null
                ? Instantiate(equipped.ParticlePrefab, root.transform, false)
                : CreateFallbackParticles(root.transform);
            if (particles != null) particles.Play(true);
            appliedVisuals.Add(miner, root);
        }

        private static ParticleSystem CreateFallbackParticles(Transform parent)
        {
            GameObject root = new("Golden Sparkles");
            root.transform.SetParent(parent, false);
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.startColor = new Color(1f, 0.75f, 0.12f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 9f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            return particles;
        }
    }
}
