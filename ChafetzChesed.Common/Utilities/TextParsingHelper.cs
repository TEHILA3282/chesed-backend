using System.Text.RegularExpressions;

namespace ChafetzChesed.Common.Utilities
{
    public static class TextParsingHelper
    {
        private static readonly Regex AmountRegex = new(
            @"-?\d{1,3}(?:[,\.\s]\d{3})*(?:\.\d+)?|-?\d+(?:\.\d+)?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static decimal ExtractAmount(string perut)
        {
            if (string.IsNullOrWhiteSpace(perut))
                return 0m;

            var normalized = perut
                .Replace("₪", string.Empty)
                .Replace("\u00A0", " ") 
                .Trim();

            var matches = AmountRegex.Matches(normalized);
            if (matches.Count == 0)
            {
                Console.WriteLine($"❗ לא זוהה סכום מתוך: '{perut}'");
                return 0m;
            }

            decimal maxAbs = 0m;

            foreach (Match match in matches)
            {
                var raw = match.Value;

                var cleaned = raw.Replace(",", string.Empty).Replace(" ", string.Empty);

                if (decimal.TryParse(
                        cleaned,
                        System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var val))
                {
                    if (Math.Abs(val) > Math.Abs(maxAbs))
                        maxAbs = val;
                }
            }

            if (maxAbs == 0m)
                Console.WriteLine($"❗ לא זוהה סכום מתוך: '{perut}'");

            return Math.Abs(maxAbs);
        }
    }
}
