using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChestDefense.Fps.Editor
{
    public static class FpsSightChecks
    {
        public static string Align()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            foreach(var name in new[]{"Pistol","Rifle"})
            {
                bool rifle=name=="Rifle";
                var data=AssetDatabase.LoadAssetAtPath<FpsWeaponData>("Assets/ChestDefense/FPS/Generated/"+name+".asset");
                var path=AssetDatabase.GetAssetPath(data.viewPrefab);
                var go=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var view=go.GetComponent<FpsGunView>();
                    data.idle.SampleAnimation(view.rig.gameObject,0);
                    go.transform.SetPositionAndRotation(data.aimPosition,Quaternion.Euler(data.aimEuler));
                    Transform bone=null;
                    foreach(var t in view.rig.GetComponentsInChildren<Transform>(true))
                        if(t.name==(rifle?"Root":"Slide")){bone=t;break;}
                    Vector3 rear=rifle?new Vector3(.0084f,-.019f,.473f):new Vector3(-.005f,.026f,.400f);
                    Vector3 front=rifle?new Vector3(.0084f,-.010f,.854f):new Vector3(-.005f,.026f,.560f);
                    Vector3 up=rifle?Vector3.up:new Vector3(-.3f,.954f,0);
                    if(view.rearSight!=null && view.frontSight!=null)
                    {
                        rear=view.rearSight.position; front=view.frontSight.position;
                        up=view.rearSight.up;
                    }
                    else
                    {
                        view.rearSight=Anchor(bone,"RearSight",rear);
                        view.frontSight=Anchor(bone,"FrontSight",front);
                        view.rearSight.rotation=Quaternion.LookRotation(front-rear,up);
                    }
                    Quaternion correction=Quaternion.Inverse(Quaternion.LookRotation(front-rear,up));
                    Undo.RecordObject(data,"Align iron sights");
                    data.alignIronSights=true;data.aimEyeRelief=rifle?.14f:.17f;
                    data.aimEuler=(correction*go.transform.rotation).eulerAngles;
                    data.aimPosition=correction*go.transform.position+new Vector3(0,0,rifle?.14f:.17f)-correction*rear;
                    go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    PrefabUtility.SaveAsPrefabAsset(go,path);
                    EditorUtility.SetDirty(data); AssetDatabase.SaveAssetIfDirty(data);
                }
                finally{PrefabUtility.UnloadPrefabContents(go);}
            }
            return "Aligned rear/front sights; hip poses and weapon balance unchanged.";
        }
        static Transform Anchor(Transform parent,string name,Vector3 point)
        {
            var anchor=parent.Find(name);
            if(anchor==null) anchor=new GameObject(name).transform;
            anchor.SetParent(parent,true); anchor.position=point;
            return anchor;
        }
        public static string Capture()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var original = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var active = RenderTexture.active;
            try
            {
                var camera = new GameObject("Sight camera", typeof(Camera)).GetComponent<Camera>();
                camera.fieldOfView = 48; camera.nearClipPlane = .01f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.32f,.4f,.46f);
                camera.cullingMask = 1 << 30;
                var light = new GameObject("Sight light", typeof(Light)).GetComponent<Light>();
                light.type = LightType.Directional; light.intensity = 2; light.transform.rotation = Quaternion.Euler(35,-35,0);
                foreach (var name in new[] { "Pistol", "Rifle" })
                {
                    var data = AssetDatabase.LoadAssetAtPath<FpsWeaponData>("Assets/ChestDefense/FPS/Generated/"+name+".asset");
                    var view = UnityEngine.Object.Instantiate(data.viewPrefab);
                    view.Initialize(data, null); view.Tick(true, 10);
                    var meshes = new System.Collections.Generic.List<Mesh>();
                    foreach (var skin in view.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        if (!skin.enabled) continue;
                        var mesh = new Mesh(); skin.BakeMesh(mesh); meshes.Add(mesh);
                        var frozen = new GameObject("Frozen pose", typeof(MeshFilter),typeof(MeshRenderer));
                        frozen.layer=30; frozen.transform.SetParent(view.transform,true);
                        frozen.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);
                        frozen.transform.localScale=Vector3.one;
                        frozen.GetComponent<MeshFilter>().sharedMesh=mesh;
                        frozen.GetComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                        skin.enabled=false;
                    }
                    var rt = new RenderTexture(960,540,24);
                    var image = new Texture2D(960,540,TextureFormat.RGB24,false);
                    try
                    {
                        camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                        image.ReadPixels(new Rect(0,0,960,540),0,0); image.Apply();
                        System.IO.File.WriteAllBytes("Docs/AI/"+name+"IronSights.png",image.EncodeToPNG());
                    }
                    finally { camera.targetTexture=null; RenderTexture.active=active; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(view.gameObject); foreach(var mesh in meshes) UnityEngine.Object.DestroyImmediate(mesh); }
                }
                return "Captured both ADS views in an isolated scene.";
            }
            finally { RenderTexture.active=active; EditorSceneManager.CloseScene(scene,true); SceneManager.SetActiveScene(original); }
        }
    }
}
