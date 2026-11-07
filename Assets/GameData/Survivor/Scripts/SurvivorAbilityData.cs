using System;
using System.Globalization;
using UnityEngine;

namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Survivor/Ability Balance")]
    public sealed class SurvivorAbilityData : ScriptableObject
    {
        [Serializable] public struct Scaling
        {
            [InspectorName("Starting Value (Level 1)")] public float baseValue;
            [InspectorName("Added Per Upgrade")] public float perLevel;
            public Scaling(float basis,float growth){baseValue=basis;perLevel=growth;}
            public float At(int level)=>baseValue+perLevel*Mathf.Max(0,Mathf.Clamp(level,1,SurvivorUpgradeData.LevelCap)-1);
            public override string ToString()=>perLevel==0?N(baseValue):$"{N(baseValue)} ({(perLevel<0?"-":"+")}{N(Mathf.Abs(perLevel))} per upgrade)";
        }
        [Serializable] public class Ability {public string title;}
        [Serializable] public sealed class LoopVisual
        {
            public GameObject prefab;
            [Tooltip("Follow the visible body, including airborne animation offsets (Ignite).")] public bool followBodyBounds;
            [Tooltip("Keep orientation and meter scale independent of actor rotation and size (Aura).")] public bool worldSpace;
            public Vector3 offset;
            public Vector3 scale=Vector3.one;
            [Min(.01f),Tooltip("Authored radius of the prefab. Aura scales horizontally to match its actual damage radius.")] public float referenceRadius=1;
        }
        [Serializable] public sealed class CardAttraction : Ability {public Scaling radius=new(2,.25f);[Min(.1f)]public float pullSpeed=8;}
        [Serializable] public sealed class Bounce : Ability { [Min(0)] public float baseDamagePercent;public Scaling flatDamage; }
        [Serializable] public sealed class Momentum : Ability { [Min(.01f)] public float bonusSpeedPerStack=20;public Scaling damagePerStack=new(1,10); }
        [Serializable] public sealed class ArmoredStrike : Ability {public Scaling flatDamage=new(5,1);[Min(0)] public float armorPercent=2,magicResistancePercent=2;}
        [Serializable] public sealed class Healing : Ability {[Min(.01f)]public float interval=5;public Scaling flatHealing=new(1,5);[Min(0)]public float maxHealthPercent=5;}
        [Serializable] public sealed class Power : Ability {public Scaling flatDamage=new(1,.5f);[Min(0)]public float bonusAttackDamagePercent=20;}
        [Serializable] public sealed class Ignite : Ability {public LoopVisual visual=new();public AudioClip[] sounds=Array.Empty<AudioClip>();[Range(0,1)]public float volume=.65f;public Scaling flatDamage=new(.25f,.25f);[Min(0)] public float enemyMaxHealthPercent=2;[Min(.1f)]public float tickInterval=1;[Min(.01f)]public float duration=3;}
        [Serializable] public sealed class Shield : Ability {public LoopVisual visual=new();[Min(0)]public float maxHealthPercent=15;public Scaling flatShield=new(10,10);[Min(0)]public float brokenRecovery=15;}
        [Serializable] public sealed class Airborne : Ability {[Min(1)]public int requiredHits=10;[Min(.01f)]public float hitWindow=3,height=4,flightDuration=1.2f;[Min(0)]public float enemyMaxHealthPercent=1;[Min(0),Tooltip("Per enemy, after landing. Hits during cooldown do not build a combo.")]public float cooldown=3;public Scaling landingDamage=new(5,5);}
        [Serializable] public sealed class Aura : Ability {public LoopVisual visual=new();[Min(.01f)]public float tickInterval=.5f;public Scaling damage=new(1,1),radius=new(1,.25f);}
        [Serializable] public sealed class Invulnerability : Ability {public Scaling cooldown=new(50,-.25f);[Min(.01f)]public float minimumCooldown=1,duration=1;}
        [Serializable] public sealed class StatBonus : Ability {public Scaling bonus;}
        [Serializable] public sealed class TwinMagic : Ability {[Min(0)]public float flatDamage=1,baseDamagePercent=15;}
        [Serializable] public sealed class AdaptiveArmor : Ability {[Min(0)]public float armorPerEnemy=1,magicResistancePerEnemy=1;[Min(.01f)]public float quietTime=2,decayInterval=1;}
        [Serializable] public sealed class Weaken : Ability {public Scaling damageReduction=new(1,1);[Min(.01f)]public float duration=3;}
        [Serializable] public sealed class Recovery : Ability {[Range(0,100)]public float healthThresholdPercent=30;[Min(0)]public float healMaxHealthPercent=50;[Min(.01f)]public float duration=3,minimumCooldown=1;public Scaling cooldown=new(70,-.3f);}
        [Serializable] public sealed class Retaliate : Ability {[Min(1)]public int incomingHits=5;[Min(.01f)]public float range=6;[Min(0)]public float armorPercent=15,magicResistancePercent=15;public Scaling flatDamage=new(1,1);}
        [Serializable] public sealed class BossVitality : Ability {[Min(1)]public int killsPerReward=5;public Scaling maxHealthReward=new(1,1);}
        [Serializable] public sealed class DoubleChoice : Ability {[Range(1,10)]public int choices=2;}
        [Serializable] public sealed class Orbs : Ability {[Min(1)]public int hitsRequired=5,orbCount=5;[Min(.1f)]public float range=7;public Scaling damage=new(1,1);}
        [Serializable] public sealed class TripleChoice : Ability {[Range(1,10)]public int choices=3;[Min(0)]public float basicDropBonus=10;}
        [Serializable] public sealed class Frost : Ability {public AudioClip sound;[Range(0,1)]public float volume=.65f;[Min(0),Tooltip("Minimum gap between frost sounds. One dedicated voice prevents overlaps across all enemies.")]public float soundInterval=.25f;[Range(0,100)]public float slowPercent=20;public Scaling damageTakenPercent=new(1,1);[Min(.1f)]public float duration=3;public Color tint=new(.25f,.65f,1);}
        [Serializable] public sealed class Fortune : Ability {public Scaling dropBonus=new(5,1);}
        [Serializable] public sealed class TrueShots : Ability {public Scaling projectileCount=new(1,1);[Min(0)]public float attackDamagePercent=5;}
        [Serializable] public sealed class Titan : Ability {[Min(0)]public float maxHealthPercent=35,sizePercent=15,physicalDamagePercent=15;public Scaling flatHealth=new(1,1);}
        [Serializable] public sealed class MissingHealthDefense : Ability {[Min(.01f)]public float missingHealthPerStack=10;public Scaling defensesPerStack=new(1,1);}
        [Serializable] public sealed class Cube : Ability {
            public AudioClip launchSound;
            public AudioClip[] impactSounds=Array.Empty<AudioClip>();
            [Range(0,1)]public float launchVolume=.8f,impactVolume=.85f;
            [Min(.1f)]public float chargeSeconds=10;
            public Scaling damage=new(100,10),radius=new(1,.25f),sizePercent=new(0,1);
            [Min(.01f)]public float fullSize=.65f,startingSize=.1f;
            public Vector3 chargeOffset=new(.8f,1f,0);
            [Min(1)]public float chargeSizeDivisor=8;
            [Min(0)]public float rotationDegreesPerSecond=36;
            public Color color=new(.65f,.4f,1);
            public MiningSimulator.Ores.CombatDamageType damageType=MiningSimulator.Ores.CombatDamageType.Magic;
        }
        [Serializable] public sealed class Epic
        {
            public TripleChoice tripleChoice=new(){title="Triple Choice"};
            public Frost frostbite=new(){title="Frostbite"};
            public Fortune cardFortune=new(){title="Card Fortune"};
            public TrueShots trueBarrage=new(){title="True Barrage"};
            public Titan titanBlessing=new(){title="Titan's Blessing"};
            public MissingHealthDefense missingHealthArmor=new(){title="Last Stand"};
            public Cube chargedCube=new(){title="Arcane Cube"};
        }
        [Serializable] public sealed class Common
        {
            public CardAttraction cardMagnet=new(){title="Card Magnet"};
            public Bounce chainShot=new(){title="Chain Shot",baseDamagePercent=20,flatDamage=new(1,1)};
            public Momentum momentum=new(){title="Momentum"};
            public ArmoredStrike armoredStrike=new(){title="Armored Strike"};
            public Healing lifeSpring=new(){title="Life Spring"};
            public Power powerSurge=new(){title="Power Surge"};
            public Ignite ignite=new(){title="Ignite"};
            public Orbs orbBarrage=new(){title="Orb Barrage"};
        }
        [Serializable] public sealed class Uncommon
        {
            public Shield shield=new(){title="Shield"};
            public Airborne airborne=new(){title="Airborne"};
            public Aura aura=new(){title="Aura"};
            public Bounce ricochet=new(){title="Ricochet",baseDamagePercent=25,flatDamage=new(2,2)};
            public Invulnerability invulnerability=new(){title="Invulnerability"};
            public StatBonus quickening=new(){title="Quickening",bonus=new(15,1)};
            public StatBonus flatPower=new(){title="Flat Power",bonus=new(1,2)};
        }
        [Serializable] public sealed class FlyingSword : Ability
        {
            public GameObject prefab,slashPrefab;
            [Min(.01f)] public float slashReferenceRadius=.17f;
            public float slashYawOffset;
            [Min(.01f),Tooltip("Thickness of the swept blade, not the full area radius.")] public float bladeThickness=.12f;
            public Scaling flatDamage=new(1,1);
            [Min(0)] public float attackDamagePercent=10,attackSpeedPercent=35;
            [Min(.1f)] public float cleaveRadius=2,orbitRadius=1.5f,orbitHeight=1.2f;
            [Range(90,330)] public float cleaveDegrees=240;
            [Min(.01f)] public float approachSeconds=.15f,swingSeconds=.35f,returnSeconds=.18f;
            public Color slashColor=new(.4f,.75f,1,.9f);
        }
        [Serializable] public sealed class Rare
        {
            public FlyingSword flyingSword=new(){title="Spectral Sword"};
            public TwinMagic twinMagic=new(){title="Twin Magic"};
            public AdaptiveArmor reactiveArmor=new(){title="Reactive Armor"};
            public Weaken weaken=new(){title="Weaken"};
            public Recovery emergencyHeal=new(){title="Emergency Heal"};
            public Retaliate retaliate=new(){title="Retaliate"};
            public BossVitality bossVitality=new(){title="Boss Vitality"};
            public DoubleChoice doubleChoice=new(){title="Double Choice"};
        }
        [Tooltip("Base Value is the level-1 value. Each later card upgrade adds Per Level once. Example: 0.25, 0.50, 0.75. Growth is additive.")]
        public Common common=new();
        public Uncommon uncommon=new();
        public Rare rare=new();
        public Epic epic=new();
        private static SurvivorAbilityData fallback;
        public static SurvivorAbilityData Fallback {get {if(!fallback){fallback=CreateInstance<SurvivorAbilityData>();fallback.hideFlags=HideFlags.HideAndDontSave;}return fallback;}}
        private static string N(float value)=>value.ToString("0.##",CultureInfo.InvariantCulture);
        public Ability Find(SurvivorUpgradeId id)=>id switch{
            SurvivorUpgradeId.CardMagnet=>common.cardMagnet,SurvivorUpgradeId.SplitShot=>common.chainShot,SurvivorUpgradeId.FleetForce=>common.momentum,SurvivorUpgradeId.ArmorStrike=>common.armoredStrike,
            SurvivorUpgradeId.LifeSpring=>common.lifeSpring,SurvivorUpgradeId.PowerSurge=>common.powerSurge,SurvivorUpgradeId.Burn=>common.ignite,
            SurvivorUpgradeId.Shield=>uncommon.shield,SurvivorUpgradeId.Airborne=>uncommon.airborne,SurvivorUpgradeId.Aura=>uncommon.aura,
            SurvivorUpgradeId.Ricochet=>uncommon.ricochet,SurvivorUpgradeId.Invulnerability=>uncommon.invulnerability,SurvivorUpgradeId.Quickening=>uncommon.quickening,SurvivorUpgradeId.FlatPower=>uncommon.flatPower,
            SurvivorUpgradeId.FlyingSword=>rare.flyingSword,SurvivorUpgradeId.TwinMagic=>rare.twinMagic,SurvivorUpgradeId.ReactiveArmor=>rare.reactiveArmor,SurvivorUpgradeId.Weaken=>rare.weaken,
            SurvivorUpgradeId.EmergencyHeal=>rare.emergencyHeal,SurvivorUpgradeId.Retaliate=>rare.retaliate,SurvivorUpgradeId.BossVitality=>rare.bossVitality,SurvivorUpgradeId.DoubleBasics=>rare.doubleChoice,
            SurvivorUpgradeId.OrbBarrage=>common.orbBarrage,SurvivorUpgradeId.TripleBasics=>epic.tripleChoice,SurvivorUpgradeId.Frostbite=>epic.frostbite,SurvivorUpgradeId.CardFortune=>epic.cardFortune,SurvivorUpgradeId.TrueBarrage=>epic.trueBarrage,SurvivorUpgradeId.TitanBlessing=>epic.titanBlessing,SurvivorUpgradeId.MissingHealthArmor=>epic.missingHealthArmor,SurvivorUpgradeId.ChargedCube=>epic.chargedCube,_=>null};
        public string Description(SurvivorUpgradeId id)
        {
            switch(id){
            case SurvivorUpgradeId.CardMagnet:var magnet=common.cardMagnet;return $"Pull dropped cards within {magnet.radius} m.";
            case SurvivorUpgradeId.SplitShot:var c=common.chainShot;return $"On hit, bounce once to another nearby enemy. Physical damage: {N(c.baseDamagePercent)}% base damage + ({c.flatDamage}). Requires another enemy; no further bounces.";
            case SurvivorUpgradeId.FleetForce:var m=common.momentum;return $"For every {N(m.bonusSpeedPerStack)} bonus movement speed, gain ({m.damagePerStack}) attack damage. Only speed gained from upgrades, items and other bonuses counts.";
            case SurvivorUpgradeId.ArmorStrike:var a=common.armoredStrike;return $"Attack hits deal extra physical damage: ({a.flatDamage}) + {N(a.armorPercent)}% armor + {N(a.magicResistancePercent)}% magic resistance.";
            case SurvivorUpgradeId.LifeSpring:var h=common.lifeSpring;return $"Every {N(h.interval)}s, heal ({h.flatHealing}) + {N(h.maxHealthPercent)}% of your maximum health.";
            case SurvivorUpgradeId.PowerSurge:var p=common.powerSurge;return $"Gain ({p.flatDamage}) attack damage, plus {N(p.bonusAttackDamagePercent)}% of your bonus attack damage. Base attack damage is not multiplied.";
            case SurvivorUpgradeId.Burn:var b=common.ignite;return $"+1 fire bolt per attack at every level. Only fire bolts burn; bounces retain burn. For {N(b.duration)}s, every {N(b.tickInterval)}s: ({b.flatDamage}) + {N(b.enemyMaxHealthPercent)}% enemy maximum health as magic damage.";
            case SurvivorUpgradeId.Shield:var s=uncommon.shield;return $"Gain a shield on acquisition, upgrade or respawn: {N(s.maxHealthPercent)}% maximum health + ({s.flatShield}). When broken, fully restores after {N(s.brokenRecovery)}s; hits do not reset this timer.";
            case SurvivorUpgradeId.Airborne:var k=uncommon.airborne;return $"{k.requiredHits} hits within {N(k.hitWindow)}s launch an enemy {N(k.height)} m upward. Landing deals {N(k.enemyMaxHealthPercent)}% enemy maximum health + ({k.landingDamage}) physical damage. Each enemy has a {N(k.cooldown)}s cooldown after landing; hits during cooldown do not build a new combo.";
            case SurvivorUpgradeId.Aura:var u=uncommon.aura;return $"An aura beneath you deals ({u.damage}) magic damage every {N(u.tickInterval)}s to enemies within ({u.radius}) m.";
            case SurvivorUpgradeId.Ricochet:var r=uncommon.ricochet;return $"On hit, bounce once to another enemy for {N(r.baseDamagePercent)}% base damage + ({r.flatDamage}) physical damage. Only one bounce; cannot loop.";
            case SurvivorUpgradeId.Invulnerability:var i=uncommon.invulnerability;return $"Every ({i.cooldown})s, become invulnerable for {N(i.duration)}s. Minimum cooldown: {N(i.minimumCooldown)}s.";
            case SurvivorUpgradeId.Quickening:return $"Gain ({uncommon.quickening.bonus})% attack speed.";
            case SurvivorUpgradeId.FlatPower:return $"Gain ({uncommon.flatPower.bonus}) attack damage.";
            case SurvivorUpgradeId.FlyingSword:var sword=rare.flyingSword;return $"Summon one orbiting sword. Sweep the sword through enemies in your attack range with a {N(sword.cleaveDegrees)}-degree crescent slash, dealing ({sword.flatDamage}) + {N(sword.attackDamagePercent)}% of your attack damage as magic damage in a {N(sword.cleaveRadius)} m arc. Each enemy is hit once per swing. Attacks at {N(sword.attackSpeedPercent)}% of your attack speed. Upgrades strengthen the same sword.";
            case SurvivorUpgradeId.TwinMagic:var t=rare.twinMagic;return $"Fire two projectiles simultaneously at two different enemies in attack range (both target the same enemy when only one is available). The second deals {N(t.flatDamage)} + {N(t.baseDamagePercent)}% base damage as magic damage.";
            case SurvivorUpgradeId.ReactiveArmor:var d=rare.reactiveArmor;return $"Each distinct enemy that hits you grants {N(d.armorPerEnemy)} armor and {N(d.magicResistancePerEnemy)} magic resistance. After {N(d.quietTime)}s without hits, lose one stack every {N(d.decayInterval)}s.";
            case SurvivorUpgradeId.Weaken:var w=rare.weaken;return $"Damage reduces enemy damage dealt by ({w.damageReduction}) for {N(w.duration)}s. Does not stack; hits while active do not reset its duration.";
            case SurvivorUpgradeId.EmergencyHeal:var e=rare.emergencyHeal;return $"At {N(e.healthThresholdPercent)}% HP or less, heal {N(e.healMaxHealthPercent)}% maximum health over {N(e.duration)}s. Cooldown: ({e.cooldown})s, minimum {N(e.minimumCooldown)}s.";
            case SurvivorUpgradeId.Retaliate:var f=rare.retaliate;return $"Every {f.incomingHits} enemy hits, strike the nearest enemy within {N(f.range)} m for {N(f.armorPercent)}% armor + {N(f.magicResistancePercent)}% magic resistance + ({f.flatDamage}) physical damage.";
            case SurvivorUpgradeId.BossVitality:var v=rare.bossVitality;return $"Every {v.killsPerReward} boss kills, permanently gain ({v.maxHealthReward}) maximum health. Boss-kill progress is saved.";
            case SurvivorUpgradeId.DoubleBasics:return $"Receive {rare.doubleChoice.choices} stat-card choices. Each choice can offer sparkling stat cards.";
            case SurvivorUpgradeId.OrbBarrage:var o=common.orbBarrage;return $"Every {o.hitsRequired} damage hits summon {o.orbCount} homing orbs at random enemies within {N(o.range)} m, each dealing {o.damage} physical damage. With one enemy, all orbs target it. Orb hits cannot trigger another barrage.";
            case SurvivorUpgradeId.TripleBasics:var tc=epic.tripleChoice;return $"Receive {tc.choices} stat-card choices; each can be sparkling. While owned, gain +{N(tc.basicDropBonus)} percentage points to stat-card drop chance (does not stack with levels).";
            case SurvivorUpgradeId.Frostbite:var fr=epic.frostbite;return $"+1 ice bolt per attack at every level. Only ice bolts chill for {N(fr.duration)}s: -{N(fr.slowPercent)}% movement speed, +{fr.damageTakenPercent}% damage taken. Bounces retain chill. Active chill cannot refresh.";
            case SurvivorUpgradeId.CardFortune:return $"Gain {epic.cardFortune.dropBonus} percentage points to both stat-card and ability-card drop chances. Combined drop chance cannot exceed 100%.";
            case SurvivorUpgradeId.TrueBarrage:var tr=epic.trueBarrage;return $"Each attack fires {tr.projectileCount} extra projectiles, each dealing {N(tr.attackDamagePercent)}% of your current attack damage as true damage. Prefer other enemies in attack range; focus the sole enemy when alone.";
            case SurvivorUpgradeId.TitanBlessing:var titan=epic.titanBlessing;return $"While owned, gain {N(titan.maxHealthPercent)}% maximum health, {N(titan.sizePercent)}% body size and {N(titan.physicalDamagePercent)}% physical damage. Also gain {titan.flatHealth} flat maximum health. The percentage bonuses do not stack with levels.";
            case SurvivorUpgradeId.MissingHealthArmor:var mh=epic.missingHealthArmor;return $"For every {N(mh.missingHealthPerStack)} currently missing HP, gain {mh.defensesPerStack} armor and magic resistance per stack. Healing reduces the bonus.";
            case SurvivorUpgradeId.ChargedCube:var cc=epic.chargedCube;return $"Every {N(cc.chargeSeconds)}s, charge a growing, rotating cube and launch it at the nearest enemy. Deals {cc.damage} + 10% of each enemy's missing health as {cc.damageType.ToString().ToLowerInvariant()} damage in a {cc.radius} m radius. Cube size bonus: {cc.sizePercent}%. Holds its charge when no target is in range.";
            default:return "";
            }
        }
    }
}

