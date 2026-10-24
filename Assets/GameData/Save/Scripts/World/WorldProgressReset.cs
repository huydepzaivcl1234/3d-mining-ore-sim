using GameData.Player.Script;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Explicit gameplay reset; never clears controls, audio or graphics preferences.</summary>
    public static class WorldProgressReset
    {
        public static bool ResetAll()
        {
            if (!GameSave.ResetAll()) { Debug.LogError("Cannot reset save files. Progress preserved."); return false; }
            MiningDamagePopup.ClearAll();
            foreach (var cards in Loaded<MiningCardSystem>()) cards.CancelChoice();
            foreach (var monster in Loaded<MushroomMonster>()) Remove(monster);
            foreach (var god in Loaded<GodBossController>()) Remove(god);
            foreach (var bleed in Loaded<GodBossBleed>()) bleed.Clear();
            foreach (var projectile in Loaded<HomingMonsterProjectile>())
                if (projectile.gameObject.activeInHierarchy) SceneProjectilePool.Release(projectile.gameObject);
            foreach (var projectile in Loaded<TowerProjectile>())
                if (projectile.gameObject.activeInHierarchy) SceneProjectilePool.Release(projectile.gameObject);
            foreach (var card in Loaded<MiningWorldCard>()) Remove(card);
            foreach (var box in Loaded<BlueGunUpgradeBox>()) Remove(box);
            foreach (var special in Loaded<GunSpecialUpgrades>()) special.ResetProgress();
            foreach (var weapons in Loaded<PlayerWeaponProgression>()) weapons.ResetProgress();
            MiningPlayerStats.ResetSavedProgress();
            foreach (var upgrades in Loaded<MiningUpgradeSystem>()) { upgrades.SetPermanentMoneyMultiplier(1f); upgrades.SetPermanentExperienceMultiplier(1f); upgrades.ResetAutoUpgrade(); upgrades.ResetAllUpgrades(); }
            foreach (var items in Loaded<MiningItemSystem>()) items.ResetAllData();
            foreach (var achievements in Loaded<MiningAchievementSystem>()) achievements.ResetAllData();
            foreach (var quests in Loaded<MiningQuestSystem>()) quests.ResetAllData();
            MiningComputerStation.ResetAllLoadedStations();
            TreasureChest.ResetSavedProgress(); TowerWorldSave.ResetAllProgress();
            foreach (var wallet in Loaded<PlayerWallet>()) { wallet.ResetMoney(); wallet.ResetGems(); }
            foreach (var player in Loaded<PlayerRespawn>()) player.RespawnNow();
            DayNightSystem.ResetSavedClock(); MonsterSpawnZone.ResetLoadedEncounters();
            GameSave.Flush();
            return true;
        }
        private static T[] Loaded<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        private static void Remove(Component component) { component.gameObject.SetActive(false); Object.Destroy(component.gameObject); }
    }
}
