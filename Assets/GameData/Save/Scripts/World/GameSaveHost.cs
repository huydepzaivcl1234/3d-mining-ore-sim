using UnityEngine;

namespace MiningSimulator.Ores
{
    [DefaultExecutionOrder(32000)]
    public sealed class GameSaveHost : MonoBehaviour
    {
        private float nextSave;
        private string reportedError;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("Game Save System").AddComponent<GameSaveHost>();
            DontDestroyOnLoad(host.gameObject);
        }
        private void Update()
        {
            if (Time.unscaledTime < nextSave) return;
            nextSave = Time.unscaledTime + 1f;
            FlushAndReport();
        }
        private void FlushAndReport()
        {
            MonsterSpawnZone.CaptureWorldsIfDue();
            GameSave.Flush();
            string error = GameSave.Store.LastError;
            if (error != null && error != reportedError) Debug.LogError("Game save: " + error);
            reportedError = error;
        }
        private void OnApplicationPause(bool paused) { if (paused) {MonsterSpawnZone.CaptureWorldsIfDue(force:true);FlushAndReport();} }
        private void OnApplicationFocus(bool focused) { if (!focused) {MonsterSpawnZone.CaptureWorldsIfDue(force:true);FlushAndReport();} }
        private void OnApplicationQuit() { MonsterSpawnZone.CaptureWorldsIfDue(true);FlushAndReport(); }
        private void OnDestroy() { FlushAndReport(); }
    }
}
