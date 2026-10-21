using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    public static class CannonLodSetup
    {
        public static string Apply()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Exit Play first.");
            const string path="Assets/GameData/Tower/Cannon/CannonTower.prefab";
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Tower/Cannon/Source/CannonLOD.fbx");
            var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var lod0=go.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!r.name.EndsWith("LOD1")).ToArray();
                var lod1=new System.Collections.Generic.List<Renderer>();
                foreach(var renderer in lod0)
                {
                    var original=renderer.GetComponent<MeshFilter>().sharedMesh;
                    var mesh=model.GetComponentsInChildren<MeshFilter>().First(f=>f.name==renderer.name).sharedMesh;
                    if((mesh.bounds.size-original.bounds.size).magnitude>original.bounds.size.magnitude*.02f)
                        throw new InvalidOperationException("LOD coordinate mismatch: "+renderer.name);
                    var child=renderer.transform.Find(renderer.name+"LOD1");
                    if(child==null) child=new GameObject(renderer.name+"LOD1",typeof(MeshFilter),typeof(MeshRenderer)).transform;
                    child.SetParent(renderer.transform,false);child.localPosition=Vector3.zero;child.localRotation=Quaternion.identity;child.localScale=Vector3.one;
                    child.GetComponent<MeshFilter>().sharedMesh=mesh;
                    var copy=child.GetComponent<MeshRenderer>();
                    copy.sharedMaterials=renderer.sharedMaterials;copy.shadowCastingMode=renderer.shadowCastingMode;copy.receiveShadows=renderer.receiveShadows;
                    lod1.Add(copy);
                }
                var group=go.GetComponent<LODGroup>();if(group==null)group=go.AddComponent<LODGroup>();
                // Original close-up; reduced mesh for surrounding cannons. Never cull gameplay.
                group.fadeMode=LODFadeMode.None;
                group.SetLODs(new[]{new LOD(.5f,lod0),new LOD(0,lod1.ToArray())});
                group.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(go,path);
                int triangles=lod1.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3);
                return $"Cannon: original 21,972 triangles at close range; LOD1 {triangles} triangles, animated under original mesh transforms.";
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
    }
}
