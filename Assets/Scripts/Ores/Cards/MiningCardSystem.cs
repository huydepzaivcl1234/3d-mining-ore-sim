using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent]
    public sealed class MiningCardSystem : MonoBehaviour
    {
        [SerializeField] private MiningCardData data;
        [SerializeField] private MiningPlayerStats player;
        [SerializeField] private MiningUiPanelCoordinator coordinator;
        [SerializeField] private RectTransform choicePanel;
        [SerializeField] private TMP_Text title;
        [SerializeField] private UnityEngine.UI.Button[] choices;
        private MiningCardData.Tier pending;
        private MiningCharacterHealth playerHealth;
        public MiningPlayerStats Player => player;
        public bool CanCollect => pending==null && player!=null && playerHealth!=null && playerHealth.Health>0 &&
            (coordinator==null || !coordinator.BlocksGameplay);
        public void Configure(MiningCardData settings, MiningPlayerStats target, MiningUiPanelCoordinator ui,
            RectTransform panel, TMP_Text heading, UnityEngine.UI.Button[] buttons)
        { data=settings;player=target;coordinator=ui;choicePanel=panel;title=heading;choices=buttons; Bind(); }
        private void Awake() { Bind(); if(choicePanel!=null) choicePanel.gameObject.SetActive(false); }
        private void Update()
        {
            // If another modal interrupted the choice, offer it again once that modal closes.
            if(pending!=null && choicePanel!=null && !choicePanel.gameObject.activeInHierarchy &&
                playerHealth!=null && playerHealth.Health>0 && (coordinator==null || !coordinator.BlocksGameplay))
            {
                if(coordinator!=null) coordinator.OpenPanel(choicePanel); else choicePanel.gameObject.SetActive(true);
            }
        }
        private void Bind()
        {
            if(player!=null) playerHealth=player.GetComponent<MiningCharacterHealth>();
            if(choices==null || choices.Length<3) return;
            if (choices.Length < 5 && choicePanel != null)
            {
                var expanded = new UnityEngine.UI.Button[5];
                System.Array.Copy(choices, expanded, choices.Length);
                for (int i = choices.Length; i < 5; i++)
                {
                    string name = ((MiningCardChoice)i).ToString();
                    var authored = choicePanel.Find(name);
                    expanded[i] = authored != null ? authored.GetComponent<UnityEngine.UI.Button>() : null;
                    if (expanded[i] == null) expanded[i] = Instantiate(choices[2], choicePanel);
                    expanded[i].name = name;
                    expanded[i].onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                }
                choices = expanded;
                choicePanel.sizeDelta = new Vector2(choicePanel.sizeDelta.x, Mathf.Max(choicePanel.sizeDelta.y, 600f));
                for (int i = 0; i < 5; i++)
                {
                    var rect = (RectTransform)choices[i].transform;
                    int column = i < 3 ? i : i - 3;
                    rect.anchorMin = new Vector2(.04f + column * .32f, i < 3 ? .43f : .06f);
                    rect.anchorMax = new Vector2(.31f + column * .32f, i < 3 ? .74f : .37f);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
            }
            choices[0].onClick.RemoveListener(ChooseDamage); choices[0].onClick.AddListener(ChooseDamage);
            choices[1].onClick.RemoveListener(ChooseSpeed); choices[1].onClick.AddListener(ChooseSpeed);
            choices[2].onClick.RemoveListener(ChooseHealth); choices[2].onClick.AddListener(ChooseHealth);
            if (choices.Length >= 5)
            {
                choices[3].onClick.RemoveListener(ChooseRegen); choices[3].onClick.AddListener(ChooseRegen);
                choices[4].onClick.RemoveListener(ChooseHealing); choices[4].onClick.AddListener(ChooseHealing);
            }
        }
        public bool TryDrop(Vector3 position, bool boss)
        {
            if(data==null || Random.value*100f >= (boss?data.bossDropPercent:data.monsterDropPercent)) return false;
            float total=0f;
            foreach(var tier in data.tiers) if(tier!=null && tier.prefab!=null) total+=Mathf.Max(0,tier.weight);
            if(total<=0f) return false;
            float roll=Random.value*total;
            foreach(var tier in data.tiers)
            {
                if(tier==null || tier.prefab==null || tier.weight<=0f) continue;
                roll-=tier.weight;
                if(roll<=0f) { Spawn(tier,position); return true; }
            }
            return false;
        }
        public GameObject Spawn(MiningCardData.Tier tier, Vector3 position)
        {
            if(data==null || tier==null || tier.prefab==null) return null;
            var root=new GameObject("World Card - "+tier.name);
            root.transform.position=position+Vector3.up*data.spawnHeight;
            var visual=Instantiate(tier.prefab,root.transform);
            visual.transform.localScale*=data.cardScale;
            // Keep authored visual intact, but do not let nested prefab colliders collect/block the player.
            foreach(var c in visual.GetComponentsInChildren<Collider>(true)) c.enabled=false;
            foreach(var rb in visual.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic=true;
            var renderers=visual.GetComponentsInChildren<Renderer>();
            Bounds bounds=renderers.Length>0?renderers[0].bounds:new Bounds(root.transform.position,Vector3.one*.05f);
            foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            // FBX card variants have different authored X offsets. Centre each spawned visual only.
            visual.transform.position-=bounds.center-root.transform.position;
            var box=root.AddComponent<BoxCollider>(); box.center=Vector3.zero;
            box.size=Vector3.Max(bounds.size,Vector3.one*.05f);
            var body=root.AddComponent<Rigidbody>();
            body.AddForce(Vector3.up*data.upwardImpulse+Vector3.ProjectOnPlane(Random.onUnitSphere,Vector3.up)*data.sidewaysImpulse,ForceMode.Impulse);
            root.AddComponent<MiningWorldCard>().Configure(this,data,tier);
            return root;
        }
        public bool Collect(MiningCardData.Tier tier)
        {
            if(!CanCollect || tier==null || choicePanel==null || choices==null || choices.Length<3) return false;
            pending=tier;
            title.text=MiningLocalization.Text(tier.name,tier.name)+" — "+MiningLocalization.Text("CARD_CHOOSE","Chọn một chỉ số");
            string[] keys={"CARD_DAMAGE","CARD_AS","CARD_HEALTH","CARD_REGEN_INTERVAL","CARD_HEALING"};
            string[] fallback={"Sát thương","Tốc đánh","Máu tối đa","Giảm thời gian hồi máu","Tăng hiệu quả hồi máu"};
            for(int i=0;i<choices.Length && i<keys.Length;i++)
            {
                choices[i].interactable=true;
                choices[i].GetComponentInChildren<TMP_Text>().text=MiningLocalization.Text(keys[i],fallback[i])+"\n"+(i==3?"-":"+")+tier.Bonus((MiningCardChoice)i).ToString("0.##")+"%";
            }
            if(coordinator!=null) coordinator.OpenPanel(choicePanel); else choicePanel.gameObject.SetActive(true);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            return true;
        }
        private void ChooseDamage()=>Choose(MiningCardChoice.Damage);
        private void ChooseSpeed()=>Choose(MiningCardChoice.AttackSpeed);
        private void ChooseHealth()=>Choose(MiningCardChoice.Health);
        private void ChooseRegen()=>Choose(MiningCardChoice.RegenInterval);
        private void ChooseHealing()=>Choose(MiningCardChoice.HealingEffectiveness);
        public bool Choose(MiningCardChoice choice)
        {
            if(pending==null || player==null || (int)choice<0 || (int)choice>4) return false;
            if(!player.AddCardBonus(choice,pending.Bonus(choice))) return false;
            pending=null;
            foreach(var button in choices) button.interactable=false;
            if(coordinator!=null) coordinator.ClosePanel(choicePanel); else choicePanel.gameObject.SetActive(false);
            return true;
        }
    }
}
