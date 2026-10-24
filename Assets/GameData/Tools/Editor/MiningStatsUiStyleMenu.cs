#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.EditorTools
{
    // Explicit scene authoring: never runs on import or in Play Mode.
    public sealed class MiningStatsUiStyleMenu : EditorWindow
    {
        private MiningPlayerStatsPanel target;
        private TMP_FontAsset font;
        private Vector2 panelSize = new(790, 738);
        private Vector2 buttonSize = new(110, 90);
        private static readonly Color Ink = Hex("352923"), Cream = Hex("FFF5DC"), Gold = Hex("FFCE57"), Teal = Hex("19B5A5");
        private const string Art = "Assets/Art/UI/Stats/";
        [MenuItem("Tools/Mining Simulator/UI/Style Stats Panel")]
        private static void ShowWindow() => GetWindow<MiningStatsUiStyleMenu>("Stats UI");
        private void OnGUI()
        {
            target = (MiningPlayerStatsPanel)EditorGUILayout.ObjectField("Stats panel component", target, typeof(MiningPlayerStatsPanel), true);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("Font (optional)", font, typeof(TMP_FontAsset), false);
            panelSize = EditorGUILayout.Vector2Field("Panel size", panelSize);
            buttonSize = EditorGUILayout.Vector2Field("Stats button size", buttonSize);
            EditorGUILayout.HelpBox("Applies to the selected component only. Keeps the player, coordinator and open button click bindings. Layout appears in Scene and supports Undo. Existing layout is deactivated, not deleted. Reapplying replaces only StatsStyledContent.", MessageType.Info);
            using (new EditorGUI.DisabledScope(target == null || EditorApplication.isPlaying))
                if (GUILayout.Button("Apply Stats Design")) Apply();
        }
        private void Apply()
        {
            var so = new SerializedObject(target);
            var panel = so.FindProperty("panel").objectReferenceValue as RectTransform;
            var open = so.FindProperty("openButton").objectReferenceValue as Button;
            var oldBody = so.FindProperty("body").objectReferenceValue as TMP_Text;
            if (panel == null || open == null || oldBody == null || Sprite("Rounded") == null)
            {
                Debug.LogError("Assign panel, openButton and body on the Stats component and import Assets/Art/UI/Stats first.", target);
                return;
            }
            if (!target.gameObject.scene.IsValid() || EditorUtility.IsPersistent(target))
            {
                Debug.LogError("Select a scene component or open its prefab in Prefab Mode first.", target);
                return;
            }
            var chosenFont = font != null ? font : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/UI/Fonts/Fredoka-SemiBold SDF.asset");
            if (chosenFont == null) chosenFont = oldBody.font;
            if (chosenFont == null) { Debug.LogError("Choose a TMP font asset first.", target); return; }
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Style Stats UI");
            var old = panel.Find("StatsStyledContent");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            foreach (Transform child in panel)
            {
                Undo.RecordObject(child.gameObject, "Hide previous stats layout");
                child.gameObject.SetActive(false);
            }
            Undo.RecordObject(panel, "Stats panel size");
            panel.sizeDelta = new Vector2(Mathf.Max(700, panelSize.x), Mathf.Max(650, panelSize.y));
            var background = panel.GetComponent<Image>();
            if (background != null) { Undo.RecordObject(background, "Stats background"); background.sprite = Sprite("Rounded"); background.type = Image.Type.Sliced; background.color = Cream; }
            var root = Rect(panel, "StatsStyledContent"); Stretch(root); var rootImage=Decorate(root, Cream, true); rootImage.raycastTarget=true; Raised(rootImage);
            var header = Rect(root, "Header"); Place(header, new(0, 0), new(panel.sizeDelta.x, 90), new(.5f,1), new(.5f,1)); Decorate(header, Gold);
            Icon(header, "stats", new(-panel.sizeDelta.x / 2 + 46,0), new(38,38), Ink);
            var title = Label(header, "Title", "PLAYER STATS", new(0,0), new(panel.sizeDelta.x-170,66), 36, Ink, chosenFont);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            var closeRect = Rect(header, "Close"); Place(closeRect,new(panel.sizeDelta.x/2-52,0),new(48,48));
            var closeImage = Decorate(closeRect, Hex("EF6862"),true); closeImage.raycastTarget=true; Raised(closeImage); var close = Undo.AddComponent<Button>(closeRect.gameObject); close.targetGraphic=closeImage; Undo.AddComponent<SmoothButtonPunch>(closeRect.gameObject);
            Label(closeRect,"CloseSymbol","×",Vector2.zero,new(44,44),32,Color.white,chosenFont);
            var progress = Rect(root, "LevelAndExperience"); Place(progress,new(0,-116),new(panel.sizeDelta.x-56,102),new(.5f,1),new(.5f,1));Decorate(progress,Color.white,true);
            var badge = Rect(progress,"LevelBadge");Place(badge,new(-progress.sizeDelta.x/2+64,0),new(92,68));Decorate(badge,Gold,true);
            var level = Label(badge,"Level","Lv. 1",Vector2.zero,new(86,62),26,Ink,chosenFont);
            float xpWidth = progress.sizeDelta.x-164;
            var experience = Label(progress,"Experience","0 / 100 XP",new(64,20),new(xpWidth,28),18,Ink,chosenFont);
            experience.alignment=TextAlignmentOptions.MidlineRight;
            var track = Rect(progress,"XPTrack");Place(track,new(64,-15),new(xpWidth,22));Decorate(track,Hex("D9E9DE"),true);
            var fill = Rect(track,"Fill");Stretch(fill);var fillImage=Decorate(fill,Teal,true);
            var slider=Undo.AddComponent<Slider>(track.gameObject);slider.fillRect=fill;slider.targetGraphic=fillImage;slider.interactable=false;slider.transition=Selectable.Transition.None;slider.minValue=0;slider.maxValue=1;slider.SetValueWithoutNotify(0);
            var rows=so.FindProperty("statRows");rows.arraySize=10;
            string[] keys={"DAMAGE","ATTACK_SPEED","MOVEMENT_SPEED","RANGE","STAMINA","HEALTH","ARMOR","MAGIC_RESISTANCE","REGEN_SPEED","REGEN_AMOUNT"};
            string[] labels={"Damage","Attack speed","Movement speed","Attack range","Stamina","Health","Armor","Magic resistance","Regeneration speed","Health regenerated"};
            string[] icons={"sword","bolt","boot","range","bolt","heart","shield","magic","regen","regen"};
            Color[] colors={Hex("B57635"),Hex("B57635"),Teal,Teal,Teal,Hex("EF6862"),Hex("65B5F6"),Hex("AD8DEB"),Teal,Teal};
            float width=(panel.sizeDelta.x-74)/2;
            var offense=Label(root,"OffenseHeading","OFFENSE & MOVEMENT",new(-(width/2+9),-238),new(width,30),16,Hex("806958"),chosenFont);
            offense.rectTransform.anchorMin=offense.rectTransform.anchorMax=new Vector2(.5f,1);
            offense.alignment=TextAlignmentOptions.MidlineLeft;
            var survival=Label(root,"SurvivalHeading","SURVIVAL",new(width/2+9,-238),new(width,30),16,Hex("806958"),chosenFont);
            survival.rectTransform.anchorMin=survival.rectTransform.anchorMax=new Vector2(.5f,1);
            survival.alignment=TextAlignmentOptions.MidlineLeft;
            var footer=Label(root,"Footer","CURRENT STATS • INCLUDING EQUIPMENT BONUSES",new(0,28),new(panel.sizeDelta.x-56,32),15,Hex("806958"),chosenFont);
            footer.rectTransform.anchorMin=footer.rectTransform.anchorMax=new Vector2(.5f,0);
            so.FindProperty("offenseHeading").objectReferenceValue=offense;so.FindProperty("survivalHeading").objectReferenceValue=survival;so.FindProperty("statsFooter").objectReferenceValue=footer;
            for(int i=0;i<10;i++)
            {
                int col=i/5, index=i%5;
                var r=Rect(root,"Stat_"+keys[i]);Place(r,new((col==0?-1:1)*(width/2+9),-264-index*70),new(width,60),new(.5f,1),new(.5f,1));Decorate(r,Color.white,true);
                Icon(r,icons[i],new(-width/2+26,0),new(24,24),Hex("B57635"));
                var label=Label(r,"Label",labels[i],new(-width*.125f,0),new(width*.43f,54),19,Ink,chosenFont);label.alignment=TextAlignmentOptions.MidlineLeft;
                var value=Label(r,"Value","—",new(width*.29f,0),new(width*.37f,54),23,colors[i],chosenFont);value.alignment=TextAlignmentOptions.MidlineRight;
                var entry=rows.GetArrayElementAtIndex(i);entry.FindPropertyRelative("key").stringValue=keys[i];entry.FindPropertyRelative("label").objectReferenceValue=label;entry.FindPropertyRelative("value").objectReferenceValue=value;
            }
            // Use existing localization keys; the presenter updates every dynamic label.
            so.FindProperty("title").objectReferenceValue=title;so.FindProperty("closeButton").objectReferenceValue=close;
            so.FindProperty("closeLabel").objectReferenceValue=null;so.FindProperty("levelLabel").objectReferenceValue=level;
            so.FindProperty("experienceLabel").objectReferenceValue=experience;so.FindProperty("experienceBar").objectReferenceValue=slider;
            var buttonRoot=open.transform as RectTransform;
            var previousButtonContent=buttonRoot.Find("StatsStyledButton");if(previousButtonContent!=null)Undo.DestroyObjectImmediate(previousButtonContent.gameObject);
            foreach(Transform child in buttonRoot){Undo.RecordObject(child.gameObject,"Hide old stats button visual");child.gameObject.SetActive(false);}
            Undo.RecordObject(buttonRoot,"Stats button size");buttonRoot.sizeDelta=new(Mathf.Max(72,buttonSize.x),Mathf.Max(70,buttonSize.y));
            var image=open.targetGraphic as Image;if(image==null) image=open.GetComponent<Image>();
            if(image!=null){Undo.RecordObject(image,"Stats button visual");image.sprite=Sprite("Rounded");image.type=Image.Type.Sliced;image.color=Color.white;Raised(image);}
            Undo.RecordObject(open,"Stats button colors");open.transition=Selectable.Transition.ColorTint;
            var buttonColors=open.colors;buttonColors.normalColor=Gold;buttonColors.highlightedColor=Hex("FFE592");buttonColors.selectedColor=Hex("FFE592");buttonColors.pressedColor=Hex("E5A832");buttonColors.fadeDuration=.1f;open.colors=buttonColors;
            var visuals=Rect(buttonRoot,"StatsStyledButton");Stretch(visuals);
            Icon(visuals,"stats",new(0,14),new(32,32),Ink);
            var openLabel=Label(visuals,"Label","STATS",new(0,-23),new(buttonRoot.sizeDelta.x-8,30),21,Ink,chosenFont);
            so.FindProperty("openLabel").objectReferenceValue=openLabel;so.ApplyModifiedProperties();
            Undo.RecordObject(panel.gameObject,"Preview stats in Scene");panel.gameObject.SetActive(true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
            Undo.CollapseUndoOperations(group);Selection.activeGameObject=panel.gameObject;
        }
        private static RectTransform Rect(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Stats UI element");go.transform.SetParent(parent,false);return (RectTransform)go.transform;
        }
        private static void Place(RectTransform r,Vector2 position,Vector2 size,Vector2? anchor=null,Vector2? pivot=null)
        { r.anchorMin=r.anchorMax=anchor??new Vector2(.5f,.5f);r.pivot=pivot??new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size; }
        private static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static Image Decorate(RectTransform r,Color color,bool rounded=false)
        {
            var image=Undo.AddComponent<Image>(r.gameObject);image.color=color;image.raycastTarget=false;
            if(rounded){image.sprite=Sprite("Rounded");image.type=Image.Type.Sliced;}
            return image;
        }
        private static TMP_Text Label(Transform parent,string name,string value,Vector2 position,Vector2 size,float fontSize,Color color,TMP_FontAsset font)
        {
            var rect=Rect(parent,name);Place(rect,position,size);var text=Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            var bodyFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/UI/Fonts/Nunito-SemiBold SDF.asset");
            text.font=(fontSize>=26 || name=="Label" && parent.name=="StatsStyledButton")?font:(bodyFont!=null?bodyFont:font);
            text.fontSharedMaterial=text.font.material;text.fontStyle=FontStyles.Normal;
            text.text=value;text.color=color;text.fontSize=fontSize;text.enableAutoSizing=true;text.fontSizeMin=14;text.fontSizeMax=fontSize;
            text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
        }
        private static void Icon(Transform parent,string name,Vector2 position,Vector2 size,Color color)
        { var r=Rect(parent,"Icon_"+name);Place(r,position,size);var image=Decorate(r,color);image.sprite=Sprite(name);image.preserveAspect=true; }
        private static void Raised(Image image)
        {
            var outline=image.GetComponent<Outline>();if(outline==null)outline=Undo.AddComponent<Outline>(image.gameObject);else Undo.RecordObject(outline,"Stats outline");
            outline.effectColor=Ink;outline.effectDistance=new Vector2(2,-2);
            var shadow=image.GetComponent<Shadow>();
            // Outline derives from Shadow; add an independent shadow only when needed.
            foreach(var candidate in image.GetComponents<Shadow>())if(!(candidate is Outline))shadow=candidate;
            if(shadow==null || shadow is Outline)shadow=Undo.AddComponent<Shadow>(image.gameObject);else Undo.RecordObject(shadow,"Stats shadow");
            shadow.effectColor=new Color(Ink.r,Ink.g,Ink.b,.5f);shadow.effectDistance=new Vector2(0,-6);
        }
        private static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
        private static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var result);return result;}
    }
}
#endif
