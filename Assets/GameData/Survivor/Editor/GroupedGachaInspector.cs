using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GameData.Player.Script;

namespace MiningSimulator.Ores.Editor
{
    [CustomEditor(typeof(SurvivorUpgradeData))]
    public sealed class SurvivorUpgradeInspector : UnityEditor.Editor
    { public override VisualElement CreateInspectorGUI()=>GroupedGachaInspector.Create(this,serializedObject,target); }
    [CustomEditor(typeof(MiningItemDatabase))]
    public sealed class ItemDatabaseGachaInspector : UnityEditor.Editor
    { public override VisualElement CreateInspectorGUI()=>GroupedGachaInspector.Create(this,serializedObject,target); }
    [CustomEditor(typeof(MiningItemData))]
    public sealed class ItemGachaInspector : UnityEditor.Editor
    { public override VisualElement CreateInspectorGUI()=>GroupedGachaInspector.Create(this,serializedObject,target); }

    public static class GroupedGachaInspector
    {
        private static readonly string[] Tiers={"common","uncommon","rare","epic","legendary"};
        public static VisualElement Create(UnityEditor.Editor editor,SerializedObject source,UnityEngine.Object target)
        {
            var root=new VisualElement();Action rebuild=null;
            rebuild=()=>{
                if(!target)return;root.Unbind();root.Clear();source.Update();
                var excluded=new HashSet<string>();
                if(target is SurvivorUpgradeData){excluded.UnionWith(new[]{"basicCards","abilityCards","definitions"});}
                if(target is MiningItemDatabase)excluded.Add("items");
                if(target is MiningShopData)excluded.Add("wheelRewards");
                if(target is MiningItemData item&&item.UseType==MiningItemUseType.GiftBox)excluded.Add("giftRewards");
                var it=source.GetIterator();bool next=it.NextVisible(true);
                while(next){if(!excluded.Contains(it.name)){var field=new PropertyField(it.Copy());if(it.name=="m_Script")field.SetEnabled(false);root.Add(field);}next=it.NextVisible(false);}
                root.Add(new HelpBox("Rarity: Legendary → Epic → Rare → Uncommon → Common. Mỗi % là lượt thử có điều kiện; Common = 100% khi các bậc trên trượt. Bảng trống/weight 0 được bỏ qua. Weight chỉ chia phần thưởng bên trong cùng rarity.",HelpBoxMessageType.Info));
                if(target is SurvivorUpgradeData upgrades){
                    root.Add(new HelpBox("Ability names, descriptions, formulas and effects are authored in the Ability Balance asset. The tables below only control rarity and selection weights.",HelpBoxMessageType.Info));
                    root.Add(new Button(()=>Selection.activeObject=upgrades.abilityData){text="Edit Ability Balance (Common / Uncommon / Rare / Epic)"});
                    root.Add(new Button(()=>Selection.activeObject=AssetDatabase.LoadAssetAtPath<SurvivorCombatData>("Assets/GameData/Survivor/SurvivorCombat.asset")){text="Edit base stats (damage / HP / armor / speed)"});
                    var definitions=new Foldout{text="BASIC STAT GAINS — amount per card",value=true};root.Add(definitions);
                    for(int i=0;i<upgrades.definitions.Length;i++){var d=upgrades.definitions[i];if(d==null||d.Ability||d.Retired)continue;var row=new Foldout{text=d.title,value=false};definitions.Add(row);var entry=source.FindProperty("definitions").GetArrayElementAtIndex(i);foreach(string field in new[]{"title","description","amount","unit","icon"})row.Add(new PropertyField(entry.FindPropertyRelative(field)));}
                    DrawCardPool(root,source,upgrades,"basicCards",false,rebuild);DrawCardPool(root,source,upgrades,"abilityCards",true,rebuild);}
                else if(target is MiningItemDatabase db)DrawItems(root,source,db,rebuild);
                else if(target is MiningShopData shop)DrawRewards(root,source,"wheelRewards",i=>shop.WheelRewards[i]?.Rarity??MiningItemRarity.Common,i=>shop.WheelRewards[i]!=null?shop.GetWheelRewardDisplayPercent(shop.WheelRewards[i]):0,rebuild);
                else if(target is MiningItemData gift&&gift.UseType==MiningItemUseType.GiftBox)DrawRewards(root,source,"giftRewards",i=>gift.GiftRewards[i]?.Rarity??MiningItemRarity.Common,i=>gift.GiftRewards[i]!=null?gift.GetGiftRewardDisplayPercent(gift.GiftRewards[i]):0,rebuild);
                root.Bind(source);
            };
            Undo.UndoRedoCallback undo=()=>rebuild();
            root.RegisterCallback<AttachToPanelEvent>(_=>Undo.undoRedoPerformed+=undo);
            root.RegisterCallback<DetachFromPanelEvent>(_=>Undo.undoRedoPerformed-=undo);
            rebuild();return root;
        }
        private static Foldout Group(VisualElement parent,int rarity,int count)
        {
            var fold=new Foldout{text=$"{((MiningItemRarity)rarity)} ({count})",value=count>0};
            fold.style.marginTop=8;fold.style.paddingLeft=6;fold.style.borderLeftWidth=3;
            fold.style.borderLeftColor=rarity==4?new Color(1,.72f,.22f):rarity==3?new Color(.7f,.6f,.93f):rarity==2?new Color(.36f,.65f,1):rarity==1?new Color(.45f,.83f,.69f):Color.gray;
            parent.Add(fold);return fold;
        }
        private static void DrawCardPool(VisualElement root,SerializedObject source,SurvivorUpgradeData data,string path,bool abilities,Action rebuild)
        {
            var poolProperty=source.FindProperty(path);var pool=abilities?data.abilityCards:data.basicCards;
            var section=new Foldout{text=abilities?"BLUE CARD — NĂNG LỰC":"RED CARD — CHỈ SỐ",value=true};root.Add(section);
            DrawRarityRates(section,source,data,path,abilities);
            for(int r=4;r>=0;r--){int tier=r;string listPath=path+"."+Tiers[r];var list=source.FindProperty(listPath);var group=Group(section,r,list.arraySize);
                for(int i=0;i<list.arraySize;i++){
                    int index=i;var entry=list.GetArrayElementAtIndex(i);var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.flexWrap=Wrap.Wrap;group.Add(row);
                    var id=new PropertyField(entry.FindPropertyRelative("id"),"");id.style.flexGrow=1;row.Add(id);
                    var weight=new FloatField("Weight"){isDelayed=true};weight.style.width=130;weight.labelElement.style.minWidth=48;weight.labelElement.style.width=48;weight.BindProperty(entry.FindPropertyRelative("weight"));row.Add(weight);
                    if(!abilities){var multiplier=new FloatField("×"){isDelayed=true};multiplier.style.width=85;multiplier.labelElement.style.minWidth=12;multiplier.labelElement.style.width=12;multiplier.BindProperty(entry.FindPropertyRelative("amountMultiplier"));row.Add(multiplier);}
                    var percent=new Label();percent.style.width=85;row.Add(percent);
                    row.schedule.Execute(()=>{if(!data)return;var entries=pool.Entries(tier);double total=entries.Sum(c=>c!=null?SequentialRarity.Weight(c.weight):0);percent.text=index<entries.Count&&entries[index]!=null&&total>0?$"{SequentialRarity.Weight(entries[index].weight)/total*100:0.##}%":"0%";}).Every(300);
                    row.Add(new Button(()=>{source.Update();source.FindProperty(listPath).DeleteArrayElementAtIndex(index);source.ApplyModifiedProperties();rebuild();}){text="−"});
                }
                group.Add(new Button(()=>{
                    var existing=pool.Entries(tier).Where(c=>c!=null).Select(c=>c.id).ToHashSet();var next=data.definitions.FirstOrDefault(d=>d!=null&&!d.Retired&&d.Ability==abilities&&!existing.Contains(d.id));if(next==null)return;
                    source.Update();var array=source.FindProperty(listPath);int index=array.arraySize;array.InsertArrayElementAtIndex(index);var entry=array.GetArrayElementAtIndex(index);entry.FindPropertyRelative("id").enumValueIndex=(int)next.id;entry.FindPropertyRelative("weight").floatValue=1;entry.FindPropertyRelative("amountMultiplier").floatValue=1;source.ApplyModifiedProperties();rebuild();
                }){text="+ Thêm thẻ vào bảng này"});
            }
        }
        private static void DrawRarityRates(VisualElement parent,SerializedObject source,SurvivorUpgradeData data,string path,bool abilities)
        {
            parent.Add(new Label("RARITY                         ROLL %                 EFFECTIVE %"));
            var pool=abilities?data.abilityCards:data.basicCards;
            for(int r=4;r>=0;r--){int tier=r;var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;parent.Add(row);
                var field=new FloatField(SurvivorUpgradeData.RarityName(r)){isDelayed=true};field.style.flexGrow=1;field.labelElement.style.minWidth=110;row.Add(field);
                string key=r==4?"legendaryPercent":r==3?"epicPercent":r==2?"rarePercent":"uncommonPercent";
                var property=source.FindProperty(path+".rarityChances."+key);field.SetValueWithoutNotify(r==0?100:property.floatValue);
                if(r==0)field.SetEnabled(false);else field.RegisterValueChangedCallback(e=>{float value=float.IsNaN(e.newValue)||float.IsInfinity(e.newValue)?0:Mathf.Clamp(e.newValue,0,100);source.Update();source.FindProperty(path+".rarityChances."+key).floatValue=value;source.ApplyModifiedProperties();field.SetValueWithoutNotify(value);});
                var actual=new Label();actual.style.width=135;row.Add(actual);
                actual.schedule.Execute(()=>{if(!data)return;double remaining=1,percent=0;bool available=false;
                    for(int i=4;i>=0;i--){bool has=pool.Entries(i).Any(c=>{var d=c==null?null:data.Find(c.id);return c!=null&&SequentialRarity.Weight(c.weight)>0&&d!=null&&!d.Retired&&d.Ability==abilities&&SequentialRarity.Weight(c.amountMultiplier)>0&&(abilities||SequentialRarity.Weight(d.amount)>0);});
                        if(!has){if(i==tier)break;continue;}double chance=pool.rarityChances.Chance((MiningItemRarity)i)*.01;if(i==tier){percent=remaining*chance*100;available=true;break;}remaining*=1-chance;}
                    actual.text=available?$"{percent:0.###}%":"0% (empty)";
                }).Every(250);
            }
            parent.Add(new HelpBox("Roll % is conditional on all higher tiers missing. Effective % skips empty tables and is per offer, before player level caps. Common is the guaranteed fallback when its table has eligible cards. Weight only changes selection within a tier.",HelpBoxMessageType.Info));
        }
        private static void DrawItems(VisualElement root,SerializedObject source,MiningItemDatabase data,Action rebuild)
        {
            for(int r=4;r>=0;r--){int tier=r;var indices=Enumerable.Range(0,data.Items.Count).Where(i=>data.Items[i]&&(int)data.Items[i].Rarity==tier).ToArray();var group=Group(root,r,indices.Length);
                foreach(int i in indices){int index=i;var item=data.Items[i];var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.flexWrap=Wrap.Wrap;group.Add(row);
                    var reference=new PropertyField(source.FindProperty("items").GetArrayElementAtIndex(i),"");reference.style.flexGrow=1;row.Add(reference);reference.RegisterValueChangeCallback(_=>root.schedule.Execute(rebuild));
                    var weight=new FloatField("Weight"){value=item.SelectionWeight,isDelayed=true};weight.style.width=130;weight.labelElement.style.minWidth=48;weight.labelElement.style.width=48;row.Add(weight);
                    weight.RegisterValueChangedCallback(e=>{if(!item)return;using(var so=new SerializedObject(item)){so.FindProperty("selectionWeight").floatValue=(float)SequentialRarity.Weight(e.newValue);so.ApplyModifiedProperties();}});
                    var percent=new Label();percent.style.width=85;row.Add(percent);row.schedule.Execute(()=>{if(data&&index<data.Items.Count)percent.text=$"{data.ItemSelectionPercent(index):0.###}% thật";}).Every(300);
                    row.Add(new Button(()=>{source.Update();var a=source.FindProperty("items");int n=a.arraySize;a.DeleteArrayElementAtIndex(index);if(a.arraySize==n)a.DeleteArrayElementAtIndex(index);source.ApplyModifiedProperties();rebuild();}){text="−"});
                }
                var add=new ObjectField("+ Thêm item"){objectType=typeof(MiningItemData),allowSceneObjects=false};group.Add(add);
                add.RegisterValueChangedCallback(e=>{var item=e.newValue as MiningItemData;if(!item||data.Items.Contains(item))return;using(var meta=new SerializedObject(item)){meta.FindProperty("rarity").enumValueIndex=tier;meta.ApplyModifiedProperties();}source.Update();var a=source.FindProperty("items");int n=a.arraySize;a.InsertArrayElementAtIndex(n);a.GetArrayElementAtIndex(n).objectReferenceValue=item;source.ApplyModifiedProperties();rebuild();});
            }
        }
        private static void DrawRewards(VisualElement root,SerializedObject source,string path,Func<int,MiningItemRarity> rarity,Func<int,float> percent,Action rebuild)
        {
            root.Add(new HelpBox("Rarity của Item lấy từ asset item. Currency Rarity chỉ áp dụng cho tiền/gem. % thật đã tính cả các lượt thử rarity phía trên.",HelpBoxMessageType.Info));
            var rewards=source.FindProperty(path);
            for(int r=4;r>=0;r--){int tier=r;var indices=Enumerable.Range(0,rewards.arraySize).Where(i=>(int)rarity(i)==tier).ToArray();var group=Group(root,r,indices.Length);
                foreach(int i in indices){int index=i;var element=new PropertyField(rewards.GetArrayElementAtIndex(i),$"Phần thưởng #{i+1}");group.Add(element);
                    element.RegisterValueChangeCallback(e=>{string p=e.changedProperty.propertyPath;if(p.EndsWith("currencyRarity")||p.EndsWith(".item")||p.EndsWith("rewardType"))root.schedule.Execute(rebuild);});
                    var chance=new Label();group.Add(chance);chance.schedule.Execute(()=>{if(source.targetObject&&index<source.FindProperty(path).arraySize)chance.text=$"Tỷ lệ thực tế: {percent(index):0.###}%";}).Every(300);
                    group.Add(new Button(()=>{source.Update();source.FindProperty(path).DeleteArrayElementAtIndex(index);source.ApplyModifiedProperties();rebuild();}){text="− Xóa phần thưởng"});
                }
                group.Add(new Button(()=>{source.Update();var a=source.FindProperty(path);int n=a.arraySize;a.InsertArrayElementAtIndex(n);var e=a.GetArrayElementAtIndex(n);e.FindPropertyRelative("rewardType").enumValueIndex=0;e.FindPropertyRelative("currencyRarity").enumValueIndex=tier;e.FindPropertyRelative("item").objectReferenceValue=null;e.FindPropertyRelative("itemAmount").intValue=1;var w=e.FindPropertyRelative(path=="giftRewards"?"chancePercent":"weight");w.floatValue=1;e.FindPropertyRelative(path=="giftRewards"?"moneyAmount":"currencyAmount").floatValue=100;source.ApplyModifiedProperties();rebuild();}){text="+ Thêm phần thưởng tiền vào bảng này"});
            }
        }
    }
}
