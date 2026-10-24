#if UNITY_EDITOR
using System.Linq;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class TowerAssetSetup
{
    private const string Folder="Assets/GameData/Tower/Cannon/";
    [MenuItem("Chest Defense/Towers/Setup Cannon And Consumables")]
    public static void Setup()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play first.");
        ConfigureItems();
        foreach(string file in new[]{"ReferenceCannon.fbx","CannonBall.fbx"})
        {
            var imp=(ModelImporter)AssetImporter.GetAtPath(Folder+"Source/"+file);
            imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.importCameras=false;imp.importLights=false;
            imp.animationType=ModelImporterAnimationType.Generic;imp.importAnimation=file=="ReferenceCannon.fbx";
            imp.animationCompression=ModelImporterAnimationCompression.Off;
            if(imp.importAnimation)
            {
                var clips=imp.defaultClipAnimations;
                foreach(var clip in clips){clip.name="Cannon_Fire";clip.loopTime=false;clip.events=System.Array.Empty<AnimationEvent>();}
                imp.clipAnimations=clips;
            }
            imp.SaveAndReimport();
        }
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Folder+"Source/Textures"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var imp=(TextureImporter)AssetImporter.GetAtPath(path);
            imp.sRGBTexture=path.EndsWith("BaseColor.png");imp.textureType=path.EndsWith("Normal.png")?TextureImporterType.NormalMap:TextureImporterType.Default;
            imp.SaveAndReimport();
        }
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Cannon.mat");
        if(material==null)
        {
            material=new Material(shader);material.SetTexture("_BaseMap",Tex("BaseColor"));material.SetTexture("_BumpMap",Tex("Normal"));
            material.EnableKeyword("_NORMALMAP");material.SetTexture("_MetallicGlossMap",Tex("MetallicSmoothness"));
            material.EnableKeyword("_METALLICSPECGLOSSMAP");AssetDatabase.CreateAsset(material,Folder+"Cannon.mat");
        }
        var cannon=AssetDatabase.LoadAssetAtPath<CannonTowerData>(Folder+"CannonData.asset");
        if(cannon==null){cannon=ScriptableObject.CreateInstance<CannonTowerData>();AssetDatabase.CreateAsset(cannon,Folder+"CannonData.asset");}
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"Cannon.controller");
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"Cannon.controller");controller.AddParameter("Fire",AnimatorControllerParameterType.Trigger);
            var sm=controller.layers[0].stateMachine;var idle=sm.AddState("Idle");sm.defaultState=idle;
            var shoot=sm.AddState("Cannon_Fire");shoot.motion=AssetDatabase.LoadAllAssetsAtPath(Folder+"Source/ReferenceCannon.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
            var enter=sm.AddAnyStateTransition(shoot);enter.hasExitTime=false;enter.duration=0;enter.canTransitionToSelf=false;enter.AddCondition(AnimatorConditionMode.If,0,"Fire");
            var exit=shoot.AddTransition(idle);exit.hasExitTime=true;exit.exitTime=1;exit.duration=.04f;
        }
        if(cannon.projectile==null)
        {
            var ball=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Source/CannonBall.fbx"));
            try{Assign(ball,material);cannon.projectile=PrefabUtility.SaveAsPrefabAsset(ball,Folder+"CannonBall.prefab");}finally{Object.DestroyImmediate(ball);}
        }
        if(cannon.prefab==null)
        {
            var root=new GameObject("Cannon Tower");var visual=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Source/ReferenceCannon.fbx"),root.transform);
            try
            {
                Assign(visual,material);var bounds=BoundsOf(visual);visual.transform.localPosition-=Vector3.up*bounds.min.y;
                bounds=BoundsOf(visual);
                var box=root.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;
                var hp=root.AddComponent<MiningCharacterHealth>();var runtime=root.AddComponent<TowerRuntime>();
                var anim=visual.GetComponent<Animator>();if(anim==null)anim=visual.AddComponent<Animator>();anim.runtimeAnimatorController=controller;anim.applyRootMotion=false;
                var ser=new SerializedObject(runtime);ser.FindProperty("data").objectReferenceValue=cannon;ser.FindProperty("animator").objectReferenceValue=anim;
                ser.FindProperty("muzzle").objectReferenceValue=visual.GetComponentsInChildren<Transform>().First(t=>t.name=="Muzzle");ser.ApplyModifiedPropertiesWithoutUndo();
                cannon.prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"CannonTower.prefab");
            }
            finally{Object.DestroyImmediate(root);}
        }
        // FBX keys its own root position. Keep ground-height compensation outside that Animator.
        var editable=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(cannon.prefab));
        try
        {
            var animated=editable.GetComponentInChildren<Animator>().transform;
            if(animated.parent==editable.transform)
            {
                var mount=new GameObject("VisualMount").transform;mount.SetParent(editable.transform,false);
                mount.localPosition=animated.localPosition;
                animated.SetParent(mount,false);animated.localPosition=Vector3.zero;
                PrefabUtility.SaveAsPrefabAsset(editable,AssetDatabase.GetAssetPath(cannon.prefab));
            }
        }
        finally{PrefabUtility.UnloadPrefabContents(editable);}
        if(cannon.icon==null)cannon.icon=RenderIcon(cannon.prefab);
        var item=AssetDatabase.LoadAssetAtPath<MiningItemData>(Folder+"CannonInventory.asset");
        if(item==null){item=ScriptableObject.CreateInstance<MiningItemData>();AssetDatabase.CreateAsset(item,Folder+"CannonInventory.asset");}
        var iser=new SerializedObject(item);iser.FindProperty("itemId").stringValue="tower_cannon";iser.FindProperty("displayName").stringValue="Cannon";
        iser.FindProperty("useType").intValue=(int)MiningItemUseType.Tower;iser.FindProperty("tower").objectReferenceValue=cannon;
        iser.FindProperty("inventoryIcon").objectReferenceValue=cannon.icon;iser.FindProperty("selectionChancePercent").floatValue=0;
        iser.FindProperty("traderCanBuy").boolValue=false;iser.FindProperty("traderCanSell").boolValue=false;iser.ApplyModifiedPropertiesWithoutUndo();
        cannon.inventoryItem=item;EditorUtility.SetDirty(cannon);
        var db=AssetDatabase.LoadAssetAtPath<MiningItemDatabase>("Assets/GameData/Items/MiningItemDatabase.asset");var dbs=new SerializedObject(db);var list=dbs.FindProperty("items");
        if(!Enumerable.Range(0,list.arraySize).Any(i=>list.GetArrayElementAtIndex(i).objectReferenceValue==item))
        {list.InsertArrayElementAtIndex(list.arraySize);list.GetArrayElementAtIndex(list.arraySize-1).objectReferenceValue=item;dbs.ApplyModifiedPropertiesWithoutUndo();}
        var catalog=AssetDatabase.LoadAssetAtPath<TowerCatalog>("Assets/Resources/TowerCatalog.asset");
        if(catalog==null){catalog=ScriptableObject.CreateInstance<TowerCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/Resources/TowerCatalog.asset");}
        if(!catalog.towers.Contains(cannon)){catalog.towers=catalog.towers.Concat(new TowerData[]{cannon}).ToArray();EditorUtility.SetDirty(catalog);}
        AssetDatabase.SaveAssets();
    }
    private static Texture2D Tex(string suffix)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Source/Textures/Cannon_"+suffix+".png");
    private static void Assign(GameObject go,Material mat){foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();}
    private static Bounds BoundsOf(GameObject go){var renderers=go.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
    private static Sprite RenderIcon(GameObject prefab)
    {
        var root=Object.Instantiate(prefab);var cameraObject=new GameObject("Cannon icon camera");var lightObject=new GameObject("Cannon icon light");
        var rt=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(512,512,TextureFormat.RGBA32,false);
        var previous=RenderTexture.active;
        try
        {
            foreach(var script in root.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
            root.transform.position=new Vector3(10000,10000,10000);foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
            var b=BoundsOf(root);var camera=cameraObject.AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            camera.orthographic=true;camera.orthographicSize=b.size.magnitude*.53f;camera.transform.position=b.center+new Vector3(3,2,4);camera.transform.LookAt(b.center);camera.targetTexture=rt;
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(35,-30,0);
            camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,512,512),0,0);pixels.Apply();
            System.IO.File.WriteAllBytes(Folder+"CannonIcon.png",pixels.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;Object.DestroyImmediate(root);Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(lightObject);Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);}
        AssetDatabase.ImportAsset(Folder+"CannonIcon.png");var imp=(TextureImporter)AssetImporter.GetAtPath(Folder+"CannonIcon.png");imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.alphaIsTransparency=true;imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"CannonIcon.png");
    }
    public static void ConfigureItems()
    {
        Set("Apple",MiningItemEffectType.PlayerHealing,10,5,"Hồi tổng 10 HP trong 5 giây");
        Set("Banana",MiningItemEffectType.PlayerMoveSpeed,5,180,"Cộng 5 tốc chạy trong 3 phút");
        Set("Carrot",MiningItemEffectType.PlayerAttackSpeed,1,180,"Tăng 1% tốc đánh trong 3 phút");
        Set("Grape",MiningItemEffectType.PlayerMaxHealth,5,180,"Tăng 5% máu tối đa trong 3 phút");
        Set("Ice cream",MiningItemEffectType.PlayerDamageReduction,1,180,"Giảm 1% sát thương nhận trong 3 phút");
        Set("Pea",MiningItemEffectType.MonsterMaxHealthTrueDamage,.5f,150,"Mỗi đòn đánh thêm 0,5% HP tối đa của quái thành sát thương chuẩn trong 150 giây");
    }
    private static void Set(string name,MiningItemEffectType effect,float amount,float duration,string description)
    {
        var item=AssetDatabase.LoadAssetAtPath<MiningItemData>("Assets/GameData/Items/"+name+".asset");var s=new SerializedObject(item);
        s.FindProperty("effectType").intValue=(int)effect;s.FindProperty("effectPercent").floatValue=amount;s.FindProperty("effectDurationSeconds").floatValue=duration;
        s.FindProperty("description").stringValue=description;s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(item);
    }
}
#endif
