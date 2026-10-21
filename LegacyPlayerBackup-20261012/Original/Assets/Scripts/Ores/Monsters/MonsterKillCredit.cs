using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    // Published only after MushroomMonster's once-per-death/despawn guards.
    public static class MonsterKillCredit
    {
        public static event Action<MonsterRewardData, bool> Defeated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Defeated = null;

        public static bool IsPlayerOwned(GameObject source) => source != null &&
            (source.GetComponentInParent<PlayerCombatInput>(true) != null ||
             IsTowerOwned(source));

        public static bool IsTowerOwned(GameObject source) => source != null &&
            (source.GetComponentInParent<TowerRuntime>(true) != null ||
             source.GetComponentInParent<TowerProjectile>(true)?.HasPlayerOwner == true);

        public static void Notify(GameObject source, MonsterRewardData species, bool boss, float experience)
        {
            if (!IsPlayerOwned(source)) return;
            TreasureChest.Active?.GrantKillExperience(experience);
            Defeated?.Invoke(species, boss);
        }
    }
}
