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
        [SerializeField] private MiningRebirthSystem rebirthSystem;

        [Header("Presentation Systems")]
        [SerializeField] private MiningHud hud;
        [SerializeField] private MiningUpgradePanel upgradePanel;
        [SerializeField] private MiningRebirthPanel rebirthPanel;
        [SerializeField] private MiningAudioManager audioManager;
        [SerializeField] private MiningAudioSettingsPanel audioSettingsPanel;
        [SerializeField] private MiningOrbitCamera orbitCamera;

        public PlayerWallet Wallet => wallet;
        public OreSpawner OreSpawner => oreSpawner;
        public NpcShop NpcShop => npcShop;
        public MiningUpgradeSystem UpgradeSystem => upgradeSystem;
        public MiningRebirthSystem RebirthSystem => rebirthSystem;
        public MiningHud Hud => hud;
        public MiningUpgradePanel UpgradePanel => upgradePanel;
        public MiningRebirthPanel RebirthPanel => rebirthPanel;
        public MiningAudioManager AudioManager => audioManager;
        public MiningAudioSettingsPanel AudioSettingsPanel => audioSettingsPanel;
        public MiningOrbitCamera OrbitCamera => orbitCamera;

        public bool IsConfigured => wallet != null && oreSpawner != null && npcShop != null &&
                                    upgradeSystem != null && rebirthSystem != null && hud != null &&
                                    upgradePanel != null && rebirthPanel != null &&
                                    audioManager != null && audioSettingsPanel != null &&
                                    orbitCamera != null;
    }
}
