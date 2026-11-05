using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Ores
{
    /// <summary>Reset gameplay and reload the authored scene, including every transient runtime object.</summary>
    public static class WorldProgressReset
    {
        public static bool IsResetting {get;private set;}
        public static bool ContinueAfterReload {get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize(){SceneManager.sceneLoaded-=Reloaded;IsResetting=false;ContinueAfterReload=false;}
        public static bool ResetAll(bool continueGame=false)
        {
            if(IsResetting)return false;
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying&&SceneUtility.GetBuildIndexByScenePath(scene.path)<0){Debug.LogError("Reset requires the current gameplay scene in Build Settings; save preserved.");return false;}
            if(!GameSave.ResetAll()){Debug.LogError("Cannot reset save files. Progress preserved: "+GameSave.Store.LastError);return false;}
            if(!Application.isPlaying)return true;
            if(continueGame){GameSave.SetInt("MiningSimulator.SaveExists.v1",1);if(!GameSave.Flush())return false;}
            IsResetting=true;ContinueAfterReload=continueGame;GameSave.SuppressWrites=true;
            TowerWorldSave.ResetAllProgress();
            MonsterSpawnZone.ClearResetSchedules();
            RunCheckpoint.IsRestoring=false;
            SceneManager.sceneLoaded+=Reloaded;
            Time.timeScale=1;AudioListener.pause=false;
            try{if(SceneManager.LoadSceneAsync(scene.path,LoadSceneMode.Single)==null)throw new System.InvalidOperationException("Scene reload did not start");}
            catch(System.Exception e){Reloaded(default,LoadSceneMode.Single);Debug.LogError("Save was reset, but scene reload failed: "+e.Message);return false;}
            return true;
        }
        static void Reloaded(Scene scene,LoadSceneMode mode)
        {
            SceneManager.sceneLoaded-=Reloaded;
            GameSave.SuppressWrites=false;IsResetting=false;ContinueAfterReload=false;
        }
    }
}
