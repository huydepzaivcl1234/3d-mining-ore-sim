using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    // One small live rendering of the imported particle prefab is shared by both button ends.
    public sealed class UpgradeSparkVfx : MonoBehaviour
    {
        private readonly List<RawImage> views=new();
        private GameObject stage;
        private Camera view;
        private RenderTexture texture;
        public void Refresh(UnityEngine.UI.Button[] buttons,IReadOnlyList<GameData.Player.Script.SurvivorUpgradeData.Offer> offers,MiningCardData settings)
        {
            if(!settings||!settings.sparkleVfx||!settings.sparkleComposite)return;
            if(!stage)Build(settings);
            for(int i=0;i<buttons.Length;i++){
                if(!buttons[i])continue;
                var parent=(RectTransform)buttons[i].transform;
                for(int side=0;side<2;side++){
                    string name=side==0?"Anime sparkle — left":"Anime sparkle — right";
                    var child=parent.Find(name) as RectTransform;
                    if(!child){child=new GameObject(name,typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();child.SetParent(parent,false);}
                    child.anchorMin=child.anchorMax=new Vector2(side,.5f);child.pivot=new Vector2(.5f,.5f);
                    child.anchoredPosition=new Vector2(side==0?settings.sparkleInset:-settings.sparkleInset,0);child.sizeDelta=Vector2.one*settings.sparkleSize;child.SetAsLastSibling();
                    var image=child.GetComponent<RawImage>();image.texture=texture;image.material=settings.sparkleComposite;image.raycastTarget=false;image.maskable=false;
                    if(!views.Contains(image))views.Add(image);
                    child.gameObject.SetActive(i<offers.Count&&offers[i].shiny);
                }
            }
        }
        private void Build(MiningCardData settings)
        {
            stage=new GameObject("UI sparkle preview (runtime)"){hideFlags=HideFlags.HideAndDontSave};stage.transform.position=new Vector3(0,-50000,0);
            var source=Instantiate(settings.sparkleVfx,stage.transform);source.transform.localPosition=Vector3.zero;
            foreach(var renderer in source.GetComponentsInChildren<Renderer>()){renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}
            foreach(var particle in source.GetComponentsInChildren<ParticleSystem>()){
                particle.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=particle.main;main.loop=true;main.useUnscaledTime=true;main.stopAction=ParticleSystemStopAction.None;main.maxParticles=Mathf.Min(main.maxParticles,32);
                particle.Play(false);
            }
            var cameraObject=new GameObject("Particle preview camera");cameraObject.transform.SetParent(stage.transform,false);cameraObject.transform.localPosition=new Vector3(0,0,-5);
            view=cameraObject.AddComponent<Camera>();view.enabled=false;view.orthographic=true;view.orthographicSize=1.3f;view.nearClipPlane=.1f;view.farClipPlane=12;
            view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=Color.clear;view.allowHDR=true;view.allowMSAA=false;view.useOcclusionCulling=false;
            texture=new RenderTexture(128,128,16,RenderTextureFormat.ARGBHalf){name="Anime star UI preview"};texture.Create();view.targetTexture=texture;
        }
        private void LateUpdate()
        {
            if(!stage)return;
            bool visible=false;foreach(var image in views)if(image&&image.gameObject.activeInHierarchy){visible=true;break;}
            if(stage.activeSelf!=visible)stage.SetActive(visible);
            if(visible)view.Render();
        }
        private void OnDisable(){if(stage)stage.SetActive(false);}
        private void OnDestroy(){if(view)view.targetTexture=null;if(texture){texture.Release();Destroy(texture);}if(stage)Destroy(stage);}
    }
}
