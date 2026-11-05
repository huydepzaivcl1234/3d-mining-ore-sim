using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    // Native uGUI paint for the Figma fills; icons remain the original imported SVGs.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UpgradeSurface : MaskableGraphic
    {
        public Color top, bottom, stroke;
        public float radius=16, border=1;
        public void Paint(Color a, Color b, Color edge, float r=16, float width=1)
        {top=a;bottom=b;stroke=edge;radius=r;border=width;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=GetPixelAdjustedRect();if(rect.width<=0||rect.height<=0)return;
            int count=36;float r=Mathf.Min(radius,Mathf.Min(rect.width,rect.height)*.5f);
            vh.AddVert(rect.center,Color.Lerp(bottom,top,.5f),Vector2.zero);
            for(int i=0;i<count;i++){
                Vector2 p=Edge(rect,r,i);vh.AddVert(p,Color.Lerp(bottom,top,Mathf.InverseLerp(rect.yMin,rect.yMax,p.y)),Vector2.zero);
            }
            for(int i=0;i<count;i++)vh.AddTriangle(0,1+i,1+(i+1)%count);
            if(border<=0||stroke.a<=0)return;
            var inner=new Rect(rect.xMin+border,rect.yMin+border,Mathf.Max(0,rect.width-2*border),Mathf.Max(0,rect.height-2*border));
            int start=vh.currentVertCount;
            for(int i=0;i<count;i++){vh.AddVert(Edge(rect,r,i),stroke,Vector2.zero);vh.AddVert(Edge(inner,Mathf.Max(0,r-border),i),stroke,Vector2.zero);}
            for(int i=0;i<count;i++){int a=start+2*i,b=start+2*((i+1)%count);vh.AddTriangle(a,b,a+1);vh.AddTriangle(a+1,b,b+1);}
        }
        static Vector2 Edge(Rect rect,float r,int i)
        {
            int corner=i/9;float angle=(corner*90+i%9*90f/8)*Mathf.Deg2Rad;
            var center=new Vector2(corner==0||corner==3?rect.xMax-r:rect.xMin+r,corner<2?rect.yMax-r:rect.yMin+r);
            return center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r;
        }
    }
}
