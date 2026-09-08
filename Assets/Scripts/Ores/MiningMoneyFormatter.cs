using System.Globalization;

namespace MiningSimulator.Ores
{
    /// <summary>Formats whole-number currency consistently without changing wallet storage.</summary>
    public static class MiningMoneyFormatter
    {
        private static readonly CultureInfo DisplayCulture = CultureInfo.InvariantCulture;

        public static string Format(int amount)
        {
            return amount.ToString("#,0", DisplayCulture);
        }
    }
}
