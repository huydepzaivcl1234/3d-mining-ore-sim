using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Opt-in terrain authoring. Never mutates the original TerrainData.</summary>
    public static class MosswoodTerrainAuthoring
    {
        const string Output = ForestDefenseMapBuilder.Output;
        const string Vendor = "Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/Terrain/";

        static TerrainLayer Layer(string name, string source, Color tint)
        {
            string path = Output + "/" + name + ".terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                var original = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Vendor + "Layers/" + source + ".terrainlayer");
                if (original == null) throw new FileNotFoundException(source);
                layer = UnityEngine.Object.Instantiate(original);
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.tileSize = new Vector2(5, 5);
            layer.diffuseRemapMin = Vector4.zero;
            layer.diffuseRemapMax = new Vector4(tint.r, tint.g, tint.b, 1);
            layer.smoothness = .06f;
            EditorUtility.SetDirty(layer);
            AssetDatabase.SaveAssetIfDirty(layer);
            return layer;
        }

        public static void Paint(TerrainData data, Vector3 terrainPosition, Vector3 centre, bool standalone)
        {
            var layers = data.terrainLayers.ToList();
            var moss = Layer("Moss Grass", "UNS_Grass", new Color(.56f, .66f, .48f));
            var shade = Layer("Shade Grass", "UNS_Grass", new Color(.40f, .51f, .38f));
            var earth = Layer("Earth Path", "UNS_Dirt", new Color(.66f, .57f, .45f));
            float[,,] old = layers.Count == 0 ? null : data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            foreach (var layer in new[] { moss, shade, earth }) if (!layers.Contains(layer)) layers.Add(layer);
            data.terrainLayers = layers.ToArray();
            int mi = layers.IndexOf(moss), si = layers.IndexOf(shade), ei = layers.IndexOf(earth);
            int width = data.alphamapWidth, height = data.alphamapHeight;
            var map = new float[height, width, layers.Count];
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                float px = terrainPosition.x + x / (float)(width - 1) * data.size.x - centre.x;
                float pz = terrainPosition.z + z / (float)(height - 1) * data.size.z - centre.z;
                float blend = standalone ? 1 : 1 - Fade(40, 47, Mathf.Max(Mathf.Abs(px), Mathf.Abs(pz)));
                if (old != null) for (int i = 0; i < old.GetLength(2); i++) map[z,x,i] = old[z,x,i] * (1 - blend);
                float dirt = PathWeight(px, pz);
                float shadeWeight = Mathf.SmoothStep(.25f, .75f, Mathf.PerlinNoise(px * .085f + 13, pz * .085f + 29));
                map[z,x,mi] += blend * (1 - dirt) * (1 - shadeWeight);
                map[z,x,si] += blend * (1 - dirt) * shadeWeight;
                map[z,x,ei] += blend * dirt;
            }
            data.SetAlphamaps(0, 0, map);
        }

        static float PathWeight(float x, float z)
        {
            float noise = (Mathf.PerlinNoise(x * .3f + 7, z * .3f + 17) - .5f) * .7f;
            float lane = Mathf.Min(Mathf.Abs(x - Mathf.Sin(z * .10f) * 1.2f), Mathf.Abs(z - Mathf.Sin(x * .09f) * 1.2f));
            float path = 1 - Fade(2.2f + noise, 4.3f + noise, lane);
            float clearing = 1 - Fade(4.8f, 8.5f, new Vector2(x, z * 1.1f).magnitude + noise);
            return Mathf.Max(path, clearing) * .9f;
        }

        static float Fade(float low, float high, float value) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(low, high, value));

        public static void CreatePreviewGround(GameObject parent)
        {
            string path = Output + "/MosswoodPreviewTerrain.asset";
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data == null) { data = new TerrainData(); AssetDatabase.CreateAsset(data, path); }
            data.heightmapResolution = 129; data.alphamapResolution = 256; data.size = new Vector3(90, 6, 90);
            var heights = new float[129,129];
            for (int z=0; z<129; z++) for(int x=0;x<129;x++)
            {
                float px=x/128f*90-45, pz=z/128f*90-45;
                float edge=Mathf.Clamp01((Mathf.Max(Mathf.Abs(px),Mathf.Abs(pz))-31)/14);
                heights[z,x]=edge*edge*(1.5f+Mathf.PerlinNoise(px*.09f+10,pz*.09f+10)*2)/6;
            }
            data.SetHeights(0,0,heights);
            Paint(data,new Vector3(-45,0,-45),Vector3.zero,true);
            AssetDatabase.SaveAssetIfDirty(data);
            var ground=Terrain.CreateTerrainGameObject(data);
            ground.name="Soft Moss and Earth Terrain";
            SceneManager.MoveGameObjectToScene(ground,parent.scene);
            ground.transform.SetParent(parent.transform,false);
            ground.transform.localPosition=new Vector3(-45,0,-45);
            int layer=LayerMask.NameToLayer("Ground"); if(layer>=0)ground.layer=layer;
            ground.GetComponent<Terrain>().drawInstanced=true;
        }

        [MenuItem("Chest Defense/Map/Apply Mosswood to Current Scene (backup + Undo)")]
        public static void ApplyFromMenu() => Debug.Log(Apply());

        public static string Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            var scene=SceneManager.GetActiveScene();
            var roots=scene.GetRootGameObjects();
            if(roots.Any(r=>r.GetComponent<ForestMapLighting>()!=null)) throw new InvalidOperationException("Mosswood is already installed. Undo before applying again.");
            var terrain=roots.SelectMany(r=>r.GetComponentsInChildren<Terrain>()).FirstOrDefault();
            var chest=roots.SelectMany(r=>r.GetComponentsInChildren<TreasureChest>()).FirstOrDefault();
            if(terrain==null||chest==null) throw new InvalidOperationException("Current scene must contain Terrain and the gameplay chest.");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/MosswoodDefenseMap.prefab");
            if(prefab==null)throw new FileNotFoundException("Build Mosswood first.");
            string stamp=DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backup="Temp/MosswoodBackups/"+scene.name+"_"+stamp+".unity";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if(!EditorSceneManager.SaveScene(scene,backup,true))throw new IOException("Scene backup failed; no changes applied.");
            var copy=UnityEngine.Object.Instantiate(terrain.terrainData);
            string dataPath=AssetDatabase.GenerateUniqueAssetPath(Output+"/AppliedTerrain.asset");
            AssetDatabase.CreateAsset(copy,dataPath);
            Vector3 centre=chest.transform.position; centre.y=terrain.SampleHeight(centre)+terrain.transform.position.y;
            int undoGroup=-1;
            try
            {
                Paint(copy,terrain.transform.position,centre,false);
                int before=copy.treeInstanceCount;
                copy.treeInstances=copy.treeInstances.Where(tree=>
                {
                    Vector3 world=Vector3.Scale(tree.position,copy.size)+terrain.transform.position;
                    return Mathf.Max(Mathf.Abs(world.x-centre.x),Mathf.Abs(world.z-centre.z))>44;
                }).ToArray();
                AssetDatabase.SaveAssetIfDirty(copy);
                Undo.IncrementCurrentGroup(); undoGroup=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Apply Mosswood environment");
                Undo.RecordObject(terrain,"Replace environment terrain data"); terrain.terrainData=copy;
                terrain.treeDistance=130;terrain.treeBillboardDistance=45;terrain.treeCrossFadeLength=10;
                terrain.detailObjectDistance=40;terrain.drawInstanced=true;
                var collider=terrain.GetComponent<TerrainCollider>();
                if(collider!=null){Undo.RecordObject(collider,"Keep terrain support identical");collider.terrainData=copy;}
                var map=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                Undo.RegisterCreatedObjectUndo(map,"Install Mosswood environment");map.transform.position=centre;
                map.transform.Find("Optional Ground - disable over existing Terrain").gameObject.SetActive(false);
                foreach(Transform prop in map.transform.Find("Forest - LOD and distance culling"))
                {
                    Vector3 position=prop.position;position.y=terrain.SampleHeight(position)+terrain.transform.position.y;prop.position=position;
                }
                var clock=roots.SelectMany(r=>r.GetComponentsInChildren<DayNightSystem>()).FirstOrDefault();
                if(clock!=null)
                {
                    var serialized=new SerializedObject(clock);var field=serialized.FindProperty("data");
                    var original=field.objectReferenceValue as DayNightData;
                    if(original!=null)
                    {
                        var lighting=UnityEngine.Object.Instantiate(original);
                        AssetDatabase.CreateAsset(lighting,AssetDatabase.GenerateUniqueAssetPath(Output+"/AppliedLighting.asset"));
                        var palette=AssetDatabase.LoadAssetAtPath<DayNightData>(Output+"/MosswoodLighting.asset");
                        var src=new SerializedObject(palette);var dst=new SerializedObject(lighting);
                        foreach(string name in new[]{"daySunColor","nightSunColor","daySunIntensity","nightSunIntensity","nightSunRotation","nightSunArcDegrees","nightAmbientColor","nightFogDensity","dayBloomIntensity","nightBloomIntensity","bloomThreshold","goldenHourBloomBoost","nightPostExposure"})
                            dst.CopyFromSerializedProperty(src.FindProperty(name));
                        dst.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(lighting);
                        Undo.RecordObject(clock,"Use Mosswood palette without changing gameplay timing");field.objectReferenceValue=lighting;serialized.ApplyModifiedProperties();
                    }
                }
                Undo.CollapseUndoOperations(undoGroup);EditorSceneManager.MarkSceneDirty(scene);Physics.SyncTransforms();
                return "Applied to "+scene.name+" (left unsaved). Backup: "+backup+". Original terrain preserved; replaced "+(before-copy.treeInstanceCount)+" local trees; retained "+copy.treeInstanceCount+" outside. Terrain heights, gameplay objects and saves unchanged.";
            }
            catch
            {
                if(undoGroup>=0)Undo.RevertAllDownToGroup(undoGroup);
                throw; // Keep the safety copy and authored assets for diagnosis.
            }
        }
    }
}
