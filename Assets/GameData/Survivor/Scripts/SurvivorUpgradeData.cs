using System;
using System.Collections.Generic;
using UnityEngine;
using MiningSimulator.Ores;
namespace GameData.Player.Script
{
    public enum SurvivorUpgradeId { PhysicalDamage,AttackSpeed,ReloadSpeed,AttackRange,ProjectileSpeed,Armor,MagicResistance,MoveSpeed,Magazine,LifeSteal,Regeneration,MaxHealth,SplitShot,FleetForce,ArmorStrike,LifeSpring,PowerSurge,Burn,ProjectileSize,Shield,Airborne,Aura,Ricochet,Invulnerability,Quickening,FlatPower,TwinMagic,ReactiveArmor,Weaken,EmergencyHeal,Retaliate,BossVitality,DoubleBasics,OrbBarrage,TripleBasics,Frostbite,CardFortune,TrueBarrage,TitanBlessing,MissingHealthArmor,ChargedCube,CardMagnet,Count }
    [CreateAssetMenu(menuName="Survivor/Upgrade Cards")]
    public sealed class SurvivorUpgradeData : ScriptableObject
    {
        [Serializable] public sealed class Definition
        {
            public SurvivorUpgradeId id;
            public string title;
            [TextArea] public string description;
            [Min(.01f),Tooltip("Gain per card before rarity/sparkling bonuses. Flat damage for Attack Damage; percentage points for % stats.")] public float amount=1;
            public string unit="%";
            public Sprite icon;
            public bool Ability=>id>=SurvivorUpgradeId.SplitShot&&id!=SurvivorUpgradeId.ProjectileSize;
            public bool Retired=>id==SurvivorUpgradeId.Magazine||id==SurvivorUpgradeId.ReloadSpeed;
        }
        [Serializable] public struct Offer { public SurvivorUpgradeId id; public int rarity; public float amount; public bool shiny; }
        public SurvivorAbilityData abilityData;
        public string Title(SurvivorUpgradeId id)=>abilityData&&abilityData.Find(id)!=null?abilityData.Find(id).title:Find(id)?.title??"";
        public string Description(SurvivorUpgradeId id)=>abilityData&&abilityData.Find(id)!=null?abilityData.Description(id):Find(id)?.description??"";
        public const int LevelCap=99;
        [HideInInspector]
        [Min(0)] public float commonWeight=80,uncommonWeight=17,rareWeight=3;
        public Color commonColor=new(.65f,.70f,.66f),uncommonColor=new(.45f,.83f,.69f),rareColor=new(.70f,.60f,.93f);
        public Definition[] definitions={
            new(){id=SurvivorUpgradeId.MissingHealthArmor,title="Last Stand",description="Gain armor and magic resistance for every 10 missing health",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.CardMagnet,title="Card Magnet",description="Automatically pull nearby dropped cards toward you.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.ChargedCube,title="Arcane Cube",description="Charge a growing cube for 10 seconds, then launch it at the nearest enemy",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.TitanBlessing,title="Titan's Blessing",description="Gain maximum health, body size and physical damage. Flat health increases with each upgrade.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.PhysicalDamage,title="Attack Damage",description="Increase attack damage",amount=1,unit=" damage"},
            new(){id=SurvivorUpgradeId.AttackSpeed,title="Attack Speed",description="Increase attacks per second",amount=5},
            new(){id=SurvivorUpgradeId.ReloadSpeed,title="Quick Reload",description="Reduce automatic reload time",amount=10},
            new(){id=SurvivorUpgradeId.AttackRange,title="Attack Range",description="Increase automatic targeting range",amount=5},
            new(){id=SurvivorUpgradeId.ProjectileSpeed,title="Projectile Speed",description="Increase homing projectile speed",amount=5},
            new(){id=SurvivorUpgradeId.Armor,title="Armor",description="Increase base physical armor",amount=5,unit="%"},
            new(){id=SurvivorUpgradeId.MagicResistance,title="Magic Resistance",description="Increase base magic resistance",amount=5,unit="%"},
            new(){id=SurvivorUpgradeId.MoveSpeed,title="Movement Speed",description="Increase walking and running speed",amount=3},
            new(){id=SurvivorUpgradeId.Magazine,title="Magazine",description="Gain attacks before reloading",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.LifeSteal,title="Life Steal",description="Heal for a percentage of attack damage dealt",amount=5},
            new(){id=SurvivorUpgradeId.Regeneration,title="Regeneration",description="Increase base health restored per regeneration tick",amount=5,unit="%"},
            new(){id=SurvivorUpgradeId.MaxHealth,title="Vitality",description="Increase maximum health",amount=5},
            new(){id=SurvivorUpgradeId.SplitShot,title="Chain Shot",description="On hit, bounce to one other nearby enemy for 20% base damage + 1, then +1 per additional level. No bounce with only one enemy; cannot chain again.",amount=1},
            new(){id=SurvivorUpgradeId.FleetForce,title="Momentum",description="Every 20 bonus movement speed grants 1 attack damage, plus 10 per additional level.",amount=1},
            new(){id=SurvivorUpgradeId.ArmorStrike,title="Armored Strike",description="Hits deal an extra 5 + 2% of armor and magic resistance. +1 damage per additional level.",amount=1},
            new(){id=SurvivorUpgradeId.LifeSpring,title="Life Spring",description="Every 5 seconds, heal 1 + 5% maximum health. +5 healing per additional level.",amount=1},
            new(){id=SurvivorUpgradeId.PowerSurge,title="Power Surge",description="Gain 1 attack damage, plus 0.5 per additional level; increase bonus attack damage by 20%.",amount=1},
            new(){id=SurvivorUpgradeId.Burn,title="Ignite",description="Burn enemies for 3 seconds, dealing 0.25 per level + 2% of their maximum health as magic damage each second.",amount=1},
            new(){id=SurvivorUpgradeId.ProjectileSize,title="Projectile Size",description="Increase projectile size and impact area",amount=5,unit="%"},
            new(){id=SurvivorUpgradeId.Shield,title="Shield",description="Gain a shield on acquisition, upgrade or respawn: 15% maximum health + 10 per level. Fully recharges 15 seconds after breaking.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.Airborne,title="Airborne",description="10 hits within 3 seconds launch an enemy 4 m high. Landing deals 1% of enemy maximum health + 5 per level.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.Aura,title="Aura",description="Deal 1 magic damage every 0.5 seconds. Each additional level adds 1 damage and 0.25 m radius.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.Ricochet,title="Ricochet",description="On hit, bounce to one other nearby enemy for 25% base damage + 2 per level. Bounces only once.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.Invulnerability,title="Invulnerability",description="Become invulnerable for 1 second every 50 seconds. Each additional level reduces cooldown by 0.25 seconds.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.Quickening,title="Quickening",description="Gain 15% attack speed, plus 1% per additional level.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.FlatPower,title="Flat Power",description="Gain 1 attack damage, plus 2 per additional level.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.TwinMagic,title="Twin Magic",description="Fire a second projectile that deals 1 + 15% base damage as magic damage.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.ReactiveArmor,title="Reactive Armor",description="Each distinct enemy that hits you grants 1 armor and magic resistance. Stacks decay after 2 seconds without taking hits.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.Weaken,title="Weaken",description="Hits reduce enemy damage by 1 per level for 3 seconds. Does not stack or refresh while active.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.EmergencyHeal,title="Emergency Heal",description="At 30% health or less, recover 50% maximum health over 3 seconds. 70-second cooldown; -0.3 seconds per additional level.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.Retaliate,title="Retaliate",description="Every 5 enemy hits, strike the nearest enemy within 6 m for 15% armor + 15% magic resistance + 1 damage per level.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.BossVitality,title="Boss Vitality",description="Every 5 boss kills, permanently gain 1 + 1 maximum health per level. Progress is saved.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.OrbBarrage,title="Orb Barrage",description="Every 5 damage hits summon 5 homing orbs within 7 m.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.TripleBasics,title="Triple Choice",description="Receive 3 stat-card choices and increase stat-card drop chance.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.Frostbite,title="Frostbite",description="Hits chill enemies, slowing movement and increasing damage taken.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.CardFortune,title="Card Fortune",description="Increase stat-card and ability-card drop chances.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.TrueBarrage,title="True Barrage",description="Fire extra projectiles dealing true damage.",amount=1,unit=""},
            new(){id=SurvivorUpgradeId.DoubleBasics,title="Double Choice",description="Immediately receive two stat-card choices, each with a chance to be sparkling.",amount=1,unit=""}
        };
        [Tooltip("Extra reward percentage by rarity: Common, Uncommon, Rare, Epic, Legendary. Abilities do not use this bonus.")]
        public float[] basicRarityBonusPercent={0,3,5,10,20};
        [Header("Sparkling basic cards")]
        [Range(0,100)] public float shinyPercent=5;
        public static float ShinyBaseBonus(PlayerStats player,SurvivorUpgradeId id)
        {
            var core=player?player.CoreData:null;
            return id switch {
                SurvivorUpgradeId.ProjectileSize or SurvivorUpgradeId.AttackSpeed or SurvivorUpgradeId.AttackRange or SurvivorUpgradeId.ProjectileSpeed or SurvivorUpgradeId.MoveSpeed or SurvivorUpgradeId.MaxHealth=>20,
                SurvivorUpgradeId.PhysicalDamage=>(core?core.damage:0)*.2f,
                SurvivorUpgradeId.Armor=>20,
                SurvivorUpgradeId.MagicResistance=>20,
                SurvivorUpgradeId.Regeneration=>20,_=>0
            };
        }
        public Definition Find(SurvivorUpgradeId id)=>definitions==null?null:Array.Find(definitions,d=>d!=null&&d.id==id);
        public Color RarityColor(int rarity)=>rarity==4?legendaryColor:rarity==3?epicColor:rarity==2?rareColor:rarity==1?uncommonColor:commonColor;
        public static string RarityName(int rarity)=>rarity==4?"LEGENDARY":rarity==3?"EPIC":rarity==2?"RARE":rarity==1?"UNCOMMON":"COMMON";
        [Serializable] public sealed class WeightedCard
        {
            public SurvivorUpgradeId id;
            [Min(0)] public float weight=1;
            [Min(.01f)] public float amountMultiplier=1;
        }
        [Serializable] public sealed class CardPool
        {
            public SequentialRaritySettings rarityChances=new();
            public List<WeightedCard> legendary=new(),epic=new(),rare=new(),uncommon=new(),common=new();
            public List<WeightedCard> Entries(int rarity)=>rarity switch{4=>legendary,3=>epic,2=>rare,1=>uncommon,_=>common};
        }
        [Header("Red cards — stat upgrades")]
        public CardPool basicCards=new();
        [Header("Blue cards — abilities")]
        public CardPool abilityCards=new();
        [HideInInspector] public bool poolsConfigured;
        public Color epicColor=new(.70f,.60f,.93f),legendaryColor=new(1,.72f,.22f);
        private struct Candidate { public WeightedCard card; public int rarity; }
        private List<Candidate> Eligible(PlayerStats player,bool abilities)
        {
            var result=new List<Candidate>();var pool=abilities?abilityCards:basicCards;
            if(pool==null)return result;
            for(int r=4;r>=0;r--){var entries=pool.Entries(r);if(entries==null)continue;
                var seen=new HashSet<SurvivorUpgradeId>();
                foreach(var card in entries){if(card==null||!seen.Add(card.id)||SequentialRarity.Weight(card.weight)<=0)continue;
                    var def=Find(card.id);if(def==null||def.Retired||def.Ability!=abilities||(!def.Ability&&SequentialRarity.Weight(def.amount)<=0)||(player&&player.UpgradeLevel(card.id)>=LevelCap)||card.amountMultiplier<=0||float.IsNaN(card.amountMultiplier)||float.IsInfinity(card.amountMultiplier))continue;
                    result.Add(new Candidate{card=card,rarity=r});}}
            return result;
        }
        public bool HasEligible(PlayerStats player,bool abilities)=>Eligible(player,abilities).Count>0;
        public List<Offer> Roll(PlayerStats player,int count=3,bool abilities=false,float rarityMultiplier=1f)
        {
            var offers=new List<Offer>(Mathf.Max(0,count));var available=Eligible(player,abilities);var pool=abilities?abilityCards:basicCards;if(pool==null)return offers;
            var chances=pool.rarityChances;
            if(abilities&&rarityMultiplier>1&&!float.IsInfinity(rarityMultiplier))chances=new SequentialRaritySettings{
                legendaryPercent=Mathf.Min(100,chances.legendaryPercent*rarityMultiplier),epicPercent=Mathf.Min(100,chances.epicPercent*rarityMultiplier),
                rarePercent=Mathf.Min(100,chances.rarePercent*rarityMultiplier),uncommonPercent=Mathf.Min(100,chances.uncommonPercent*rarityMultiplier)};
            while(offers.Count<count&&SequentialRarity.TryRoll(available,chances,c=>(MiningItemRarity)c.rarity,c=>c.card.weight,out int index))
            {
                var pick=available[index];var def=Find(pick.card.id);float rarityBonus=basicRarityBonusPercent!=null&&pick.rarity<basicRarityBonusPercent.Length?Mathf.Max(0,basicRarityBonusPercent[pick.rarity]):0;float amount=def.Ability?1:def.amount*pick.card.amountMultiplier*(1+rarityBonus*.01f);
                if(float.IsNaN(amount)||float.IsInfinity(amount)||amount<=0)break;
                bool shiny=!def.Ability&&UnityEngine.Random.value<shinyPercent*.01f;
                if(shiny)amount+=ShinyBaseBonus(player,def.id);
                offers.Add(new Offer{id=def.id,rarity=pick.rarity,amount=amount,shiny=shiny});available.RemoveAll(c=>c.card.id==def.id);
            }
            return offers;
        }

    }
}
