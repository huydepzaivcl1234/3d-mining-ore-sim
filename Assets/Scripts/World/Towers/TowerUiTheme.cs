using TMPro;
using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed class TowerUiTheme : ScriptableObject
    {
        public TMP_FontAsset titleFont, bodyFont;
    }
    internal static class TowerFantasyUi
    {
        internal static readonly Color Gold=new Color32(237,202,123,255), Cream=new Color32(255,244,218,255),
            Background=new Color32(43,28,39,255), Cell=new Color32(36,29,44,255), Teal=new Color32(24,178,163,255), Critical=new Color32(246,98,91,255);
        static TowerUiTheme theme;
        static Sprite rounded;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()=>theme=null;
        internal static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
        }
        internal static UnityEngine.UI.Image Box(RectTransform rect,Color color,float radius=10)
        {
            if(rounded==null)
            {
                var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.name="Tower UI rounded shape";texture.wrapMode=TextureWrapMode.Clamp;
                var pixels=new Color[4096];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {float dx=Mathf.Max(0,12-Mathf.Min(x+.5f,63.5f-x));float dy=Mathf.Max(0,12-Mathf.Min(y+.5f,63.5f-y));pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(12.5f-Mathf.Sqrt(dx*dx+dy*dy)));}
                texture.SetPixels(pixels);texture.Apply(false,true);
                rounded=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
            }
            var g=rect.gameObject.AddComponent<UnityEngine.UI.Image>();g.sprite=rounded;g.type=UnityEngine.UI.Image.Type.Sliced;g.pixelsPerUnitMultiplier=radius>0?12/radius:1;g.color=color;g.raycastTarget=false;return g;
        }
        internal static RectTransform Frame(string name,Transform parent,Vector2 size,Vector2 position,Color fill,float radius=10)
        {
            var r=Rect(name,parent,size,position);Box(r,Gold,radius);
            Box(Rect("Surface",r,size-Vector2.one*2,Vector2.zero),fill,Mathf.Max(0,radius-1));return r;
        }
        internal static TMP_Text Label(Transform parent,string name,Vector2 position,Vector2 size,float font,Color color,bool title=false,TextAlignmentOptions align=TextAlignmentOptions.Left)
        {
            theme??=Resources.Load<TowerUiTheme>("TowerUiTheme");
            var t=Rect(name,parent,size,position).gameObject.AddComponent<TextMeshProUGUI>();
            var f=theme!=null?(title?theme.titleFont:theme.bodyFont):null;if(f!=null)t.font=f;
            t.fontSize=font;t.color=color;t.alignment=align;t.raycastTarget=false;return t;
        }
        internal static UnityEngine.UI.Image HealthFill(Transform parent,Vector2 size,Vector2 position)
        {
            var frame=Frame("HealthTrack",parent,size,position,Background,4);
            var clip=Rect("Clip",frame,size-Vector2.one*6,Vector2.zero);clip.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var r=Rect("HealthFill",clip,clip.sizeDelta,Vector2.zero);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,.5f);r.anchoredPosition=Vector2.zero;
            var fill=r.gameObject.AddComponent<UnityEngine.UI.Image>();fill.color=Teal;fill.raycastTarget=false;return fill;
        }
        internal static void SetHealth(UnityEngine.UI.Image fill,float fraction)
        {fill.rectTransform.sizeDelta=new Vector2(((RectTransform)fill.transform.parent).sizeDelta.x*Mathf.Clamp01(fraction),fill.rectTransform.sizeDelta.y);fill.color=fraction<=.2f?Critical:Teal;}
    }
}
