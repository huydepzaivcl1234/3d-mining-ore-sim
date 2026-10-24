using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed class TowerWorldSaveHost : MonoBehaviour
    {
        private float nextFlush;
        public static void Ensure(TreasureChest chest)
        { if (chest.GetComponent<TowerWorldSaveHost>() == null) chest.gameObject.AddComponent<TowerWorldSaveHost>(); }
        private void Start() => TowerWorldSave.Restore(gameObject.scene);
        private void OnDisable() => TowerWorldSave.Flush();
        private void Update()
        { if (Time.unscaledTime >= nextFlush) { nextFlush = Time.unscaledTime + 1f; TowerWorldSave.Restore(gameObject.scene); TowerWorldSave.Flush(); } }
        private void OnApplicationPause(bool pause) { if (pause) TowerWorldSave.Flush(); }
        private void OnApplicationQuit() => TowerWorldSave.Flush();
    }
}
