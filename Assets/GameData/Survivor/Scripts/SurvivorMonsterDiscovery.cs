using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    // Only visual meshes enter the preview. Never instantiate an enemy prefab with live AI.
    public sealed class SurvivorMonsterDiscovery : MonoBehaviour
    {
        private static SurvivorMonsterDiscovery current;
        private readonly HashSet<(Object,bool)> seen=new();
        private readonly Queue<(GameObject visual,string name)> waiting=new();
        private readonly List<Mesh> baked=new();
        private RectTransform panel;
        private CanvasGroup fade;
        private RawImage image;
        private TMP_Text label;
        private Camera previewCamera;
        private RenderTexture texture;
        private GameObject activeVisual;
        private float elapsed;
        public int AnnouncementsShown {get;private set;}
        public int PendingCount=>waiting.Count;
        public float PreviewAngle=>activeVisual?activeVisual.transform.eulerAngles.y:0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession(){current=null;}
        public static void AnnouncePrefab(MushroomMonster prefab,MonsterSpawnEntry entry)
        {
            if(!prefab)return;
            Announce(prefab,prefab,string.IsNullOrWhiteSpace(entry.displayName)?prefab.name:entry.displayName,false);
        }
        public static void Announce(MushroomMonster monster,MonsterSpawnEntry entry,bool boss)
        {
            if(!monster||entry==null||!entry.prefab)return;
            string name=string.IsNullOrWhiteSpace(entry.displayName)?entry.prefab.name:entry.displayName;
            Announce(monster,entry.prefab,name,boss);
        }
        public static void Announce(Component monster,Object identity,string name,bool boss)
        {
            if(!monster||!identity)return;
            if(!current)current=new GameObject("Survivor — Monster Discovery").AddComponent<SurvivorMonsterDiscovery>();
            if(!current.seen.Add((identity,boss)))return;
            current.StartCoroutine(current.PrepareVisual(monster,identity,name,boss));
        }
        private System.Collections.IEnumerator PrepareVisual(Component monster,Object identity,string name,bool boss)
        {
            // Let the newly spawned Animator evaluate before baking its visible pose.
            yield return null;
            var visual=monster?CopyVisual(monster):null;
            if(!visual){seen.Remove((identity,boss));yield break;}
            waiting.Enqueue((visual,boss?"BOSS · "+name:name));
        }
        private GameObject CopyVisual(Component source)
        {
            var root=new GameObject("Discovery visual");root.transform.position=new Vector3(10000,10000,10000);
            bool found=false;Bounds bounds=default;var visibility=source.GetComponent<WorldActorVisibility>();
            foreach(var renderer in source.GetComponentsInChildren<Renderer>())
            {
                if(!(visibility?visibility.RendererWasEnabled(renderer):renderer.enabled)||(source.gameObject.scene.IsValid()?!renderer.gameObject.activeInHierarchy:!renderer.gameObject.activeSelf)||renderer.GetComponent<TMP_Text>())continue;
                Mesh mesh=null;
                if(renderer is SkinnedMeshRenderer skin){mesh=new Mesh();skin.BakeMesh(mesh);mesh.RecalculateBounds();baked.Add(mesh);}
                else if(renderer is MeshRenderer){var filter=renderer.GetComponent<MeshFilter>();if(filter)mesh=filter.sharedMesh;}
                if(!mesh||mesh.bounds.size.sqrMagnitude<.000001f)continue;
                var part=new GameObject(renderer.name,typeof(MeshFilter),typeof(MeshRenderer));part.layer=31;
                part.transform.SetParent(root.transform,false);
                part.transform.localPosition=source.transform.InverseTransformPoint(renderer.transform.position);
                part.transform.localRotation=Quaternion.Inverse(source.transform.rotation)*renderer.transform.rotation;
                var scale=renderer.transform.lossyScale;float s=source.transform.lossyScale.x;part.transform.localScale=scale/Mathf.Max(.001f,Mathf.Abs(s));
                part.GetComponent<MeshFilter>().sharedMesh=mesh;
                var copy=part.GetComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;copy.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;copy.receiveShadows=false;
                var local=Matrix4x4.TRS(part.transform.localPosition,part.transform.localRotation,part.transform.localScale);
                var box=mesh.bounds;
                for(int corner=0;corner<8;corner++)
                {
                    var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                    var point=local.MultiplyPoint3x4(box.center+Vector3.Scale(box.extents,sign));
                    if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
                }
            }
            if(!found){Destroy(root);return null;}
            Vector3 center=bounds.center;
            foreach(Transform child in root.transform)child.localPosition-=center;
            root.transform.localScale=Vector3.one*(2/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
            root.SetActive(false);return root;
        }
        private void Build()
        {
            var canvas=new GameObject("Monster discovery canvas",typeof(Canvas),typeof(CanvasScaler));
            canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=80;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            panel=new GameObject("New monster",typeof(RectTransform),typeof(Image),typeof(CanvasGroup)).GetComponent<RectTransform>();panel.SetParent(canvas.transform,false);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(1,1);panel.anchoredPosition=new Vector2(-28,-112);panel.sizeDelta=new Vector2(294,190);
            panel.GetComponent<Image>().color=new Color(.055f,.08f,.095f,.94f);fade=panel.GetComponent<CanvasGroup>();fade.blocksRaycasts=false;fade.interactable=false;
            image=new GameObject("Rotating model",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();image.transform.SetParent(panel,false);image.rectTransform.anchorMin=new Vector2(0,.22f);image.rectTransform.anchorMax=Vector2.one;image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;image.raycastTarget=false;
            label=new GameObject("Monster name",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();label.transform.SetParent(panel,false);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=new Vector2(1,.24f);label.rectTransform.offsetMin=new Vector2(10,4);label.rectTransform.offsetMax=new Vector2(-10,-2);label.font=Resources.Load<TMP_FontAsset>("SurvivorRoboto");label.fontSize=20;label.enableAutoSizing=true;label.fontSizeMin=15;label.fontSizeMax=20;label.overflowMode=TextOverflowModes.Ellipsis;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.94f,.85f,.62f);label.raycastTarget=false;
            texture=new RenderTexture(384,256,24,RenderTextureFormat.ARGB32);texture.Create();image.texture=texture;
            previewCamera=new GameObject("Discovery render camera",typeof(Camera)).GetComponent<Camera>();previewCamera.transform.SetParent(transform,false);previewCamera.transform.position=new Vector3(10000,10000,9996);previewCamera.transform.LookAt(new Vector3(10000,10000,10000));previewCamera.orthographic=true;previewCamera.orthographicSize=1.28f;previewCamera.nearClipPlane=.1f;previewCamera.farClipPlane=10;previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=Color.clear;previewCamera.cullingMask=1<<31;previewCamera.targetTexture=texture;previewCamera.enabled=false;
            var light=new GameObject("Preview light",typeof(Light));light.transform.SetParent(transform,false);light.transform.position=new Vector3(9998,10003,9997);var l=light.GetComponent<Light>();l.type=LightType.Point;l.range=10;l.intensity=16;l.shadows=LightShadows.None;l.cullingMask=1<<31;
            panel.gameObject.SetActive(false);
        }
        private void Update()
        {
            if(!panel)Build();
            // Do not consume discovery announcements while an upgrade/menu pauses gameplay.
            if(Time.timeScale<=0){panel.gameObject.SetActive(false);return;}
            if(!activeVisual&&waiting.Count>0){var next=waiting.Dequeue();activeVisual=next.visual;label.text="QUÁI MỚI · "+next.name;elapsed=0;activeVisual.SetActive(true);AnnouncementsShown++;}
            if(!activeVisual){panel.gameObject.SetActive(false);return;}
            elapsed+=Time.unscaledDeltaTime;panel.gameObject.SetActive(true);
            activeVisual.transform.rotation=Quaternion.Euler(0,360*Mathf.Clamp01(elapsed),0);
            fade.alpha=Mathf.Min(Mathf.Clamp01(elapsed/.1f),Mathf.Clamp01((1-elapsed)/.12f));
            previewCamera.Render();
            if(elapsed>=1){Destroy(activeVisual);activeVisual=null;panel.gameObject.SetActive(false);ReleaseUnusedMeshes();}
        }
        private void ReleaseUnusedMeshes(){if(waiting.Count==0){foreach(var mesh in baked)if(mesh)Destroy(mesh);baked.Clear();}}
        private void OnDestroy(){if(current==this)current=null;if(activeVisual)Destroy(activeVisual);foreach(var item in waiting)if(item.visual)Destroy(item.visual);foreach(var mesh in baked)if(mesh)Destroy(mesh);if(texture){texture.Release();Destroy(texture);}}
    }
}
