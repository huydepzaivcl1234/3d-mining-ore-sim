using System;
using System.Collections.Generic;
using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed partial class MiningItemSystem
    {
        [Serializable] public sealed class SavedEffect {public string item;public float remaining,age;}
        public List<SavedEffect> CaptureRuntimeEffects(){var saved=new List<SavedEffect>();foreach(var effect in activeEffects.Values)if(effect.item&&effect.endTime>Time.time)saved.Add(new(){item=effect.item.ItemId,remaining=effect.endTime-Time.time,age=Time.time-effect.startTime});return saved;}
        public void RestoreRuntimeEffects(List<SavedEffect> saved){activeEffects.Clear();if(saved!=null&&database)foreach(var s in saved){var item=database.FindById(s.item);if(item&&s.remaining>0)activeEffects[item.EffectType]=new RuntimeEffect{item=item,startTime=Time.time-s.age,endTime=Time.time+s.remaining};}EffectsChanged?.Invoke();}
    }
}
