using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class SequentialRaritySettings
    {
        [Tooltip("Conditional chance: only tried when every higher tier misses. Common is the guaranteed fallback.")]
        [Range(0,100)] public float legendaryPercent=1;
        [Range(0,100)] public float epicPercent=5;
        [Range(0,100)] public float rarePercent=3;
        [Range(0,100)] public float uncommonPercent=17;
        public float Chance(MiningItemRarity tier)=>tier switch
        {
            MiningItemRarity.Legendary=>Percent(legendaryPercent),MiningItemRarity.Epic=>Percent(epicPercent),
            MiningItemRarity.Rare=>Percent(rarePercent),MiningItemRarity.Uncommon=>Percent(uncommonPercent),_=>100
        };
        private static float Percent(float n)=>float.IsNaN(n)||float.IsInfinity(n)?0:Mathf.Clamp(n,0,100);
    }

    // All reward systems use the same two steps. No rarity chance depends on item count or weight sum.
    public static class SequentialRarity
    {
        public static double Weight(float n)=>float.IsNaN(n)||float.IsInfinity(n)||n<=0?0:n;
        private static double[] Totals<T>(IReadOnlyList<T> entries,Func<T,MiningItemRarity> rarity,Func<T,float> weight)
        {
            var sums=new double[5];if(entries==null)return sums;
            foreach(var entry in entries){int r=(int)rarity(entry);if(r>=0&&r<5)sums[r]+=Weight(weight(entry));}
            return sums;
        }
        public static bool TryRoll<T>(IReadOnlyList<T> entries,SequentialRaritySettings settings,
            Func<T,MiningItemRarity> rarity,Func<T,float> weight,out int index,Func<float> sample=null)
        {
            index=-1;if(entries==null||settings==null)return false;
            var sums=Totals(entries,rarity,weight);int selected=-1;
            for(int r=4;r>=0;r--)
            {
                // Empty/exhausted tables are skipped rather than granting an invalid reward.
                if(sums[r]<=0)continue;
                float chance=settings.Chance((MiningItemRarity)r);
                if(chance>=100||(chance>0&&Next(sample)<chance*.01)){selected=r;break;}
            }
            if(selected<0)return false;
            double roll=Next(sample)*sums[selected];int last=-1;
            for(int i=0;i<entries.Count;i++)
            {
                if((int)rarity(entries[i])!=selected)continue;
                double w=Weight(weight(entries[i]));if(w<=0)continue;last=i;
                if(roll<w){index=i;return true;}roll-=w;
            }
            // Unity Random.value can return exactly 1; that selects the last positive-weight entry.
            index=last;return index>=0;
        }
        public static float DisplayPercent<T>(IReadOnlyList<T> entries,SequentialRaritySettings settings,
            Func<T,MiningItemRarity> rarity,Func<T,float> weight,int index)
        {
            if(entries==null||settings==null||index<0||index>=entries.Count)return 0;
            var sums=Totals(entries,rarity,weight);int selected=(int)rarity(entries[index]);
            if(selected<0||selected>=5||sums[selected]<=0)return 0;
            double remaining=1;
            for(int r=4;r>=0;r--)
            {
                if(sums[r]<=0)continue;double chance=settings.Chance((MiningItemRarity)r)*.01;
                if(r==selected)return (float)(remaining*chance*Weight(weight(entries[index]))/sums[r]*100);
                remaining*=1-chance;
            }
            return 0;
        }
        private static double Next(Func<float> sample)=>Mathf.Clamp01(sample==null?UnityEngine.Random.value:sample());
    }
}
