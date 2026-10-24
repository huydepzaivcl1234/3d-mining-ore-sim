using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Ores.Editor
{
    public static class MosswoodSceneCapture
    {
        // Render the installed geometry without entering gameplay or touching the save/clock.
        public static string Capture()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only.");
            var scene=SceneManager.GetActiveScene();
            var roots=scene.GetRootGameObjects();
            var map=roots.First(r=>r.GetComponent<ForestMapLighting>()!=null);
            var clock=roots.SelectMany(r=>r.GetComponentsInChildren<DayNightSystem>()).First();
            var settings=new SerializedObject(clock);
            var data=(DayNightData)settings.FindProperty("data").objectReferenceValue;
            var palette=new SerializedObject(data);
            var sun=(Light)settings.FindProperty("sun").objectReferenceValue;
            if(sun==null)sun=roots.SelectMany(r=>r.GetComponentsInChildren<Light>()).First(l=>l.type==LightType.Directional);
            var guide=map.GetComponent<ForestMapLighting>().guide;
            var oldColor=sun.color;float oldIntensity=sun.intensity,oldGuide=guide.intensity;
            var oldRotation=sun.transform.rotation;var oldMode=RenderSettings.ambientMode;
            var oldAmbient=RenderSettings.ambientLight;bool oldFog=RenderSettings.fog;var oldFogMode=RenderSettings.fogMode;
            var oldFogColor=RenderSettings.fogColor;float oldFogDensity=RenderSettings.fogDensity;
            var cameraObject=new GameObject("Mosswood capture (temporary)");
            SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            camera.transform.position=map.transform.position+new Vector3(18,16,-24);
            camera.transform.LookAt(map.transform.position+new Vector3(0,1,5));
            camera.fieldOfView=55;camera.farClipPlane=180;camera.useOcclusionCulling=false;
            camera.scene=scene;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags=CameraClearFlags.SolidColor;
            try
            {
                Directory.CreateDirectory("Temp/MosswoodCaptures");
                for(int night=0;night<2;night++)
                {
                    string prefix=night==0?"day":"night";
                    sun.color=palette.FindProperty(prefix+"SunColor").colorValue;
                    sun.intensity=palette.FindProperty(prefix+"SunIntensity").floatValue;
                    sun.transform.rotation=Quaternion.Euler(night==0?new Vector3(38,-30,0):new Vector3(35,-30,0));
                    guide.intensity=night==0?.15f:map.GetComponent<ForestMapLighting>().nightIntensity;
                    RenderSettings.ambientMode=AmbientMode.Flat;
                    RenderSettings.ambientLight=palette.FindProperty(prefix+"AmbientColor").colorValue;
                    RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Exponential;
                    RenderSettings.fogColor=night==0?new Color(.53f,.61f,.57f):new Color(.12f,.20f,.29f);
                    RenderSettings.fogDensity=night==0?.005f:.006f;
                    camera.backgroundColor=RenderSettings.fogColor;
                    var rt=RenderTexture.GetTemporary(1600,1000,24);var previous=RenderTexture.active;
                    Texture2D image=null;
                    try
                    {
                        camera.targetTexture=rt;camera.Render();camera.Render();camera.Render();RenderTexture.active=rt;
                        image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                        image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
                        File.WriteAllBytes("Temp/MosswoodCaptures/Applied"+(night==0?"Day":"Night")+".png",image.EncodeToPNG());
                    }
                    finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
                }
                return "Captured installed map: Temp/MosswoodCaptures/AppliedDay.png and AppliedNight.png";
            }
            finally
            {
                sun.color=oldColor;sun.intensity=oldIntensity;sun.transform.rotation=oldRotation;guide.intensity=oldGuide;
                RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldAmbient;
                RenderSettings.fog=oldFog;RenderSettings.fogMode=oldFogMode;RenderSettings.fogColor=oldFogColor;RenderSettings.fogDensity=oldFogDensity;
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
