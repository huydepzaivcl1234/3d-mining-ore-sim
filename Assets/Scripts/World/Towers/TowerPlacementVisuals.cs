using UnityEngine;
namespace MiningSimulator.Ores
{
    internal sealed class TowerPlacementVisuals
    {
        readonly GameObject root,ghost;
        readonly Material material;
        readonly LineRenderer footprint,range;
        readonly BoxCollider body;
        readonly TowerData data;
        readonly TMPro.TMP_Text remainingLabel;
        int shownCount=-1;
        Vector3 last=Vector3.positiveInfinity;
        public TowerPlacementVisuals(TowerData definition,Transform player,Camera camera)
        {
            data=definition;body=data.prefab.GetComponent<BoxCollider>();root=new GameObject("Tower placement overlays");
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            material.SetColor("_BaseColor",new Color(.2f,1,.65f,.45f));
            ghost=Object.Instantiate(data.prefab,root.transform);ghost.name="Cannon ghost";
            foreach(var s in ghost.GetComponentsInChildren<MonoBehaviour>(true))s.enabled=false;
            foreach(var a in ghost.GetComponentsInChildren<Animator>(true))a.enabled=false;
            foreach(var c in ghost.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(var r in ghost.GetComponentsInChildren<Renderer>(true)){var m=new Material[r.sharedMaterials.Length];for(int i=0;i<m.Length;i++)m[i]=material;r.sharedMaterials=m;}
            ghost.AddComponent<TowerStatsPanel>().BindPreview(data,player,camera);
            var canvasObject=new GameObject("Placement quantity",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
            canvasObject.transform.SetParent(root.transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
            var scaler=canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var panel=TowerFantasyUi.Frame("Quantity",canvas.transform,new Vector2(420,86),Vector2.zero,TowerFantasyUi.Background);
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,0);panel.anchoredPosition=new Vector2(0,32);
            var icon=TowerFantasyUi.Rect("Icon",panel,new Vector2(48,48),new Vector2(-170,10)).gameObject.AddComponent<UnityEngine.UI.Image>();icon.sprite=data.icon;icon.preserveAspect=true;icon.raycastTarget=false;
            remainingLabel=TowerFantasyUi.Label(panel,"Remaining",new Vector2(28,12),new Vector2(330,32),24,TowerFantasyUi.Cream,true);
            TowerFantasyUi.Label(panel,"Hint",new Vector2(0,-22),new Vector2(398,25),15,TowerFantasyUi.Gold,false,TMPro.TextAlignmentOptions.Center).text="Click to place • Right click / Esc to finish";
            footprint=Line("Occupied cells",5,.06f);range=Line("Attack range",129,.04f);
            int cx=Mathf.RoundToInt(player.position.x),cz=Mathf.RoundToInt(player.position.z);
            for(int i=-12;i<=12;i++)
            {
                var x=Line("Grid X",25,.015f);var z=Line("Grid Z",25,.015f);
                x.startColor=x.endColor=z.startColor=z.endColor=new Color(1,1,1,.3f);
                for(int j=-12;j<=12;j++){x.SetPosition(j+12,Surface(new Vector3(cx+i,player.position.y,cz+j)));z.SetPosition(j+12,Surface(new Vector3(cx+j,player.position.y,cz+i)));}
            }
        }
        LineRenderer Line(string name,int count,float width)
        {var l=new GameObject(name).AddComponent<LineRenderer>();l.transform.SetParent(root.transform);l.sharedMaterial=material;l.useWorldSpace=true;l.positionCount=count;l.widthMultiplier=width;return l;}
        static Vector3 Surface(Vector3 p){if(TowerPlacementGeometry.GroundBelow(p,out var g))p=g;return p+Vector3.up*.045f;}
        public void Move(Vector3 p,bool valid)
        {
            ghost.SetActive(true);ghost.transform.position=p;material.SetColor("_BaseColor",valid?new Color(.2f,1,.65f,.45f):new Color(1,.22f,.2f,.45f));
            if((p-last).sqrMagnitude<.0001f)return;last=p;
            var cells=TowerPlacementGeometry.FootprintCells(body,1);var center=p+new Vector3(body.center.x,0,body.center.z);
            var o=new[]{new Vector3(-cells.x*.5f,0,-cells.y*.5f),new Vector3(cells.x*.5f,0,-cells.y*.5f),new Vector3(cells.x*.5f,0,cells.y*.5f),new Vector3(-cells.x*.5f,0,cells.y*.5f)};
            for(int i=0;i<5;i++)footprint.SetPosition(i,Surface(center+o[i%4]));
            for(int i=0;i<129;i++){float a=i*2*Mathf.PI/128;range.SetPosition(i,Surface(p+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*data.range));}
        }
        public void Hide(){ghost.SetActive(false);footprint.enabled=range.enabled=false;}
        public void Show(){footprint.enabled=range.enabled=true;}
        public void SetRemaining(int count){if(count==shownCount)return;shownCount=count;remainingLabel.text=data.displayName+"  x"+count;}
        public void Dispose(){Object.Destroy(root);Object.Destroy(material);}
    }
}
