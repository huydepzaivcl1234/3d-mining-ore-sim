using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Survivor.CrowdPrototype
{
 public static class Crowd10KBaker
 {
 const string Root="Assets/GameData/Survivor/Crowd10K/";
 [MenuItem("Survivor/Experimental/Bake and Open 10K Crowd")]
 public static void Bake()
 {
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/MushroomMonster.prefab");
  var go=UnityEngine.Object.Instantiate(prefab);go.hideFlags=HideFlags.HideAndDontSave;
  try {
   // Only the Animator is needed for sampling; never run source gameplay scripts.
   foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
   var r=go.GetComponentInChildren<SkinnedMeshRenderer>(true); var animator=go.GetComponentInChildren<Animator>(true);
   var copied=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"MushroomLOD.fbx").GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
   var original=r.sharedMesh; if(copied.vertexCount!=original.vertexCount)throw new Exception("LOD vertex layout mismatch");
   var ov=original.vertices;var cv=copied.vertices;
   for(int i=0;i<ov.Length;i++)if((ov[i]-cv[i]).sqrMagnitude>.000001f)throw new Exception("LOD reordered source vertices");
   string[] names={"Idle","Walk","Headbutt","Down"};
   var clips=names.Select(n=>AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Monsters/Mushroom/Clips/SA_Mushroom_"+n+".anim")).ToArray();
   int frames=32,width=Mathf.NextPowerOfTwo(original.vertexCount);
   var pos=new Texture2D(width,frames*clips.Length,TextureFormat.RGBAHalf,false,true){name="Mushroom GPU Positions",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
   var normals=new Texture2D(width,frames*clips.Length,TextureFormat.RGBAHalf,false,true){name="Mushroom GPU Normals",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
   var pp=new Color[width*frames*clips.Length];var nn=new Color[pp.Length];
   var baked=new Mesh();var bounds=new Bounds(Vector3.zero,Vector3.one*4);
   animator.enabled=false;
   // Clip paths are relative to the original Animator, not the prefab root.
   for(int c=0;c<clips.Length;c++)for(int f=0;f<frames;f++) {
    clips[c].SampleAnimation(animator.gameObject,clips[c].length*f/frames);r.BakeMesh(baked);
    var matrix=go.transform.worldToLocalMatrix*r.transform.localToWorldMatrix;
    var verts=baked.vertices;var ns=baked.normals;
    for(int v=0;v<verts.Length;v++){var p=matrix.MultiplyPoint3x4(verts[v]);var n=matrix.MultiplyVector(ns[v]).normalized;int idx=(c*frames+f)*width+v;pp[idx]=new Color(p.x,p.y,p.z,1);nn[idx]=new Color(n.x,n.y,n.z,0);bounds.Encapsulate(p);}
   }
   UnityEngine.Object.DestroyImmediate(baked);
   pos.SetPixels(pp);pos.Apply(false,true);normals.SetPixels(nn);normals.Apply(false,true);
   pos=Save(pos,"Positions.asset");normals=Save(normals,"Normals.asset");
   int[] lods={0,3,5,6};var meshes=new Mesh[lods.Length];var indices=copied.GetIndices(0);var ranges=copied.GetLods(0);
   for(int l=0;l<lods.Length;l++){
    var selected=copied.GetIndices(0,lods[l],true);if(selected.Length<3)throw new Exception("Empty LOD "+l);
    var used=selected.Distinct().ToArray();var remap=used.Select((id,index)=>new{id,index}).ToDictionary(x=>x.id,x=>x.index);
    var mesh=new Mesh{name="Mushroom VAT LOD "+l};mesh.vertices=used.Select(i=>ov[i]).ToArray();mesh.uv=used.Select(i=>original.uv[i]).ToArray();mesh.uv2=used.Select(i=>new Vector2(i,0)).ToArray();mesh.triangles=selected.Select(i=>remap[i]).ToArray();mesh.bounds=bounds;meshes[l]=Save(mesh,"LOD"+l+".asset");
   }
   var mat=new Material(Shader.Find("Survivor/Experimental/CrowdVAT")){enableInstancing=true};mat.SetTexture("_BaseMap",r.sharedMaterial.mainTexture);mat.SetTexture("_PositionTex",pos);mat.SetTexture("_NormalTex",normals);mat=Save(mat,"Crowd.mat");
   var data=AssetDatabase.LoadAssetAtPath<Crowd10KData>(Root+"Crowd10K.asset");if(!data)data=ScriptableObject.CreateInstance<Crowd10KData>();data.name="Crowd10K";data.meshes=meshes;data.positions=pos;data.normals=normals;data.material=mat;data.clipLengths=clips.Select(c=>c.length).ToArray();data=Save(data,"Crowd10K.asset");
   AssetDatabase.SaveAssets();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var manager=new GameObject("GPU Crowd - 10000 structs, no NPC GameObjects").AddComponent<Crowd10K>();manager.data=data;
   var target=GameObject.CreatePrimitive(PrimitiveType.Capsule);target.name="Target - WASD";target.transform.position=Vector3.up;manager.target=target.transform;
   var plane=GameObject.CreatePrimitive(PrimitiveType.Plane);plane.name="Prototype Ground";plane.transform.localScale=Vector3.one*15;
   var groundMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMat.color=new Color(.18f,.24f,.22f);groundMat=Save(groundMat,"Ground.mat");plane.GetComponent<Renderer>().sharedMaterial=groundMat;
   var light=new GameObject("Directional Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-35,0);
   var cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(0,100,-90);cam.transform.LookAt(Vector3.zero);cam.nearClipPlane=.2f;cam.farClipPlane=300;cam.fieldOfView=65;cam.backgroundColor=new Color(.32f,.42f,.48f);cam.clearFlags=CameraClearFlags.SolidColor;manager.view=cam;
   cam.gameObject.AddComponent<AudioListener>();RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;RenderSettings.fog=false;
   EditorSceneManager.SaveScene(scene,Root+"Crowd10K.unity");Selection.activeObject=manager;Debug.Log("10K bake complete: "+string.Join(", ",meshes.Select(m=>m.triangles.Length/3+" triangles")));
  } finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 static T Save<T>(T obj,string name) where T:UnityEngine.Object {string path=Root+name;var previous=AssetDatabase.LoadAssetAtPath<T>(path);if(previous){if(obj!=previous){EditorUtility.CopySerialized(obj,previous);UnityEngine.Object.DestroyImmediate(obj);}EditorUtility.SetDirty(previous);return previous;}AssetDatabase.CreateAsset(obj,path);return obj;}
 }
}
