#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Opt-in isolated scene fixture. No wallet, shop, progression or save services.</summary>
    [AddComponentMenu("")]
    public sealed class NavigationRepairSoak : MonoBehaviour
    {
        private readonly List<MiningNpc> miners = new();
        private readonly List<Ore> ores = new();
        private readonly List<ScriptableObject> settings = new();
        private OreSpawner spawner;
        private NpcData data;
        private float started, nextSample, nextImpact;
        private int phase = -1, samples, overlapSamples, invalidSamples, stalledSamples, mined;
        private int interpolatedInvalidSamples, interpolatedOverlapSamples;
        private double frameTotal;
        private int frames;
        private float maxFrame, maxPending;
        private bool fixedPopulation;
        private static readonly int[] Counts = { 1, 16, 32, 64, 100 };
        public bool Finished => Time.time - started >= 600;
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static GameObject Cube(string name, Vector3 point, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.position = point; go.transform.localScale = size; return go;
        }
        private void Start()
        {
            started = Time.time;
            Cube("Isolated floor", new Vector3(0, -.1f, 0), new Vector3(44, .2f, 44)).layer = Mathf.Max(0, LayerMask.NameToLayer("Ground"));
            var host = new GameObject("Navigation grid"); var grid = host.AddComponent<MiningNavGrid>();
            grid.ConfigureArea(Vector3.zero, new Vector2(40, 40));
            grid.Configure(.5f, .62f, 1.8f, 45, .5f, ~0, 512, 256);
            data = ScriptableObject.CreateInstance<NpcData>(); settings.Add(data);
            Set(data, "colliderRadius", .5f); Set(data, "colliderHeight", 1.8f);
            Set(data, "miningRange", 1f); Set(data, "moveSpeed", 3f);
            var spawnData = ScriptableObject.CreateInstance<OreSpawnData>(); settings.Add(spawnData);
            Set(spawnData, "spawnOnEnable", false);
            var system = new GameObject("Isolated ore system"); system.SetActive(false);
            spawner = system.AddComponent<OreSpawner>(); Set(spawner, "spawnData", spawnData);
            for (int i = 0; i < 12; i++)
            {
                var oreData = ScriptableObject.CreateInstance<OreData>(); settings.Add(oreData);
                Set(oreData, "durability", 100000000); Set(oreData, "maximumMiningNpcs", 3);
                Vector3 p = new((i % 4 - 1.5f) * 4, 1, (i / 4 - 1f) * 4);
                var oreObject = Cube("Test ore " + i, p, new Vector3(1.6f, 2, 1.6f));
                oreObject.transform.SetParent(system.transform, true);
                var ore = oreObject.AddComponent<Ore>(); ore.SetData(oreData); ores.Add(ore);
            }
            system.SetActive(true); Physics.SyncTransforms(); grid.Rebake();
            var camera = new GameObject("Soak camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 32, -22); camera.transform.LookAt(Vector3.zero);
            new GameObject("Soak sun").AddComponent<Light>().type = LightType.Directional;
            SetPopulation(1);
        }
        private void SetPopulation(int count)
        {
            foreach (var miner in miners) { miner.gameObject.SetActive(false); Destroy(miner.gameObject); }
            miners.Clear();
            Physics.SyncTransforms();
            int spawnIndex = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 spawn = default; bool found = false;
                for (; spawnIndex < 26 * 26; spawnIndex++)
                {
                    spawn = new Vector3(-18.75f + (spawnIndex % 26) * 1.5f, 0, -18.75f + (spawnIndex / 26) * 1.5f);
                    if (!MiningNavGrid.Instance.IsPointClear(spawn, .62f, 1.8f) ||
                        !MiningNpc.IsSpawnPositionClear(spawn, .62f)) continue;
                    found = true; spawnIndex++; break;
                }
                if (!found) throw new System.InvalidOperationException("Soak field has no valid spawn for miner " + i);
                var go = new GameObject("Soak miner " + i); go.SetActive(false);
                go.transform.position = spawn;
                var capsule = go.AddComponent<CapsuleCollider>(); capsule.center = Vector3.up * .9f;
                var miner = go.AddComponent<MiningNpc>(); Set(miner, "npcData", data); Set(miner, "oreSpawner", spawner);
                var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule); visual.transform.SetParent(go.transform, false);
                visual.transform.localPosition = Vector3.up * .9f; Destroy(visual.GetComponent<Collider>());
                go.SetActive(true); miners.Add(miner);
            }
        }
        public void BeginCrowdSoak(int count)
        {
            SetPopulation(count); fixedPopulation = true;
            started = Time.time; nextSample = Time.time + 1; nextImpact = Time.time + 1;
            samples = overlapSamples = invalidSamples = stalledSamples = mined = frames = 0;
            interpolatedInvalidSamples = interpolatedOverlapSamples = 0;
            frameTotal = 0; maxFrame = maxPending = 0;
        }
        private void Update()
        {
            if (Finished) return;
            int nextPhase = Mathf.Min(4, (int)((Time.time - started) / 120));
            if (!fixedPopulation && nextPhase != phase)
            { phase = nextPhase; if (miners.Count != Counts[phase]) SetPopulation(Counts[phase]); }
            frames++; frameTotal += Time.unscaledDeltaTime; maxFrame = Mathf.Max(maxFrame, Time.unscaledDeltaTime);
            if (Time.time >= nextImpact)
            {
                nextImpact = Time.time + 1;
                foreach (var miner in miners) if (miner.IsActivelyMining) { miner.OnMiningImpact(); mined++; }
            }
            if (Time.time < nextSample) return;
            nextSample = Time.time + 1; samples++;
            var grid = MiningNavGrid.Instance;
            foreach (var miner in miners)
            {
                maxPending = Mathf.Max(maxPending, miner.PendingPathAge);
                if (!grid.IsCapsulePlacementClear(miner.Profile.Foot(miner.RootPosition), miner.Profile.Radius, miner.Profile.Height)) invalidSamples++;
                if (!grid.IsCapsulePlacementClear(miner.Profile.Foot(miner.transform.position), miner.Profile.Radius, miner.Profile.Height)) interpolatedInvalidSamples++;
                if (miner.NavigationStatus == NavigationState.Unreachable ||
                    (miner.NavigationStatus == NavigationState.Idle && !miner.IsActivelyMining)) stalledSamples++;
            }
            for (int i = 0; i < miners.Count; i++) for (int j = i + 1; j < miners.Count; j++)
            {
                Vector3 gap = miners[i].RootPosition - miners[j].RootPosition; gap.y = 0;
                if (gap.sqrMagnitude < .99f * .99f) overlapSamples++;
                Vector3 renderGap = miners[i].transform.position - miners[j].transform.position; renderGap.y = 0;
                if (renderGap.sqrMagnitude < .99f * .99f) interpolatedOverlapSamples++;
            }
        }
        public object Snapshot()
        {
            var states = new Dictionary<string, int>();
            foreach (var miner in miners)
            {
                string state = miner.IsActivelyMining ? "Mining" : miner.NavigationStatus.ToString();
                states[state] = states.TryGetValue(state, out int count) ? count + 1 : 1;
            }
            return new { elapsed = Time.time - started, finished = Finished, population = miners.Count, states,
                samples, overlapSamples, invalidSamples, interpolatedOverlapSamples, interpolatedInvalidSamples, stalledSamples, miningImpacts = mined,
                maxPendingSeconds = maxPending, averageFrameMs = frames > 0 ? frameTotal / frames * 1000 : 0,
                maxFrameMs = maxFrame * 1000, pendingRequests = MiningNavGrid.Instance.PendingRequests };
        }
        private void OnDestroy() { foreach (var value in settings) if (value != null) Destroy(value); }
    }
}
#endif
