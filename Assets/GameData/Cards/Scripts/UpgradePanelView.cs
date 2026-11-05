using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using Unity.VectorGraphics;

namespace MiningSimulator.Ores
{
    // A passive view: references and data only. RectTransforms and styling belong to the prefab author.
    [DisallowMultipleComponent]
    public sealed class UpgradePanelView : MonoBehaviour
    {
        [Serializable]
        public sealed class OfferSlot
        {
            public Button button;
            public TMP_Text name, description, rarity, category, number, previewCaption, previewValue, level;
            public Image rasterIcon, progress;
            public SVGImage vectorIcon;
            public UpgradeSurface frame, action;
            public TMP_Text actionLabel;
        }
        public enum IconSource { GameDataIcons, SanctuaryArtwork, AuthoredImages }
        [Header("Card design — one prefab for all three choices")]
        public UpgradeCardView cardPrefab;
        public RectTransform cardsRoot;
        [Tooltip("Game Data: use each upgrade's icon. Sanctuary: use the current artwork set. Authored: keep images you put in the prefab.")]
        public IconSource iconSource = IconSource.SanctuaryArtwork;
        public void ResolveSlots()
        {
            if(!cardsRoot)return;
            var found=new System.Collections.Generic.List<OfferSlot>();
            foreach(Transform child in cardsRoot){var card=child.GetComponent<UpgradeCardView>();if(card)found.Add(card.bindings);}
            if(found.Count==3)slots=found.ToArray();
        }
        [Header("Panel bindings — drag your own UI here")]
        public TMP_Text title, queueCount, playerLevel, ownedCount;
        public Button close, later, transparent;
        public TMP_Text transparentLabel;
        [Tooltip("Optional. Toggles only the full-screen scenery cover, never the panel itself.")]
        public GameObject[] sceneryCover;
        [Header("Three offers — transforms are never laid out by this component")]
        public OfferSlot[] slots = new OfferSlot[3];
        [Header("Current stats — optional")]
        public TMP_Text damage, attackSpeed, attackRange, projectileSpeed;
        [Header("Owned list — optional; use a Vertical Layout Group on Content")]
        public RectTransform ownedContent, ownedRowTemplate;
        public TMP_Text ownedEmpty;

#if UNITY_EDITOR
        [ContextMenu("Apply Card Prefab To All Three Choices")]
        public void ApplyCardPrefab()
        {
            if(Application.isPlaying||!cardPrefab||!cardsRoot)return;
            if(!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(cardPrefab)){Debug.LogWarning("Assign a card prefab from the Project window.",this);return;}
            if(cardPrefab.bindings?.button==null){Debug.LogWarning("Connect the Button in the card prefab bindings first.",this);return;}
            UnityEditor.Undo.RecordObject(this,"Replace upgrade card design");
            var old=slots;
            var next=new OfferSlot[3];
            for(int i=0;i<3;i++){
                var previous=old!=null&&i<old.Length?old[i]?.button:null;
                var previousView=previous?previous.GetComponentInParent<UpgradeCardView>():null;
                var previousRect=previousView?(RectTransform)previousView.transform:previous?(RectTransform)previous.transform:null;
                Vector3 center=previousRect?previousRect.TransformPoint(previousRect.rect.center):cardsRoot.TransformPoint(new Vector3(cardsRoot.rect.xMin+cardsRoot.rect.width*(i+.5f)/3,cardsRoot.rect.center.y,0));
                var instance=(UpgradeCardView)UnityEditor.PrefabUtility.InstantiatePrefab(cardPrefab,cardsRoot);
                UnityEditor.Undo.RegisterCreatedObjectUndo(instance.gameObject,"Replace upgrade card design");
                instance.name="Choice "+(i+1);
                var rect=(RectTransform)instance.transform;
                rect.position+=center-rect.TransformPoint(rect.rect.center);
                next[i]=instance.bindings;
            }
            if(old!=null)foreach(var slot in old)if(slot?.button&&slot.button.transform.IsChildOf(cardsRoot)){var card=slot.button.GetComponentInParent<UpgradeCardView>();UnityEditor.Undo.DestroyObjectImmediate(card?card.gameObject:slot.button.gameObject);}
            slots=next;
            UnityEditor.EditorUtility.SetDirty(this);
            if(gameObject.scene.IsValid())UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

        public static void Text(TMP_Text label, string value) { if (label) label.text=value; }
        public void SetIcon(OfferSlot slot, Sprite icon)
        {
            if(iconSource==IconSource.AuthoredImages)return;
            bool vector=icon&&icon.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color);
            if(slot.vectorIcon){slot.vectorIcon.gameObject.SetActive(vector);slot.vectorIcon.sprite=vector?icon:null;}
            if(slot.rasterIcon){slot.rasterIcon.gameObject.SetActive(!vector&&icon);slot.rasterIcon.sprite=vector?null:icon;}
        }
        public void SetOwned(string[] names,int[] levels,Sprite[] icons)
        {
            Text(ownedCount,names.Length.ToString());
            if(ownedEmpty)ownedEmpty.gameObject.SetActive(names.Length==0);
            if(!ownedContent||!ownedRowTemplate)return;
            // Keep the authored template separate from the generated entries.

            foreach(Transform child in ownedContent){if(child==ownedRowTemplate||child==ownedEmpty?.transform)continue;child.gameObject.SetActive(false);}
            for(int i=0;i<names.Length;i++){
                string key="Entry "+i;var row=ownedContent.Find(key) as RectTransform;
                if(!row){row=Instantiate(ownedRowTemplate,ownedContent);row.name=key;}
                row.gameObject.SetActive(true);row.SetAsLastSibling();
                Text(row.Find("Name")?.GetComponent<TMP_Text>(),names[i]);
                Text(row.Find("Level")?.GetComponent<TMP_Text>(),levels[i]+" / 99");
                var svg=row.Find("Vector icon")?.GetComponent<SVGImage>();var raster=row.Find("Raster icon")?.GetComponent<Image>();var icon=icons[i];bool vector=icon&&icon.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color);
                if(svg){svg.sprite=vector?icon:null;svg.gameObject.SetActive(vector);}if(raster){raster.sprite=vector?null:icon;raster.gameObject.SetActive(!vector&&icon);}
            }
            ownedRowTemplate.gameObject.SetActive(false);
        }
    }
}

