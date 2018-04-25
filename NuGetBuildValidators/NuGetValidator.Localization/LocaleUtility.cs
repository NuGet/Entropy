using System.Collections.Generic;
using System.Linq;

namespace NuGetValidator.Localization
{
    internal static class LocaleUtility
    {
        private static readonly IDictionary<string, int> _localeStringToIndex = new Dictionary<string, int>()
        {
            { "cs", 0 },
            { "de", 1 },
            { "es", 2 },
            { "fr", 3 },
            { "it", 4 },
            { "ja", 5 },
            { "ko", 6 },
            { "pl", 7 },
            { "pt-br", 8 },
            { "ru", 9 },
            { "tr", 10 },
            { "zh-hans", 11 },
            { "zh-hant", 12 }
        };

        public static string[] LocaleStrings => _localeStringToIndex.Keys.ToArray();
    }
}
