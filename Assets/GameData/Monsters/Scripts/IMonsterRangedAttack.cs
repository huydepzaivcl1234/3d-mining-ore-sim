using UnityEngine;

namespace MiningSimulator.Ores
{
    public interface IMonsterRangedAttack
    {
        bool IsReady { get; }
        string FireState { get; }
        float ReleaseMoment { get; }
        float Range { get; }
        bool CanReach(Transform victim);
        void Fire(MushroomMonster owner, Transform victim);
    }
}
