using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Ores
{
    /// <summary>World-camera frustum clipping only: offscreen enemies remain simulated.</summary>
    [DefaultExecutionOrder(1000), DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class MiningRenderDistance : MonoBehaviour
    {
        private Camera view;
        private float originalDistance;
        private static float playerDistance=-1;
        public static void SetPlayerDistance(float distance)=>playerDistance=distance;
        public static void ClearPlayerDistance()=>playerDistance=-1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHooks(){SceneManager.sceneLoaded-=SceneLoaded;playerDistance=-1;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;
            Attach();
        }
        private static void SceneLoaded(Scene scene,LoadSceneMode mode)=>Attach();
        private static void Attach()
        {
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(camera.CompareTag("MainCamera")&&!camera.GetComponent<MiningRenderDistance>())
                    camera.gameObject.AddComponent<MiningRenderDistance>();
        }
        private void OnEnable(){view=GetComponent<Camera>();originalDistance=view.farClipPlane;LateUpdate();}
        private void LateUpdate()
        {
            var data=GameplayPerformanceData.Current;
            float requested=playerDistance>0?playerDistance:data?data.renderDistance:0;
            float distance=requested>0?Mathf.Max(view.nearClipPlane+.01f,requested):originalDistance;
            if(!Mathf.Approximately(view.farClipPlane,distance))view.farClipPlane=distance;
            WorldActorVisibility.Cull(view, data);
        }
        private void OnDisable(){if(view)view.farClipPlane=originalDistance;WorldActorVisibility.RestoreAll();}
    }
}
