using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>One main save object, composed of independently versioned subsystem DTOs.</summary>
    public sealed class GameSaveStore
    {
        [Serializable] public sealed class Entry { public string key, value; public bool deleted; }
        [Serializable] private sealed class Document
        {
            public int version = 1;
            public bool legacyAllowed = true;
            public List<Entry> sections = new List<Entry>();
        }
        [Serializable] private sealed class Envelope
        {
            public int version = 1;
            public string payload, checksum;
        }
        private Document document = new Document();
        private readonly Dictionary<string, Entry> index = new Dictionary<string, Entry>();
        public string Path { get; }
        public bool Writable { get; private set; } = true;
        public bool Dirty { get; private set; }
        public bool LegacyAllowed => document.legacyAllowed;
        public string LastError { get; private set; }
        private bool recovered;

        public GameSaveStore(string path)
        {
            Path = path;
            Load();
        }
        public bool Contains(string key) => index.TryGetValue(key, out var entry) && !entry.deleted;
        public bool IsKnown(string key) => index.ContainsKey(key);
        public string Get(string key, string fallback) => Contains(key) ? index[key].value : fallback;
        public void Set(string key, string value)
        {
            if (!Writable) return;
            if (Contains(key) && index[key].value == value) return;
            Put(new Entry { key = key, value = value });
        }
        public void Delete(string key)
        {
            if (!Writable || index.TryGetValue(key, out var old) && old.deleted) return;
            Put(new Entry { key = key, deleted = true });
        }
        private void Put(Entry entry)
        {
            if (string.IsNullOrEmpty(entry.key)) throw new ArgumentException("Save section requires a key.");
            if (index.TryGetValue(entry.key, out var old)) document.sections.Remove(old);
            document.sections.Add(entry);
            index[entry.key] = entry;
            Dirty = true;
        }
        private static string Digest(string text)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
        private bool TryRead(string path, out Document result, out bool future)
        {
            result = null; future = false;
            try
            {
                var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
                if (envelope == null) return false;
                future = envelope.version > 1;
                if (future || envelope.version != 1 || string.IsNullOrEmpty(envelope.payload) ||
                    envelope.checksum != Digest(envelope.payload)) return false;
                result = JsonUtility.FromJson<Document>(envelope.payload);
                future = result != null && result.version > 1;
                if (result == null || result.version != 1 || result.sections == null) return false;
                var keys = new HashSet<string>();
                foreach (var entry in result.sections)
                    if (entry == null || string.IsNullOrEmpty(entry.key) || !keys.Add(entry.key) ||
                        !entry.deleted && entry.value == null) return false;
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { LastError = ex.Message; return false; }
        }
        private void Load()
        {
            bool any = false;
            foreach (string candidate in new[] { Path, Path + ".bak", Path + ".tmp" })
            {
                if (!File.Exists(candidate)) continue;
                any = true;
                if (TryRead(candidate, out var loaded, out bool future))
                {
                    document = loaded;
                    foreach (var entry in document.sections) index.Add(entry.key, entry);
                    recovered = candidate != Path;
                    Dirty = recovered;
                    LastError = recovered ? "Recovered save from " + candidate : null;
                    return;
                }
                // Never downgrade a newer save to an older backup.
                if (future) { Writable = false; LastError = "Save belongs to a newer version; writing blocked."; return; }
            }
            if (any) { Writable = false; LastError = "No valid save or backup. Original files preserved; writing blocked."; }
        }
        public bool Flush()
        {
            if (!Writable) return false;
            if (!Dirty) return true;
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                string payload = JsonUtility.ToJson(document);
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Envelope { payload = payload, checksum = Digest(payload) }, true));
                using (var stream = new FileStream(Path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(Path)) File.Replace(Path + ".tmp", Path, recovered ? null : Path + ".bak");
                else File.Move(Path + ".tmp", Path);
                recovered = false; Dirty = false; LastError = null;
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { LastError = ex.Message; return false; }
        }
        /// <summary>Only called by explicit New Game/reset. Archive all old files before replacing them.</summary>
        public bool Reset()
        {
            try
            {
                string suffix = ".reset-" + Guid.NewGuid().ToString("N");
                foreach (string file in new[] { Path, Path + ".bak", Path + ".tmp" })
                    if (File.Exists(file)) File.Move(file, file + suffix);
                document = new Document { legacyAllowed = false };
                index.Clear(); Writable = true; Dirty = true; recovered = false;
                return Flush();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { Writable = false; LastError = ex.Message; return false; }
        }
    }

    /// <summary>Compatibility boundary: gameplay uses JSON; graphics/audio/keybind preferences stay separate.</summary>
    public static class GameSave
    {
        private static GameSaveStore store;
#if UNITY_EDITOR
        // Injected only by isolated Editor tests; never touches the real profile.
        private static GameSaveStore testStore;
        private static bool UseFiles => Application.isPlaying || testStore != null;
        private static GameSaveStore ExistingStore => testStore ?? store;
#else
        private static bool UseFiles => Application.isPlaying;
        private static GameSaveStore ExistingStore => store;
#endif
        public static string FilePath => System.IO.Path.Combine(Application.persistentDataPath, "Saves", "profile.json");
        public static GameSaveStore Store => ExistingStore ?? (store = new GameSaveStore(FilePath));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearRuntime() { store = null; }
        private static bool Legacy(string key) => Store.Writable && Store.LegacyAllowed && !Store.IsKnown(key) && PlayerPrefs.HasKey(key);
        public static bool HasKey(string key) => !UseFiles ? PlayerPrefs.HasKey(key) : Store.Contains(key) || Legacy(key);
        public static string GetString(string key, string fallback = "")
        {
            if (!UseFiles) return PlayerPrefs.GetString(key, fallback);
            if (Legacy(key)) Store.Set(key, PlayerPrefs.GetString(key, fallback));
            return Store.Get(key, fallback);
        }
        public static int GetInt(string key, int fallback = 0)
        {
            if (!UseFiles) return PlayerPrefs.GetInt(key, fallback);
            if (Legacy(key)) Store.Set(key, PlayerPrefs.GetInt(key, fallback).ToString(CultureInfo.InvariantCulture));
            return int.TryParse(Store.Get(key, ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        }
        public static float GetFloat(string key, float fallback = 0)
        {
            if (!UseFiles) return PlayerPrefs.GetFloat(key, fallback);
            if (Legacy(key)) Store.Set(key, PlayerPrefs.GetFloat(key, fallback).ToString("R", CultureInfo.InvariantCulture));
            return float.TryParse(Store.Get(key, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) &&
                !float.IsNaN(value) && !float.IsInfinity(value) ? value : fallback;
        }
        public static void SetString(string key, string value) { if (UseFiles) Store.Set(key, value); else PlayerPrefs.SetString(key, value); }
        public static void SetInt(string key, int value) { if (UseFiles) Store.Set(key, value.ToString(CultureInfo.InvariantCulture)); else PlayerPrefs.SetInt(key, value); }
        public static void SetFloat(string key, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            if (UseFiles) Store.Set(key, value.ToString("R", CultureInfo.InvariantCulture)); else PlayerPrefs.SetFloat(key, value);
        }
        public static void DeleteKey(string key) { if (UseFiles) Store.Delete(key); else PlayerPrefs.DeleteKey(key); }
        // Existing callers request persistence; the host coalesces writes, rather than blocking each transaction.
        public static void Save() { if (!UseFiles) PlayerPrefs.Save(); }
        public static bool Flush() => ExistingStore == null || ExistingStore.Flush();
        public static bool ResetAll() => !UseFiles || Store.Reset();
    }
}
