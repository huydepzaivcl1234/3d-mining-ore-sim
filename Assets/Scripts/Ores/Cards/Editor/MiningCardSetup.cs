using TMPro;
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    public static class MiningCardSetup
    {
        [MenuItem("Mining Simulator/Setup/Card Drops And Choices")]
        public static void Setup()
        {
            if (Application.isPlaying) { Debug.LogWarning("Stop Play before setting up cards."); return; }
            var inventory=Object.FindFirstObjectByType<MiningInventoryPanel>(FindObjectsInactive.Include);
            var player=Object.FindFirstObjectByType<MiningPlayerStats>(FindObjectsInactive.Include);
            var coordinator=Object.FindFirstObjectByType<MiningUiPanelCoordinator>(FindObjectsInactive.Include);
            if(inventory==null || player==null || coordinator==null) { Debug.LogError("Cards require the existing inventory, player and panel coordinator."); return; }
            var serialized=new SerializedObject(inventory);
            var inventoryPanel=serialized.FindProperty("inventoryPanel").objectReferenceValue as GameObject;
            if(inventoryPanel==null) { Debug.LogError("Inventory panel reference is missing."); return; }
            const string folder="Assets/GameData/Cards";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/GameData","Cards");
            const string path=folder+"/CardDrops.asset";
            var data=AssetDatabase.LoadAssetAtPath<MiningCardData>(path);
            if(data==null)
            {
                data=ScriptableObject.CreateInstance<MiningCardData>();
                string[] names={"RedCard","GreenCard","GoldCard"};
                for(int i=0;i<3;i++) data.tiers[i].prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/"+names[i]+".prefab");
                var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if(shader==null) { Debug.LogError("Card pillar shader not found."); return; }
                var material=new Material(shader);
                material.SetFloat("_Surface",1); material.SetFloat("_Blend",2);
                material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_ZWrite",0); material.renderQueue=3000;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                AssetDatabase.CreateAsset(material,folder+"/CardPillar.mat");
                data.beamMaterial=material;
                AssetDatabase.CreateAsset(data,path);
            }
            var parent=inventoryPanel.transform.parent;
            var existing=parent.Find("Card Choice Panel");
            RectTransform panel;
            if(existing!=null) panel=(RectTransform)existing;
            else
            {
                panel=Rect("Card Choice Panel",parent);
                panel.sizeDelta=data.panelSize;
                panel.gameObject.AddComponent<UnityEngine.UI.Image>().color=data.panelColor;
                panel.gameObject.AddComponent<CanvasGroup>();
            }
            var title=Label("Title",panel,data.fontSize+6f);
            title.rectTransform.anchorMin=new Vector2(.05f,.77f);title.rectTransform.anchorMax=new Vector2(.95f,.98f);
            title.rectTransform.offsetMin=title.rectTransform.offsetMax=Vector2.zero;
            title.text=MiningLocalization.Text("CARD_CHOOSE","Chọn một chỉ số");
            var buttons=new UnityEngine.UI.Button[3];
            for(int i=0;i<3;i++)
            {
                string name=((MiningCardChoice)i).ToString();
                var child=panel.Find(name);
                var rect=child!=null?(RectTransform)child:Rect(name,panel);
                if(child==null)
                {
                    rect.anchorMin=new Vector2(.04f+i*.32f,.09f);rect.anchorMax=new Vector2(.31f+i*.32f,.70f);
                    rect.offsetMin=rect.offsetMax=Vector2.zero;
                    rect.gameObject.AddComponent<UnityEngine.UI.Image>().color=Color.white;
                    var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();
                    var colors=button.colors;colors.normalColor=data.buttonColor;colors.highlightedColor=data.hoverColor;
                    colors.selectedColor=data.hoverColor;colors.pressedColor=data.buttonColor;button.colors=colors;
                    rect.gameObject.AddComponent<SmoothButtonPunch>();
                }
                buttons[i]=rect.GetComponent<UnityEngine.UI.Button>();
                var text=Label("Label",rect,data.fontSize);
                text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
                text.rectTransform.offsetMin=new Vector2(8,8);text.rectTransform.offsetMax=new Vector2(-8,-8);
                text.text=name;
            }
            var system=inventory.GetComponent<MiningCardSystem>();
            if(system==null) system=Undo.AddComponent<MiningCardSystem>(inventory.gameObject);
            Undo.RecordObject(system,"Configure cards");
            system.Configure(data,player,coordinator,panel,title,buttons);
            EditorUtility.SetDirty(system);
            panel.gameObject.SetActive(false);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
        private static RectTransform Rect(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Card UI");
            go.transform.SetParent(parent,false); return (RectTransform)go.transform;
        }
        private static TMP_Text Label(string name,Transform parent,float size)
        {
            var existing=parent.Find(name);
            if(existing!=null) return existing.GetComponent<TMP_Text>();
            var rect=Rect(name,parent);
            var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.fontSize=size;label.color=Color.white;
            label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
        }
    }
}
