using System.Globalization;

namespace MiningSimulator.Ores
{
    /// <summary>Formats currency with Vietnamese decimal and grouping separators.</summary>
    public static class MiningMoneyFormatter
    {
        private static readonly NumberFormatInfo DisplayFormat = new()
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = ".",
            NumberGroupSizes = new[] { 3 }
        };

        public static string Format(float amount)
        {
            return amount.ToString("#,0.##", DisplayFormat);
        }
    }
}
