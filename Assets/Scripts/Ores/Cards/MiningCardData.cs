using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum MiningCardChoice { Damage, AttackSpeed, Health }
    [CreateAssetMenu(menuName = "Mining Simulator/Card Drops")]
    public sealed class MiningCardData : ScriptableObject
    {
        [Serializable]
        public sealed class Tier
        {
            public string name;
            public GameObject prefab;
            public Color color = Color.red;
            [Min(0f)] public float weight = 1f;
            [Tooltip("Percentage points added to base damage. 10 means +10%.")]
            [Min(0f)] public float damage = 1f;
            [Tooltip("Percentage points added to base attack speed. 5 means +5%.")]
            [Min(0f)] public float attackSpeed = 5f;
            [Tooltip("Percentage points added to level-scaled base max health.")]
            [Min(0f)] public float health = 5f;
            public float Bonus(MiningCardChoice choice) => choice == MiningCardChoice.Damage ? damage : choice == MiningCardChoice.AttackSpeed ? attackSpeed : health;
        }
        [Range(0,100)] public float monsterDropPercent = 10f;
        [Range(0,100)] public float bossDropPercent = 50f;
        public Tier[] tiers = {
            new() {name="Common",color=Color.red,weight=70,damage=1,attackSpeed=3,health=5},
            new() {name="Uncommon",color=Color.green,weight=25,damage=3,attackSpeed=6,health=15},
            new() {name="Rare",color=Color.yellow,weight=5,damage=6,attackSpeed=12,health=30}
        };
        [Header("World drop")]
        [Min(0)] public float spawnHeight = 1f;
        [Min(0)] public float upwardImpulse = 3f;
        [Min(0)] public float sidewaysImpulse = 1.3f;
        [Min(.01f)] public float cardScale = .65f;
        [Min(.01f)] public float pickupRadius = .8f;
        [Min(0)] public float pickupDelay = 1.2f;
        [Min(0)] public float beamHeight = 10f;
        [Min(0)] public float beamWidth = .22f;
        public Material beamMaterial;
        [Header("Grounded card hover")]
        [Min(0)] public float hoverHeight = .35f;
        [Min(0)] public float bobAmplitude = .08f;
        [Min(0)] public float bobFrequency = .8f;
        public float rotationDegreesPerSecond = 65f;
        [Min(0)] public float hoverLiftDuration = .35f;
        [Range(0,1)] public float groundNormalMinimum = .6f;
        public LayerMask landingLayers = ~0;
        [Header("Choice panel")]
        public Vector2 panelSize = new(900,420);
        public Color panelColor = new(.035f,.045f,.07f,.98f);
        public Color buttonColor = new(.10f,.14f,.21f,1f);
        public Color hoverColor = new(.22f,.34f,.46f,1f);
        [Min(1f)] public float fontSize = 28f;
    }
}
