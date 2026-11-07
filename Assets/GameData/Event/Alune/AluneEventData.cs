using UnityEngine;

namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Survivor/Events/Alune Meteor Shower")]
    public sealed class AluneEventData : ScriptableObject
    {
        [Header("Event — scaled game seconds")]
        [Min(1)] public float duration=120;
        [Min(.25f)] public float meteorInterval=3;
        [Min(0)] public float scatterRadius=10;
        [Header("Vertical fall and warning")]
        [Min(3)] public float fallHeight=60;
        [Min(1)] public float fallSeconds=7;
        [Min(.1f)] public float impactRadius=2;
        [Min(.1f)] public float meteorSize=1;
        public GameObject meteorPrefab, impactPrefab;
        public Shader warningShader;
        public Color warningColor=new(1,.04f,.01f,.7f);
        [Min(.1f)] public float impactVisualSeconds=1.5f;
        [Header("Monster impact — true damage")]
        [Range(0,100)] public float monsterMaxHealthPercent=20;
        [Header("Player impact — magic damage")]
        [Range(0,100)] public float playerImpactMaxHealthPercent=25;
        [Header("Proximity: slow, THEN stun and periodic magic damage")]
        [Min(.1f)] public float proximityDistance=2;
        [Range(0,99)] public float minimumSlowPercent=20, maximumSlowPercent=99;
        [Min(.01f)] public float slowSeconds=1, stunSeconds=1;
        [Range(0,100)] public float playerTickMaxHealthPercent=1;
        [Min(.1f)] public float tickSeconds=.25f;
    }
}
