using System;
using System.Globalization;

namespace MiningSimulator.Ores
{
    /// <summary>Formats all mining currency with compact game-style suffixes.</summary>
    public static class MiningMoneyFormatter
    {
        // Covers the complete finite range of the game's current float-based wallet.
        private static readonly string[] Suffixes =
        {
            string.Empty, "K", "M", "B", "T", "Qa", "Qi",
            "Sx", "Sp", "Oc", "No", "Dc", "Ud"
        };

        private static readonly NumberFormatInfo DisplayFormat = new()
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = ".",
            NumberGroupSizes = new[] { 3 }
        };

        public static string Format(float amount)
        {
            if (float.IsNaN(amount))
            {
                return "0";
            }

            if (float.IsPositiveInfinity(amount))
            {
                amount = float.MaxValue;
            }
            else if (float.IsNegativeInfinity(amount))
            {
                amount = -float.MaxValue;
            }

            return FormatNumber(amount);
        }

        public static string Format(int amount)
        {
            return FormatNumber(amount);
        }

        public static string Format(long amount)
        {
            return FormatNumber(amount);
        }

        private static string FormatNumber(double amount)
        {
            double absolute = Math.Abs(amount);
            if (absolute < 1000d)
            {
                return amount.ToString("#,0.##", DisplayFormat);
            }

            int tier = Math.Min(
                (int)Math.Floor(Math.Log10(absolute) / 3d),
                Suffixes.Length - 1);
            double scaled = absolute / Math.Pow(1000d, tier);
            scaled = Math.Round(scaled, 2, MidpointRounding.AwayFromZero);

            // Avoid values such as 1000K after rounding at a suffix boundary.
            if (scaled >= 1000d && tier < Suffixes.Length - 1)
            {
                scaled /= 1000d;
                tier++;
            }

            string compact = scaled.ToString("0.##", CultureInfo.InvariantCulture);
            int decimalIndex = compact.IndexOf('.');
            string result = decimalIndex >= 0
                ? compact.Substring(0, decimalIndex) + Suffixes[tier] +
                  compact.Substring(decimalIndex + 1)
                : compact + Suffixes[tier];

            return amount < 0d ? "-" + result : result;
        }
    }
}
