using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Optional, asset-authored additions to the existing mining encounter table.</summary>
    [CreateAssetMenu(menuName = "Mining Simulator/Game Data/Monster Spawn Roster")]
    public sealed class MonsterSpawnRoster : ScriptableObject
    {
        [SerializeField] private List<MonsterSpawnEntry> entries = new();
        public IReadOnlyList<MonsterSpawnEntry> Entries => entries;
        [Tooltip("Shared daily event settings. A reference remains valid when its asset moves outside Resources.")]
        [SerializeField] private DailyEncounterEventData dailyEvents;
        public DailyEncounterEventData DailyEvents => dailyEvents;
    }
}
