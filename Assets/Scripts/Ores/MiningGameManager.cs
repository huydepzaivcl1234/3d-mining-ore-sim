using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Serialized composition root for the mining gameplay systems.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningGameManager : MonoBehaviour
    {
        [Header("Core Systems")]
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private NpcShop npcShop;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;

        [Header("Presentation Systems")]
        [SerializeField] private MiningHud hud;
        [SerializeField] private MiningUpgradePanel upgradePanel;
        [SerializeField] private MiningAudioManager audioManager;
        [SerializeField] private MiningOrbitCamera orbitCamera;

        public PlayerWallet Wallet => wallet;
        public OreSpawner OreSpawner => oreSpawner;
        public NpcShop NpcShop => npcShop;
        public MiningUpgradeSystem UpgradeSystem => upgradeSystem;
        public MiningHud Hud => hud;
        public MiningUpgradePanel UpgradePanel => upgradePanel;
        public MiningAudioManager AudioManager => audioManager;
        public MiningOrbitCamera OrbitCamera => orbitCamera;

        public bool IsConfigured => wallet != null && oreSpawner != null && npcShop != null &&
                                    upgradeSystem != null && hud != null &&
                                    upgradePanel != null && audioManager != null && orbitCamera != null;
    }
}
