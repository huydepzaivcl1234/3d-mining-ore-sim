#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using NUnit.Framework;

namespace MiningSimulator.Ores.Tests
{
    public sealed class GameSaveTests
    {
        private string directory, path;
        [SetUp] public void Setup()
        {
            directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ChestDefense-SaveTest-" + Guid.NewGuid().ToString("N"));
            path = System.IO.Path.Combine(directory, "profile.json");
        }
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        [Test] public void NewProfileRoundTripsIndependentSections()
        {
            var save = new GameSaveStore(path);
            Assert.IsTrue(save.LegacyAllowed);
            save.Set("wallet", "123.5"); save.Set("inventory", "{\"version\":4,\"items\":[1,2]}");
            Assert.IsTrue(save.Flush());
            var loaded = new GameSaveStore(path);
            Assert.AreEqual("123.5", loaded.Get("wallet", ""));
            Assert.AreEqual(save.Get("inventory", ""), loaded.Get("inventory", ""));
            Assert.IsFalse(loaded.Dirty);
        }
        [Test] public void DeletedSectionDoesNotResurrectLegacyValue()
        {
            var save = new GameSaveStore(path); save.Delete("old-key"); save.Flush();
            var loaded = new GameSaveStore(path);
            Assert.IsTrue(loaded.IsKnown("old-key")); Assert.IsFalse(loaded.Contains("old-key"));
        }
        [Test] public void CorruptPrimaryRecoversPreviousCompleteBackup()
        {
            var save = new GameSaveStore(path); save.Set("gold", "10"); save.Flush();
            save.Set("gold", "20"); save.Flush(); File.WriteAllText(path, "broken");
            var recovered = new GameSaveStore(path);
            Assert.IsTrue(recovered.Writable); Assert.AreEqual("10", recovered.Get("gold", ""));
            Assert.IsTrue(recovered.Flush()); Assert.AreEqual("10", new GameSaveStore(path).Get("gold", ""));
        }
        [Test] public void UnrecoverableCorruptionNeverOverwritesOriginal()
        {
            Directory.CreateDirectory(directory); File.WriteAllText(path, "broken");
            var save = new GameSaveStore(path); save.Set("gold", "100");
            Assert.IsFalse(save.Flush()); Assert.AreEqual("broken", File.ReadAllText(path));
        }
        [Test] public void FutureVersionNeverDowngradesToBackup()
        {
            var save = new GameSaveStore(path); save.Set("a", "1"); save.Flush(); save.Set("a", "2"); save.Flush();
            File.WriteAllText(path, "{\"version\":99}");
            var future = new GameSaveStore(path);
            Assert.IsFalse(future.Writable); Assert.IsFalse(future.Flush());
            Assert.AreEqual("{\"version\":99}", File.ReadAllText(path));
        }
        [Test] public void UnknownSectionsSurviveKnownSectionUpdates()
        {
            var save = new GameSaveStore(path); save.Set("future-monster", "opaque-data"); save.Flush();
            var loaded = new GameSaveStore(path); loaded.Set("wallet", "7"); loaded.Flush();
            Assert.AreEqual("opaque-data", new GameSaveStore(path).Get("future-monster", ""));
        }
        [Test] public void ExplicitResetArchivesAndDisablesLegacyMigration()
        {
            var save = new GameSaveStore(path); save.Set("gold", "10"); save.Flush();
            Assert.IsTrue(save.Reset());
            var reset = new GameSaveStore(path);
            Assert.IsFalse(reset.LegacyAllowed); Assert.IsFalse(reset.Contains("gold"));
            Assert.AreEqual(1, Directory.GetFiles(directory, "*.reset-*").Length);
        }
        [Test] public void InterruptedFirstWriteRecoversValidatedTemp()
        {
            var save = new GameSaveStore(path); save.Set("gold", "42"); save.Flush(); File.Move(path, path + ".tmp");
            var loaded = new GameSaveStore(path);
            Assert.AreEqual("42", loaded.Get("gold", "")); Assert.IsTrue(loaded.Flush()); Assert.IsTrue(File.Exists(path));
        }
        [Test] public void WriteFailureRetainsDirtyDataForRetry()
        {
            Directory.CreateDirectory(directory); File.WriteAllText(System.IO.Path.Combine(directory, "blocker"), "file");
            var save = new GameSaveStore(System.IO.Path.Combine(directory, "blocker", "profile.json"));
            save.Set("gold", "55"); Assert.IsFalse(save.Flush()); Assert.IsTrue(save.Dirty);
        }
        [Test] public void LegacyPrefsMigrateWithoutDeletingOriginalAndTombstonesWin()
        {
            string prefix = "SaveTests." + Guid.NewGuid().ToString("N");
            var field = typeof(GameSave).GetField("testStore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            try
            {
                UnityEngine.PlayerPrefs.SetInt(prefix + ".int", 12);
                UnityEngine.PlayerPrefs.SetFloat(prefix + ".float", 3.5f);
                UnityEngine.PlayerPrefs.SetString(prefix + ".json", "{\"version\":4}");
                var isolated = new GameSaveStore(path); field.SetValue(null, isolated);
                Assert.AreEqual(12, GameSave.GetInt(prefix + ".int"));
                Assert.AreEqual(3.5f, GameSave.GetFloat(prefix + ".float"));
                Assert.AreEqual("{\"version\":4}", GameSave.GetString(prefix + ".json"));
                Assert.IsTrue(GameSave.Flush());
                Assert.IsTrue(UnityEngine.PlayerPrefs.HasKey(prefix + ".int"));
                GameSave.DeleteKey(prefix + ".int"); GameSave.Flush();
                field.SetValue(null, new GameSaveStore(path));
                Assert.IsFalse(GameSave.HasKey(prefix + ".int"));
                Assert.AreEqual(0, GameSave.GetInt(prefix + ".int"));
                Assert.IsTrue(GameSave.ResetAll());
                Assert.AreEqual(0, GameSave.GetFloat(prefix + ".float"));
            }
            finally
            {
                field.SetValue(null, null);
                foreach (string suffix in new[] { ".int", ".float", ".json" }) UnityEngine.PlayerPrefs.DeleteKey(prefix + suffix);
            }
        }
    }
}
#endif
