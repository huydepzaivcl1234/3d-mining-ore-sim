#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Explicit Scene authoring. Reuses serialized Shop views and all transaction owners.</summary>
    public sealed class MiningShopUiStyleMenu : EditorWindow
    {
        private MiningShopPanel shop;
        private TMP_FontAsset font;
        private Vector2 size = new(1320, 820);
        private float rowHeight = 132, rowGap = 14, wheelDiameter = 380;
        private static readonly Color Ink = Hex("352923"), Cream = Hex("FFF5DC"), Gold = Hex("FFCE57"), Teal = Hex("19B5A5"), Muted = Hex("806958");
        private const string RoundedPath = "Assets/Art/UI/Shop/Rounded.png";
        private const string GemPath = "Assets/Ores/Icons/Items/GemCurrencyIcon.png";

        [MenuItem("Tools/Mining Simulator/UI/Style Shop Panel")]
        private static void ShowWindow() => GetWindow<MiningShopUiStyleMenu>("Shop Design");
        private void OnGUI()
        {
            shop = (MiningShopPanel)EditorGUILayout.ObjectField("Shop component", shop, typeof(MiningShopPanel), true);
            font = (TMP_FontAsset)EditorGUILayout.ObjectField("Font (optional)", font, typeof(TMP_FontAsset), false);
            size = EditorGUILayout.Vector2Field("Shop size", size);
            rowHeight = EditorGUILayout.FloatField("Product height", rowHeight);
            rowGap = EditorGUILayout.FloatField("Product spacing", rowGap);
            wheelDiameter = EditorGUILayout.FloatField("Wheel diameter", wheelDiameter);
            EditorGUILayout.HelpBox("Styles the selected Scene Shop. Reuses product rows, sprites, buy buttons, wheel segments, pointer and callbacks. Shows the result in Scene; supports Undo. Save the Scene after reviewing.", MessageType.Info);
            using (new EditorGUI.DisabledScope(shop == null || EditorApplication.isPlaying))
                if (GUILayout.Button("Apply Shop Design")) Apply();
        }

        private void Apply()
        {
            var so = new SerializedObject(shop);
            var panel = Ref<RectTransform>(so, "panelRoot");
            var title = Ref<TextMeshProUGUI>(so, "titleLabel");
            var close = Ref<Button>(so, "closeButton");
            var wheel = Ref<RectTransform>(so, "wheelRoot");
            var data = Ref<MiningShopData>(so, "data");
            var views = so.FindProperty("productViews");
            var rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedPath);
            var card = panel != null ? panel.Find("Shop Card") as RectTransform : null;
            var scroll = card != null ? card.GetComponentInChildren<ScrollRect>(true) : null;
            var pointer = wheel != null && wheel.parent != null ? wheel.parent.Find("Wheel Pointer") as RectTransform : null;
            if (panel == null || card == null || title == null || close == null || wheel == null ||
                pointer == null || scroll == null || scroll.viewport == null || scroll.content == null ||
                rounded == null || views == null || views.arraySize == 0)
            {
                Debug.LogError("Import Shop art, then assign a Shop with its Shop Card, product ScrollRect, wheel and Wheel Pointer.", shop);
                return;
            }
            if (!shop.gameObject.scene.IsValid() || EditorUtility.IsPersistent(shop))
            {
                Debug.LogError("Select a Scene Shop or open its prefab in Prefab Mode first.", shop);
                return;
            }
            for (int i = 0; i < views.arraySize; i++)
            {
                var view = views.GetArrayElementAtIndex(i);
                if (View<GameObject>(view, "root") == null || View<Button>(view, "buyButton") == null)
                {
                    Debug.LogError("Assign root and buyButton for every authored product view before styling.", shop);
                    return;
                }
            }
            var chosenFont = font != null ? font : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/UI/Fonts/Fredoka-SemiBold SDF.asset");
            if(chosenFont==null)chosenFont=title.font;
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Style Shop UI");

            float width = Mathf.Max(1100, size.x), height = Mathf.Max(760, size.y);
            float leftWidth = width * .56515f, rightWidth = width - leftWidth - 90;
            float listHeight = height - 200, productHeight = Mathf.Max(116, rowHeight);
            float gap = Mathf.Max(0, rowGap), leftX = -width/2 + 30 + leftWidth/2;
            float rightX = width/2 - 30 - rightWidth/2;
            Place(card, panel, Vector2.zero, new(width,height));
            ImageStyle(card, Cream, rounded, true, true);
            var header = title.transform.parent as RectTransform;
            if (header == null || header == card) header = Decoration(card, "ShopStyledHeader");
            Place(header, card, Vector2.zero, new(width,90), new(.5f,1), new(.5f,1));
            ImageStyle(header, Gold, rounded, true, false);
            Place(title.rectTransform, header, new(-width*.20f,0), new(width*.53f,66));
            TextStyle(title,chosenFont,36,Ink,TextAlignmentOptions.MidlineLeft);
            Place((RectTransform)close.transform,header,new(width/2-48,0),new(48,48));
            ButtonStyle(close,Hex("EF6862"),rounded);
            foreach (var label in close.GetComponentsInChildren<TMP_Text>(true))
                TextStyle(label,chosenFont,28,Color.white,TextAlignmentOptions.Center);

            var gemBalance = Ref<TextMeshProUGUI>(so, "gemBalanceLabel");
            if (gemBalance != null)
            {
                var pill=Decoration(header,"ShopStyledGemBalance");
                Place(pill,header,new(width/2-250,0),new(320,54));ImageStyle(pill,Cream,rounded,false,false);
                GemIcon(pill,new(-130,0),new(30,30));
                Place(gemBalance.rectTransform,pill,new(16,0),new(268,46));
                TextStyle(gemBalance,chosenFont,24,Ink,TextAlignmentOptions.Center);
            }

            Place(scroll.transform as RectTransform,card,new(leftX,-14),new(leftWidth,listHeight));
            Undo.RecordObject(scroll,"Shop scrolling");
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            // Retain the existing smooth-scroll component and all scrollbar/listener references.
            Stretch(scroll.viewport,12);
            var scrollImage=scroll.GetComponent<Image>();
            if(scrollImage!=null){Undo.RecordObject(scrollImage,"Clear old Shop list background");scrollImage.color=new Color(1,1,1,0);scrollImage.sprite=null;scrollImage.raycastTarget=true;}
            var viewportImage=scroll.viewport.GetComponent<Image>();
            if(viewportImage!=null){Undo.RecordObject(viewportImage,"Shop viewport background");viewportImage.color=Cream;viewportImage.sprite=null;}
            var mask=scroll.viewport.GetComponent<Mask>();
            if(mask!=null){Undo.RecordObject(mask,"Shop clipping");mask.showMaskGraphic=false;}
            if(mask==null && scroll.viewport.GetComponent<RectMask2D>()==null)Undo.AddComponent<RectMask2D>(scroll.viewport.gameObject);
            var content=scroll.content;
            Undo.RecordObject(content,"Shop content layout");
            content.localScale=Vector3.one;
            var contentImage=content.GetComponent<Image>();
            if(contentImage!=null){Undo.RecordObject(contentImage,"Clear legacy content decoration");contentImage.color=Color.clear;contentImage.raycastTarget=false;}
            content.anchorMin=new(0,1);content.anchorMax=new(1,1);content.pivot=new(.5f,1);
            content.anchoredPosition=Vector2.zero;content.sizeDelta=new(0,views.arraySize*(productHeight+gap)-gap);
            var layout=content.GetComponent<VerticalLayoutGroup>();
            if(layout==null)layout=Undo.AddComponent<VerticalLayoutGroup>(content.gameObject);
            else Undo.RecordObject(layout,"Shop product layout");
            layout.padding=new RectOffset(0,0,0,0);layout.spacing=gap;layout.childAlignment=TextAnchor.UpperCenter;
            layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            var fitter=content.GetComponent<ContentSizeFitter>();
            if(fitter==null)fitter=Undo.AddComponent<ContentSizeFitter>(content.gameObject);
            else Undo.RecordObject(fitter,"Shop content height");
            fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            if(scroll.verticalScrollbar!=null)
            {
                var bar=scroll.verticalScrollbar.transform as RectTransform;
                Place(bar,scroll.transform,new(leftWidth/2-4,0),new(8,listHeight));
                var graphic=scroll.verticalScrollbar.targetGraphic as Image;
                if(graphic!=null)ImageStyle(graphic.rectTransform,Teal,rounded,true,false);
            }

            float productWidth=leftWidth-12;
            for(int i=0;i<views.arraySize;i++)
            {
                var v=views.GetArrayElementAtIndex(i);var row=View<GameObject>(v,"root").transform as RectTransform;
                Place(row,content,new(0,-i*(productHeight+gap)),new(productWidth,productHeight),new(.5f,1),new(.5f,1));
                ImageStyle(row,Color.white,rounded,false,false);
                var element=row.GetComponent<LayoutElement>();
                if(element==null)element=Undo.AddComponent<LayoutElement>(row.gameObject);else Undo.RecordObject(element,"Shop row sizing");
                element.minHeight=element.preferredHeight=productHeight;element.flexibleHeight=0;
                element.minWidth=0;element.preferredWidth=-1;element.flexibleWidth=1;
                var icon=View<Image>(v,"icon");var fallback=View<TextMeshProUGUI>(v,"iconFallback");
                var frame=row.Find("SVG Item Icon Frame") as RectTransform;
                if(frame==null)frame=Decoration(row,"ShopStyledIconFrame");
                Vector2 iconPosition=new(-productWidth/2+66,0);
                Place(frame,row,iconPosition,new(96,96));ImageStyle(frame,Cream,rounded,false,false);frame.SetAsFirstSibling();
                if(icon!=null)
                {
                    Place(icon.rectTransform,row,iconPosition,new(84,84));
                    Undo.RecordObject(icon,"Shop item art");icon.preserveAspect=true;icon.color=Color.white;icon.raycastTarget=false;
                    if(data!=null && i<data.Products.Count && data.Products[i]!=null)icon.sprite=data.Products[i].Icon;
                    icon.enabled=icon.sprite!=null;
                }
                if(fallback!=null)
                {
                    Place(fallback.rectTransform,row,iconPosition,new(80,80));TextStyle(fallback,chosenFont,32,Ink,TextAlignmentOptions.Center);
                    Undo.RecordObject(fallback.gameObject,"Shop icon fallback");fallback.gameObject.SetActive(icon==null || icon.sprite==null);
                }
                float detailWidth=productWidth-320, detailX=-productWidth/2+132+detailWidth/2;
                var name=View<TextMeshProUGUI>(v,"nameLabel");
                var description=View<TextMeshProUGUI>(v,"descriptionLabel");
                var amount=View<TextMeshProUGUI>(v,"amountLabel");
                var price=View<TextMeshProUGUI>(v,"priceLabel");
                LayoutText(name,row,new(detailX,productHeight/2-29),new(detailWidth,32),chosenFont,24,Ink,TextAlignmentOptions.MidlineLeft);
                LayoutText(description,row,new(detailX,0),new(detailWidth,44),chosenFont,18,Muted,TextAlignmentOptions.MidlineLeft);
                LayoutText(amount,row,new(detailX,-productHeight/2+24),new(detailWidth,26),chosenFont,16,Hex("AD8DEB"),TextAlignmentOptions.MidlineLeft);
                var buy=View<Button>(v,"buyButton");var buyLabel=View<TextMeshProUGUI>(v,"buyLabel");
                float actionX=productWidth/2-84;
                Place(buy.transform as RectTransform,row,new(actionX,-22),new(132,50));ButtonStyle(buy,Teal,rounded);
                if(buyLabel!=null){Place(buyLabel.rectTransform,buy.transform,Vector2.zero,new(124,44));TextStyle(buyLabel,chosenFont,24,Color.white,TextAlignmentOptions.Center);}
                LayoutText(price,row,new(actionX+10,34),new(128,30),chosenFont,24,Teal,TextAlignmentOptions.Center);
                GemIcon(row,new(actionX-60,34),new(20,20));
                if(data!=null && i<data.Products.Count && data.Products[i]!=null)
                {
                    var p=data.Products[i];
                    SetPreview(name,p.DisplayName);SetPreview(description,p.Description);
                    SetPreview(amount,"x"+p.ItemAmount);SetPreview(price,MiningMoneyFormatter.Format(p.GemCost)+" GEM");
                }
            }

            var wheelBackground=Decoration(card,"ShopStyledWheelBackground");
            Place(wheelBackground,card,new(rightX,-14),new(rightWidth,listHeight));
            ImageStyle(wheelBackground,Hex("D9E9DE"),rounded,false,false);wheelBackground.SetAsFirstSibling();
            Place(wheel,card,new(rightX,44),wheel.sizeDelta);
            Undo.RecordObject(wheel,"Wheel visual size");
            float diameter=Mathf.Max(260,Mathf.Min(wheelDiameter,rightWidth-40));
            wheel.localScale=Vector3.one*(diameter/Mathf.Max(1,wheel.sizeDelta.x));
            foreach(var label in wheel.GetComponentsInChildren<TMP_Text>(true))
                TextStyle(label,chosenFont,18,label.color,label.alignment);
            Place(pointer,card,new(rightX,44+diameter/2),new(40,36));pointer.SetAsLastSibling();
            var wheelTitle=Ref<TextMeshProUGUI>(so,"wheelTitleLabel");
            LayoutText(wheelTitle,card,new(rightX,height/2-140),new(rightWidth-30,44),chosenFont,28,Ink,TextAlignmentOptions.Center);
            var once=Ref<Button>(so,"spinOnceButton");var ten=Ref<Button>(so,"spinTenButton");
            float spinWidth=(rightWidth-54)/2;
            if(once!=null){Place(once.transform as RectTransform,card,new(rightX-spinWidth/2-9,-height/2+160),new(spinWidth,80));ButtonStyle(once,Gold,rounded);}
            if(ten!=null){Place(ten.transform as RectTransform,card,new(rightX+spinWidth/2+9,-height/2+160),new(spinWidth,80));ButtonStyle(ten,Gold,rounded);}
            SpinLabel(Ref<TextMeshProUGUI>(so,"spinOnceLabel"),once,spinWidth,chosenFont);
            SpinLabel(Ref<TextMeshProUGUI>(so,"spinTenLabel"),ten,spinWidth,chosenFont);
            var status=Ref<TextMeshProUGUI>(so,"statusLabel");
            LayoutText(status,card,new(0,-height/2+36),new(width-60,52),chosenFont,20,Muted,TextAlignmentOptions.Center);
            var result=Ref<RectTransform>(so,"wheelResultPanel");
            if(result!=null)
            {
                Place(result,card,Vector2.zero,result.sizeDelta);
                ImageStyle(result,Cream,rounded,true,true);
                TextStyle(Ref<TextMeshProUGUI>(so,"wheelResultsLabel"),chosenFont,20,Ink,TextAlignmentOptions.TopLeft);
                result.SetAsLastSibling();
                Undo.RecordObject(result.gameObject,"Hide inactive wheel results");result.gameObject.SetActive(false);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.StopMovement();scroll.verticalNormalizedPosition=1;
            Undo.RecordObject(panel.gameObject,"Preview Shop in Scene");panel.gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);Undo.CollapseUndoOperations(group);
            Selection.activeGameObject=card.gameObject;
        }

        private static T Ref<T>(SerializedObject so,string key) where T:Object=>so.FindProperty(key).objectReferenceValue as T;
        private static T View<T>(SerializedProperty view,string key) where T:Object=>view.FindPropertyRelative(key).objectReferenceValue as T;
        private static RectTransform Decoration(Transform parent,string name)
        {
            var existing=parent.Find(name) as RectTransform;if(existing!=null)return existing;
            var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Shop visual");
            go.transform.SetParent(parent,false);return (RectTransform)go.transform;
        }
        private static void Place(RectTransform rect,Transform parent,Vector2 position,Vector2 dimensions,Vector2? anchor=null,Vector2? pivot=null)
        {
            if(rect==null)return;
            if(rect.parent!=parent)Undo.SetTransformParent(rect,parent,"Shop visual parent");
            Undo.RecordObject(rect,"Shop visual layout");rect.anchorMin=rect.anchorMax=anchor??new Vector2(.5f,.5f);
            rect.pivot=pivot??new Vector2(.5f,.5f);rect.anchoredPosition3D=new(position.x,position.y,0);rect.sizeDelta=dimensions;rect.localScale=Vector3.one;
        }
        private static void Stretch(RectTransform rect,float rightInset)
        {Undo.RecordObject(rect,"Shop viewport");rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.zero;rect.offsetMax=new(-rightInset,0);}
        private static void ImageStyle(RectTransform rect,Color color,Sprite sprite,bool raycast,bool raised)
        {
            var image=rect.GetComponent<Image>();if(image==null)image=Undo.AddComponent<Image>(rect.gameObject);else Undo.RecordObject(image,"Shop graphic");
            image.sprite=sprite;image.overrideSprite=null;image.type=sprite!=null?Image.Type.Sliced:Image.Type.Simple;
            image.color=color;image.material=null;image.raycastTarget=raycast;
            if(!raised)foreach(var effect in rect.GetComponents<Shadow>()){Undo.RecordObject(effect,"Clear legacy graphic effects");effect.enabled=false;}
            if(raised)
            {
                var outline=rect.GetComponent<Outline>();if(outline==null)outline=Undo.AddComponent<Outline>(rect.gameObject);else Undo.RecordObject(outline,"Shop border");
                outline.enabled=true;outline.effectColor=Ink;outline.effectDistance=new(2,-2);
                Shadow shadow=null;foreach(var effect in rect.GetComponents<Shadow>())if(!(effect is Outline))shadow=effect;
                if(shadow==null)shadow=Undo.AddComponent<Shadow>(rect.gameObject);else Undo.RecordObject(shadow,"Shop depth");
                shadow.enabled=true;shadow.effectColor=new Color(Ink.r,Ink.g,Ink.b,.45f);shadow.effectDistance=new(0,-6);
            }
        }
        private static void ButtonStyle(Button button,Color normal,Sprite rounded)
        {
            ImageStyle(button.transform as RectTransform,Color.white,rounded,true,true);
            Undo.RecordObject(button,"Shop button states");button.targetGraphic=button.GetComponent<Image>();button.transition=Selectable.Transition.ColorTint;
            var c=button.colors;c.normalColor=normal;c.highlightedColor=Color.Lerp(normal,Color.white,.24f);
            c.selectedColor=c.highlightedColor;c.pressedColor=Color.Lerp(normal,Ink,.16f);c.disabledColor=normal==Teal?Muted:Hex("D4C9AA");c.fadeDuration=.1f;button.colors=c;
            if(button.GetComponent<SmoothButtonPunch>()==null)Undo.AddComponent<SmoothButtonPunch>(button.gameObject);
        }
        private static void TextStyle(TMP_Text text,TMP_FontAsset font,float max,Color color,TextAlignmentOptions alignment)
        {
            if(text==null)return;Undo.RecordObject(text,"Shop typography");
            var bodyFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/UI/Fonts/Nunito-SemiBold SDF.asset");
            bool heading=max>=28 || text.name=="Item Name" || text.name=="Buy Label";
            if(font!=null)text.font=heading?font:(bodyFont!=null?bodyFont:font);
            if(text.font!=null)text.fontSharedMaterial=text.font.material;
            text.fontStyle=FontStyles.Normal;text.color=color;text.fontSize=max;text.enableAutoSizing=true;text.fontSizeMin=14;text.fontSizeMax=max;
            text.alignment=alignment;text.raycastTarget=false;
        }
        private static void LayoutText(TMP_Text text,Transform parent,Vector2 pos,Vector2 size,TMP_FontAsset font,float max,Color color,TextAlignmentOptions alignment)
        {if(text==null)return;Place(text.rectTransform,parent,pos,size);TextStyle(text,font,max,color,alignment);}
        private static void SpinLabel(TMP_Text text,Button button,float width,TMP_FontAsset font)
        {if(text!=null && button!=null)LayoutText(text,button.transform,Vector2.zero,new(width-16,72),font,24,Ink,TextAlignmentOptions.Center);}
        private static void SetPreview(TMP_Text text,string value){if(text!=null){Undo.RecordObject(text,"Shop preview");text.text=value;}}
        private static void GemIcon(Transform parent,Vector2 pos,Vector2 size)
        {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(GemPath);if(sprite==null)return;
            var rect=Decoration(parent,"ShopStyledGemIcon");Place(rect,parent,pos,size);ImageStyle(rect,Color.white,sprite,false,false);
            var image=rect.GetComponent<Image>();image.type=Image.Type.Simple;image.preserveAspect=true;
        }
        private static Color Hex(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var color);return color;}
    }
}
#endif
