using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Protocol;

namespace PackageHelper.Replay
{
    public class OperationParserContext
    {
        public OperationParserContext(
            IReadOnlyDictionary<string, int> sourceToIndex,
            IReadOnlyDictionary<string, ServiceIndexResourceV3> sourceToServiceIndex,
            IReadOnlyDictionary<string, IReadOnlyList<KeyValuePair<string, Uri>>> packageBaseAddressToPairs,
            IReadOnlyDictionary<string, IReadOnlyList<string>> packageBaseAddressToSources)
        {
            SourceToIndex = sourceToIndex;
            SourceToServiceIndex = sourceToServiceIndex;
            PackageBaseAddressToPairs = packageBaseAddressToPairs;
            PackageBaseAddressToSources = packageBaseAddressToSources;
        }

        public IReadOnlyDictionary<string, int> SourceToIndex { get; }
        public IReadOnlyDictionary<string, ServiceIndexResourceV3> SourceToServiceIndex { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<KeyValuePair<string, Uri>>> PackageBaseAddressToPairs { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<string>> PackageBaseAddressToSources { get; }

        public static async Task<OperationParserContext> CreateAsync(IReadOnlyList<string> sources)
        {
            var sourceToIndex = sources.Select((x, i) => new { Index = i, Source = x }).ToDictionary(x => x.Source, x => x.Index);
            var sourceToServiceIndex = await PackageSourceUtility.GetSourceToServiceIndex(sources);

            // This look-up is used to quickly find if a package base address is in one of the source's service indexes.
            var packageBaseAddressToPairs = sourceToServiceIndex
                .SelectMany(x => x
                    .Value
                    .GetServiceEntryUris(ServiceTypes.PackageBaseAddress)
                    .Select(u => new { Source = x.Key, ResourceUri = u, PackageBaseAddress = u.AbsoluteUri.TrimEnd('/') + '/' }))
                .GroupBy(x => x.PackageBaseAddress)
                .ToDictionary(x => x.Key, x => (IReadOnlyList<KeyValuePair<string, Uri>>)x
                    .Select(y => new KeyValuePair<string, Uri>(y.Source, y.ResourceUri))
                    .ToList());

            // This look-up is used to find all of the sources with a certain package base address. This should
            // normally be a one-to-one mapping, but you can't be too careful...
            var packageBaseAddressToSources = packageBaseAddressToPairs
                .ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Value.Select(y => y.Key).Distinct().ToList());

            return new OperationParserContext(
                sourceToIndex,
                sourceToServiceIndex,
                packageBaseAddressToPairs,
                packageBaseAddressToSources);
        }
    }
}
