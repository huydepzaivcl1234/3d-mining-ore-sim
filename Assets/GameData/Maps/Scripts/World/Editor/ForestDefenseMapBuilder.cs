using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace MiningSimulator.Ores.Editor
{
    /// <summary>Builds a separate prefab in an isolated preview scene; never saves the current scene.</summary>
    public static class ForestDefenseMapBuilder
    {
        public const string Output = "Assets/GameData/Maps/Mosswood";
        private const string Vendor = "Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/";
        private static GameObject Child(GameObject parent,string name)
        { var o = new GameObject(name); o.transform.SetParent(parent.transform,false); return o; }
        [MenuItem("Chest Defense/Map/Build Mosswood Prefab (no scene edits)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            Directory.CreateDirectory(Output); AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Mosswood Defense Map"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            try
            {
                var ground = Child(root,"Optional Ground - disable over existing Terrain"); MosswoodTerrainAuthoring.CreatePreviewGround(ground);
                var decoration = Child(root,"Forest - LOD and distance culling");
                root.AddComponent<ForestMapVisibility>().decoration = decoration.transform;
                var markers = Child(root,"Layout Markers - no gameplay duplicates");
                var guideObject = Child(root,"Chest Amber Guide - no bloom blanket");
                guideObject.transform.localPosition = new Vector3(0,1.8f,0);
                var guide = guideObject.AddComponent<Light>();
                guide.type = LightType.Point; guide.color = new Color(1,.69f,.32f);
                guide.range = 13; guide.intensity = .15f; guide.shadows = LightShadows.None;
                var lighting = root.AddComponent<ForestMapLighting>(); lighting.guide = guide; lighting.nightIntensity = 9;
                Child(markers,"Chest Anchor"); Child(markers,"Respawn Anchor").transform.localPosition = new Vector3(0,0,9);
                for (int i=0;i<4;i++)
                {
                    float a=i*Mathf.PI/2;
                    Child(markers,"Approach "+i+" - 8m open lane").transform.localPosition=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*30;
                    Child(markers,"Tower Pad "+i).transform.localPosition=new Vector3(Mathf.Sin(a+.785f),0,Mathf.Cos(a+.785f))*7;
                }
                var random = new System.Random(10417);
                for (int i=0;i<100;i++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2;
                    Vector3 p=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*(25+(float)random.NextDouble()*18);
                    if (Mathf.Abs(p.x)<6||Mathf.Abs(p.z)<6||ReservedPlot(p)) continue;
                    Place("Trees/Fir/Prefabs/UNS_Spruce_0"+(i%2+1)+".prefab",decoration,p,.65f+(float)random.NextDouble()*.4f,random,true);
                }
                for (int i=0;i<36;i++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2;
                    Vector3 p=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*(16+(float)random.NextDouble()*24);
                    if (Mathf.Abs(p.x)<6||Mathf.Abs(p.z)<6||ReservedPlot(p)) continue;
                    Place("Rocks/Forest/Prefabs/UNS_Standard_Rock_0"+(i%5+1)+".prefab",decoration,p,.7f,random,false);
                    Place("Vegetation/Bushes/Prefabs/UNS_Bush.prefab",decoration,p+new Vector3(2,0,1),.7f,random,false);
                }
                for (int i=0;i<180;i++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2;
                    Vector3 p=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*(14+(float)random.NextDouble()*25);
                    if(IsPath(p.x,p.z)||Mathf.Abs(p.x)<6||Mathf.Abs(p.z)<6||ReservedPlot(p))continue;
                    Place("Vegetation/Grass/Prefabs/UNS_Grass.prefab",decoration,p,.6f+(float)random.NextDouble()*.5f,random,false);
                }
                PrefabUtility.SaveAsPrefabAsset(root,Output+"/MosswoodDefenseMap.prefab");
                BuildLightingData();
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        private static Material Tint(Material source,Color tint)
        {
            if(source==null)return null;
            string path=Output+"/"+source.name+"_"+ColorUtility.ToHtmlStringRGB(tint)+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path); if(m!=null)return m;
            m=new Material(source); if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",source.GetColor("_BaseColor")*tint);
            m.enableInstancing=true; AssetDatabase.CreateAsset(m,path); return m;
        }
        private static void Place(string suffix,GameObject parent,Vector3 p,float scale,System.Random random,bool tree)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Vendor+suffix); if(asset==null)throw new FileNotFoundException(Vendor+suffix);
            var o=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent.transform);
            p.y=Height(p.x,p.z);
            o.transform.localPosition=p; o.transform.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0); o.transform.localScale=Vector3.one*scale;
            foreach(var r in o.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials=r.sharedMaterials.Select(m=>Tint(m,tree?new Color(.72f,.78f,.64f):new Color(.65f,.72f,.74f))).ToArray();
            foreach(var c in o.GetComponentsInChildren<Collider>(true))c.enabled=false;
            if(tree){var c=o.AddComponent<CapsuleCollider>();c.radius=.32f;c.height=3;c.center=Vector3.up*1.5f;}
            else if(suffix.Contains("Rock"))
            {
                var f=o.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(x=>x.name.Contains("LOD0"));
                if(f!=null){var c=o.AddComponent<BoxCollider>();c.center=o.transform.InverseTransformPoint(f.transform.TransformPoint(f.sharedMesh.bounds.center));c.size=Vector3.Scale(f.sharedMesh.bounds.size,f.transform.localScale);}
            }
        }
        private static void Ground(GameObject parent,bool paths)
        {
            var v=new System.Collections.Generic.List<Vector3>(); var t=new System.Collections.Generic.List<int>();
            for(int z=-45;z<45;z++)for(int x=-45;x<45;x++)
            {
                bool path=IsPath(x+.5f,z+.5f);if(path!=paths)continue;
                int s=v.Count;v.Add(new Vector3(x,Height(x,z),z));v.Add(new Vector3(x,Height(x,z+1),z+1));v.Add(new Vector3(x+1,Height(x+1,z+1),z+1));v.Add(new Vector3(x+1,Height(x+1,z),z));
                t.AddRange(new[]{s,s+1,s+2,s,s+2,s+3});
            }
            string name=paths?"Earth Paths":"Moss Clearing"; string pathAsset=Output+"/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(pathAsset);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,pathAsset);}
            mesh.Clear();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            mesh.SetUVs(0,v.Select(p => new Vector2(p.x,p.z)*.15f).ToList());
            var o=Child(parent,name);
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) o.layer = groundLayer;
            o.AddComponent<MeshFilter>().sharedMesh=mesh;
            var m=AssetDatabase.LoadAssetAtPath<Material>(Output+"/"+name+".mat");if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,Output+"/"+name+".mat");}
            m.SetColor("_BaseColor",paths?new Color(.75f,.68f,.55f):new Color(.65f,.75f,.5f));m.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(m);
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Vendor+"Terrain/Textures/UNS_Terrain_"+(paths?"Dirt":"Grass")+".png"));
            o.AddComponent<MeshRenderer>().sharedMaterial=m;o.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        private static bool IsPath(float x,float z) => x*x+z*z<36 ||
            Mathf.Abs(x-Mathf.Sin(z*.12f)*1.5f)<4 || Mathf.Abs(z-Mathf.Sin(x*.1f)*1.5f)<4;
        private static bool ReservedPlot(Vector3 p) =>
            (p-new Vector3(19.65f,0,24.38f)).sqrMagnitude<49 ||
            (p-new Vector3(-13.5f,0,19.3f)).sqrMagnitude<49;
        private static float Height(float x,float z)
        {
            float edge=Mathf.Clamp01((Mathf.Max(Mathf.Abs(x),Mathf.Abs(z))-31)/14);
            return edge*edge*(1.5f+Mathf.PerlinNoise(x*.09f+10,z*.09f+10)*2);
        }
        public static void BuildLightingData()
        {
            const string source = "Assets/GameData/DayNight/DayNightData.asset";
            string target = Output+"/MosswoodLighting.asset";
            if (AssetDatabase.LoadAssetAtPath<DayNightData>(target)==null) AssetDatabase.CopyAsset(source,target);
            var data = AssetDatabase.LoadAssetAtPath<DayNightData>(target);
            if(data==null)throw new FileNotFoundException(source);
            var serialized = new SerializedObject(data);
            serialized.FindProperty("daySunColor").colorValue=new Color(1,.9f,.76f);
            serialized.FindProperty("nightSunColor").colorValue=new Color(.45f,.65f,1);
            serialized.FindProperty("daySunIntensity").floatValue=1.35f;
            serialized.FindProperty("nightSunIntensity").floatValue=.65f;
            serialized.FindProperty("nightSunRotation").vector3Value=new Vector3(35,-30,0);
            serialized.FindProperty("nightSunArcDegrees").floatValue=10;
            serialized.FindProperty("nightAmbientColor").colorValue=new Color(.23f,.29f,.4f);
            serialized.FindProperty("nightFogDensity").floatValue=.003f;
            serialized.FindProperty("dayBloomIntensity").floatValue=.12f;
            serialized.FindProperty("nightBloomIntensity").floatValue=.15f;
            serialized.FindProperty("bloomThreshold").floatValue=1.2f;
            serialized.FindProperty("goldenHourBloomBoost").floatValue=.08f;
            serialized.FindProperty("nightPostExposure").floatValue=.05f;
            serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(data);
        }
        [MenuItem("Chest Defense/Map/Capture Day and Night (isolated)")]
        public static void Capture()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/MosswoodDefenseMap.prefab"),scene);
                var co=new GameObject("Preview Camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(co,scene);
                var camera=co.AddComponent<Camera>();camera.scene=scene;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.enabled=false;camera.transform.position=new Vector3(18,16,-24);camera.transform.LookAt(new Vector3(0,1,5));camera.fieldOfView=55;camera.clearFlags=CameraClearFlags.SolidColor;
                var so=new GameObject("Preview Sun");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(so,scene);var sun=so.AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(45,-30,0);
                RenderModel("Assets/GameData/Base/BaseTreasureChest.prefab",scene,Vector3.zero);
                RenderModel("Assets/GameData/Tower/Cannon/CannonTower.prefab",scene,new Vector3(5,0,-4));
                var guideObject=new GameObject("Preview Amber");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(guideObject,scene);guideObject.transform.position=new Vector3(0,3,0);var guide=guideObject.AddComponent<Light>();guide.type=LightType.Point;guide.color=new Color(1,.69f,.32f);guide.range=13;
                Directory.CreateDirectory("Temp/MosswoodCaptures");
                for(int night=0;night<2;night++)
                {
                    sun.color=night==0?new Color(1,.88f,.72f):new Color(.45f,.65f,1);sun.intensity=night==0?1.5f:.7f;guide.intensity=night==0?.15f:3;
                    camera.backgroundColor=night==0?new Color(.47f,.62f,.7f):new Color(.08f,.13f,.22f);
                    var rt=RenderTexture.GetTemporary(1600,1000,24);var previous=RenderTexture.active;
                    try{camera.targetTexture=rt;camera.Render();camera.Render();camera.Render();RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes("Temp/MosswoodCaptures/"+(night==0?"Day":"Night")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
                    finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        private static void RenderModel(string path,UnityEngine.SceneManagement.Scene scene,Vector3 position)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);var root=new GameObject("Reference "+asset.name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=position;
            foreach(var f in asset.GetComponentsInChildren<MeshFilter>(true))
            {
                var r=f.GetComponent<MeshRenderer>();if(r==null)continue;var part=Child(root,f.name);
                part.transform.localPosition=asset.transform.InverseTransformPoint(f.transform.position);part.transform.localRotation=Quaternion.Inverse(asset.transform.rotation)*f.transform.rotation;part.transform.localScale=f.transform.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh=f.sharedMesh;part.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
            }
        }
    }
}
