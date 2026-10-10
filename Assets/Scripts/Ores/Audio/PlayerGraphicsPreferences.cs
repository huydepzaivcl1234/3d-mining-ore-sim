using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Player choices, separate from world/progression saves and lighting presets.</summary>
    [Serializable]
    public sealed class PlayerGraphicsPreferences
    {
        public const string SaveKey = "MiningSimulator.Graphics.v1";
        public int version = 1;
        public bool fullscreen, vsync, postProcessing = true;
        public int width, height;
        public uint refreshNumerator, refreshDenominator = 1;
        public int aaMode, aaQuality = 2, msaa = 1, cascades = 4;
        public float renderScale = 1f, shadowDistance = 50f;
        public bool bloom = true, vignette = true, motionBlur, depthOfField, ambientOcclusion = true;
        public float bloomMultiplier = 1f, exposureOffset, gammaOffset;

        public void Sanitize()
        {
            aaMode = Mathf.Clamp(aaMode, 0, 2); // None, FXAA, SMAA; no unsupported TAA/MSAA combination.
            aaQuality = Mathf.Clamp(aaQuality, 0, 2);
            msaa = msaa <= 1 ? 1 : msaa <= 2 ? 2 : msaa <= 4 ? 4 : 8;
            cascades = Mathf.Clamp(cascades, 1, 4);
            renderScale = FiniteRange(renderScale, .5f, 1.5f, 1f);
            shadowDistance = FiniteRange(shadowDistance, 0f, 150f, 50f);
            bloomMultiplier = FiniteRange(bloomMultiplier, 0f, 3f, 1f);
            exposureOffset = FiniteRange(exposureOffset, -2f, 2f, 0f);
            gammaOffset = FiniteRange(gammaOffset, -.5f, .5f, 0f);
            refreshDenominator = Math.Max(1u, refreshDenominator);
            width = Mathf.Max(0, width); height = Mathf.Max(0, height);
        }

        private static float FiniteRange(float value, float min, float max, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);

        public PlayerGraphicsPreferences Copy() => JsonUtility.FromJson<PlayerGraphicsPreferences>(JsonUtility.ToJson(this));

        public static PlayerGraphicsPreferences Read(string json, PlayerGraphicsPreferences defaults)
        {
            var result = defaults.Copy();
            if (string.IsNullOrEmpty(json)) return result;
            try
            {
                // Overwrite preserves defaults for missing fields; unsupported versions use defaults.
                JsonUtility.FromJsonOverwrite(json, result);
                if (result.version != 1) return defaults.Copy();
                result.Sanitize();
                return result;
            }
            catch (ArgumentException) { return defaults.Copy(); }
        }

        public static int MatchResolution(Resolution[] modes, int width, int height, uint numerator, uint denominator)
        {
            for (int i = 0; i < modes.Length; i++)
                if (modes[i].width == width && modes[i].height == height &&
                    (ulong)modes[i].refreshRateRatio.numerator * denominator ==
                    (ulong)numerator * modes[i].refreshRateRatio.denominator) return i;
            return -1;
        }
    }
}
