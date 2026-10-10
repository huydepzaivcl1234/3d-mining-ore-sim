using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class TowerPlacementRecord
    {
        public string id, towerId, scene;
        public Vector3 position;
        public float yaw, health, paidPrice;
        public bool IsValid => !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(towerId) &&
            !string.IsNullOrEmpty(scene) && Finite(position.x) && Finite(position.y) && Finite(position.z) &&
            Finite(yaw) && Finite(health) && health > 0 && Finite(paidPrice) && paidPrice >= 0;
        private static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
    }

    [Serializable]
    public sealed class TowerSaveDocument
    {
        public int version = 1;
        public List<TowerPlacementRecord> towers = new();
        public static bool TryRead(string json, out TowerSaveDocument document)
        {
            document = null;
            try
            {
                var parsed = JsonUtility.FromJson<TowerSaveDocument>(json);
                if (parsed == null || parsed.version != 1 || parsed.towers == null) return false;
                var ids = new HashSet<string>();
                foreach (var record in parsed.towers)
                    if (record == null || !record.IsValid || !ids.Add(record.id)) return false;
                document = parsed;
                return true;
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>Only committed placements are saved, never previews or fixture instances.</summary>
    public static class TowerWorldSave
    {
        private static string Key = "ChestDefense.PlacedTowers.v1";
        private static TowerSaveDocument document;
        private static bool writable;
        private static readonly Dictionary<string, TowerRuntime> loaded = new();
        private static readonly HashSet<string> warned = new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        { Key = "ChestDefense.PlacedTowers.v1"; document = null; writable = false; loaded.Clear(); warned.Clear(); }

        private static void Load()
        {
            if (document != null) return;
            document = new TowerSaveDocument();
            writable = !GameSave.HasKey(Key);
            if (!writable)
            {
                writable = TowerSaveDocument.TryRead(GameSave.GetString(Key), out var parsed);
                if (writable) document = parsed;
                else Debug.LogError("Placed tower save is invalid or newer than supported. Original data preserved; saving blocked until Reset Data.");
            }
        }
        private static string SceneKey(Scene scene) => string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
        public static void Register(TowerRuntime tower)
        {
            if (tower == null || tower.Data == null || !tower.IsAlive) return;
            Load();
            if (!writable) return;
            string id = string.IsNullOrEmpty(tower.PlacementId) ? Guid.NewGuid().ToString("N") : tower.PlacementId;
            tower.MarkDeployed(id); loaded[id] = tower;
            Capture(tower); Flush();
        }
        public static void Capture(TowerRuntime tower)
        {
            if (tower == null || string.IsNullOrEmpty(tower.PlacementId)) return;
            Load();
            if (!writable || tower.Data == null) return;
            if (tower.Health == null || tower.Health.Health <= 0) { Remove(tower); return; }
            var record = new TowerPlacementRecord { id = tower.PlacementId, towerId = tower.Data.towerId,
                scene = SceneKey(tower.gameObject.scene), position = tower.transform.position,
                yaw = tower.transform.eulerAngles.y, health = tower.Health.Health, paidPrice = tower.PaidPrice };
            if (!record.IsValid) return;
            int index = document.towers.FindIndex(x => x.id == record.id);
            if (index < 0) document.towers.Add(record); else document.towers[index] = record;
        }
        public static void Remove(TowerRuntime tower)
        {
            if (tower == null || string.IsNullOrEmpty(tower.PlacementId)) return;
            Load();
            document.towers.RemoveAll(x => x.id == tower.PlacementId);
            loaded.Remove(tower.PlacementId); tower.MarkDeployed(null);
            Flush();
        }
        public static void Flush()
        {
            if (document == null || !writable) return;
            // Do not delete entries because their scene unloaded; snapshot before disabling.
            foreach (var tower in new List<TowerRuntime>(loaded.Values))
                if (tower != null && tower.Health != null && tower.Health.Health > 0) Capture(tower);
            string json = JsonUtility.ToJson(document);
            if (GameSave.GetString(Key, "") == json) return;
            GameSave.SetString(Key, json); GameSave.Save();
        }
        public static void Restore(Scene scene)
        {
            Load();
            string sceneKey = SceneKey(scene);
            if (!writable) return;
            var catalog = Resources.Load<TowerCatalog>("TowerCatalog");
            if (catalog == null) { if(warned.Add("catalog"))Debug.LogWarning("Tower catalog missing: saved placements retained."); return; }
            Physics.SyncTransforms();
            foreach (var record in document.towers.ToArray())
            {
                if (record.scene != sceneKey || loaded.TryGetValue(record.id, out var existing) && existing != null) continue;
                TowerData data = Array.Find(catalog.towers, x => x != null && x.towerId == record.towerId);
                if (data == null || data.prefab == null) { if(warned.Add(record.id))Debug.LogWarning("Unknown saved tower " + record.towerId + ": retained for a later load."); continue; }
                var foot = record.position;
                var rotation = Quaternion.Euler(0, record.yaw, 0);
                var body = data.prefab.GetComponent<BoxCollider>();
                if (body == null || !TowerPlacementGeometry.ValidateRestore(ref foot, body, rotation))
                { if(warned.Add(record.id))Debug.LogWarning("Saved tower placement is blocked/unsupported: " + record.id + ". Will retry when clear; record retained."); continue; }
                var go = UnityEngine.Object.Instantiate(data.prefab, foot, rotation);
                SceneManager.MoveGameObjectToScene(go, scene);
                var tower = go.GetComponent<TowerRuntime>();
                tower.Initialize(data, record.paidPrice); tower.Health.RestoreSavedHealth(record.health);
                tower.AlignToGround(); tower.MarkDeployed(record.id); loaded[record.id] = tower;
                Physics.SyncTransforms();
            }
        }
        public static void ResetAllProgress()
        {
            // Clear identities first so teardown cannot re-create the deleted save.
            foreach (var tower in new List<TowerRuntime>(loaded.Values))
                if (tower != null) { tower.MarkDeployed(null); tower.gameObject.SetActive(false); UnityEngine.Object.Destroy(tower.gameObject); }
            loaded.Clear(); document = new TowerSaveDocument(); writable = true;
            GameSave.DeleteKey(Key); GameSave.Save();
        }
    }

}
