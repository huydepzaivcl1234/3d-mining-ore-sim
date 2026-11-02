using UnityEngine;
using UnityEngine.UI;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    // Compress presentation only; health, shield absorption and authored MicroBar animation stay unchanged.
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class PlayerShieldBar : MonoBehaviour
    {
        private CharacterHealth health;
        private SurvivorAbilities abilities;
        private RectTransform redGroup, shieldLayer;
        private Image white;
        private RectTransform[] fills;
        private Transform parent;
        private int[] originalOrder;
        private TMPro.TMP_Text healthText, shieldText;
        private Vector2 textSize, textPosition;
        private Color textColor;

        private void Awake(){health=GetComponent<CharacterHealth>();abilities=GetComponent<SurvivorAbilities>();}
        private void LateUpdate()=>Refresh();
        private bool Bind()
        {
            if(white)return true;
            if(!health||!health.HealthBar||!abilities)return false;
            var bar=health.HealthBar.Find("Styled Micro Bar");if(!bar)return false;
            var track=bar.Find("HP Empty Background") as RectTransform;
            var fill=bar.Find("HP Fill SVG Clip Window") as RectTransform;
            var ghost=bar.Find("HP Ghost SVG Clip Window") as RectTransform;
            if(!track||!fill||!ghost)return false;
            parent=bar;fills=new[]{ghost,fill};originalOrder=new[]{ghost.GetSiblingIndex(),fill.GetSiblingIndex()};
            redGroup=new GameObject("Health presentation",typeof(RectTransform)).GetComponent<RectTransform>();redGroup.SetParent(bar,false);
            redGroup.anchorMin=track.anchorMin;redGroup.anchorMax=track.anchorMax;redGroup.pivot=new Vector2(0,track.pivot.y);
            redGroup.sizeDelta=track.sizeDelta;redGroup.anchoredPosition=track.anchoredPosition-Vector2.right*track.rect.width*track.pivot.x;
            redGroup.SetSiblingIndex(Mathf.Min(originalOrder[0],originalOrder[1]));
            foreach(var layer in fills)layer.SetParent(redGroup,true);
            shieldLayer=new GameObject("Shield presentation",typeof(RectTransform)).GetComponent<RectTransform>();shieldLayer.SetParent(bar,false);
            shieldLayer.anchorMin=track.anchorMin;shieldLayer.anchorMax=track.anchorMax;shieldLayer.pivot=track.pivot;shieldLayer.anchoredPosition=track.anchoredPosition;shieldLayer.sizeDelta=track.sizeDelta;
            shieldLayer.SetSiblingIndex(redGroup.GetSiblingIndex()+1);
            white=new GameObject("Shield — white",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
            white.transform.SetParent(shieldLayer,false);white.color=Color.white;white.raycastTarget=false;
            var source=fill.GetComponentInChildren<Image>();if(source){white.sprite=source.sprite;white.type=source.sprite&&source.sprite.border!=Vector4.zero?Image.Type.Sliced:Image.Type.Simple;}
            white.rectTransform.offsetMin=white.rectTransform.offsetMax=Vector2.zero;
            healthText=bar.Find("Health")?.GetComponent<TMPro.TMP_Text>();
            if(healthText){
                textSize=healthText.rectTransform.sizeDelta;textPosition=healthText.rectTransform.anchoredPosition;textColor=healthText.color;
                shieldText=Instantiate(healthText,white.transform,false);shieldText.name="Shield amount";
                shieldText.rectTransform.anchorMin=Vector2.zero;shieldText.rectTransform.anchorMax=Vector2.one;shieldText.rectTransform.offsetMin=shieldText.rectTransform.offsetMax=Vector2.zero;
                shieldText.color=new Color(.16f,.1f,.13f);shieldText.alignment=TMPro.TextAlignmentOptions.Center;shieldText.raycastTarget=false;shieldText.enableAutoSizing=true;shieldText.fontSizeMax=healthText.fontSize;shieldText.fontSizeMin=9;
            }
            return true;
        }
        public void Refresh()
        {
            if(!Bind())return;
            float shield=health.Health>0?Mathf.Max(0,abilities.Shield):0;
            bool visible=shield>.0001f;
            white.gameObject.SetActive(visible);
            float capacity=Mathf.Max(1,health.MaxHealth)+(visible?Mathf.Max(shield,abilities.MaxShield):0);
            redGroup.localScale=new Vector3(health.MaxHealth/capacity,1,1);
            float start=Mathf.Clamp01(health.Health/capacity),end=Mathf.Clamp01((health.Health+shield)/capacity);
            white.rectTransform.anchorMin=new Vector2(start,0);white.rectTransform.anchorMax=new Vector2(end,1);
            white.rectTransform.offsetMin=white.rectTransform.offsetMax=Vector2.zero;
            if(shieldText){shieldText.text=Mathf.Ceil(shield).ToString("0");shieldText.enabled=white.rectTransform.rect.width>=18;}
            if(healthText){
                float shrink=shieldLayer.rect.width*(1-redGroup.localScale.x);
                bool fits=shieldLayer.rect.width-shrink>=90;
                healthText.rectTransform.sizeDelta=new Vector2(fits?Mathf.Max(60,textSize.x-shrink):textSize.x,textSize.y);
                healthText.rectTransform.anchoredPosition=textPosition-Vector2.right*(fits?shrink*healthText.rectTransform.pivot.x:0);
                healthText.color=fits?textColor:new Color(.16f,.1f,.13f);
            }
        }
        private void OnDisable(){if(redGroup)redGroup.localScale=Vector3.one;if(white)white.gameObject.SetActive(false);RestoreText();}
        private void RestoreText(){if(healthText){healthText.rectTransform.sizeDelta=textSize;healthText.rectTransform.anchoredPosition=textPosition;healthText.color=textColor;}}
        private void OnDestroy()
        {
            RestoreText();
            if(parent&&fills!=null)for(int i=0;i<fills.Length;i++)if(fills[i]){fills[i].SetParent(parent,true);fills[i].SetSiblingIndex(originalOrder[i]);}
            if(redGroup)Destroy(redGroup.gameObject);if(shieldLayer)Destroy(shieldLayer.gameObject);
        }
    }
}
