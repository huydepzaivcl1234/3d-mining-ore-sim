using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningAudioManager
    {






        private void HandleUpgradePurchased(MiningUpgradeType type)
        {
            if (upgradeSystem != null && upgradeSystem.IsAutoUpgradePurchase) return;
            if (audioData != null)
            {
                PlaySfx(audioData.UpgradePurchasedSfx);
            }
        }

        private void HandlePanelOpened()
        {
            if (audioData != null)
            {
                PlaySfx(audioData.PanelOpenSfx);
            }
        }

        private void HandlePanelClosed()
        {
            if (audioData != null)
            {
                PlaySfx(audioData.PanelCloseSfx);
            }
        }

        private void HandleRebirthCompleted(int count)
        {
            if (audioData != null)
            {
                PlaySfx(audioData.RebirthSfx);
            }
        }

        private void HandleWheelSpinStarted()
        {
            PlayWheelSpinSfx();
        }

        private void HandleWheelRewardGranted(MiningGiftReward reward)
        {
            PlayWheelRewardSfx();
        }

        private void HandlePeriodChanged(MiningTimePeriod period)
        {
            ambienceFadedForPeriodChange = false;
            if (coinRainAmbienceActive)
            {
                return;
            }

            if (period == MiningTimePeriod.Day)
            {
                PlaySunriseRooster();
            }

            if (!shopThemeActive && !mainMenuMusicActive)
            {
                PlayWorldAmbience(true);
            }
        }

        /// <summary>Crossfades from menu or shop music to the active world ambience.</summary>
    }
}
