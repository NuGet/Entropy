using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Versioning;
using PackageHelper.Replay.Operations;
using PackageHelper.Replay.Requests;

namespace PackageHelper.Replay
{
    public static class OperationParser
    {
        public static async Task<List<OperationInfo>> ParseAsync(IReadOnlyList<string> sources, IEnumerable<StartRequest> requests)
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
                .ToDictionary(x => x.Key, x => x
                    .Select(y => new KeyValuePair<string, Uri>(y.Source, y.ResourceUri))
                    .ToList());

            // This look-up is used to find all of the sources with a certain package base address. This should
            // normally be a one-to-one mapping, but you can't be too careful...
            var packageBaseAddressToSources = packageBaseAddressToPairs
                .ToDictionary(x => x.Key, x => x.Value.Select(y => y.Key).Distinct().ToList());

            var output = new List<OperationInfo>();

            foreach (var request in requests)
            {
                if (request.Method != "GET")
                {
                    output.Add(Unknown(request));
                    continue;
                }

                var uri = new Uri(request.Url, UriKind.Absolute);
                List<KeyValuePair<string, Uri>> pairs;

                if (TryParsePackageBaseAddressIndex(uri, out var packageBaseAddressIndex)
                    && packageBaseAddressToPairs.TryGetValue(packageBaseAddressIndex.packageBaseAddress, out pairs))
                {
                    output.Add(new OperationInfo(
                        new OperationWithId(
                            GetSourceIndex(
                                sourceToIndex,
                                packageBaseAddressToSources,
                                packageBaseAddressIndex.packageBaseAddress),
                            OperationType.PackageBaseAddressIndex,
                            packageBaseAddressIndex.id),
                        request,
                        pairs));
                    continue;
                }

                if (TryParsePackageBaseAddressNupkg(uri, out var packageBaseAddressNupkg)
                    && packageBaseAddressToPairs.TryGetValue(packageBaseAddressNupkg.packageBaseAddress, out pairs))
                {
                    output.Add(new OperationInfo(
                        new OperationWithIdVersion(
                            GetSourceIndex(
                                sourceToIndex,
                                packageBaseAddressToSources,
                                packageBaseAddressNupkg.packageBaseAddress),
                            OperationType.PackageBaseAddressNupkg,
                            packageBaseAddressNupkg.id,
                            packageBaseAddressNupkg.version),
                        request,
                        pairs));
                    continue;
                }

                output.Add(Unknown(request));
            }

            return output;
        }

        private static int GetSourceIndex(
            Dictionary<string, int> sourceToIndex,
            Dictionary<string, List<string>> packageBaseAddressToSources,
            string packageBaseAddress)
        {
            var matchedSources = packageBaseAddressToSources[packageBaseAddress];
            if (matchedSources.Count > 1)
            {
                Console.WriteLine("  WARNING: There are multiple resources with package base address:");
                Console.WriteLine("  " + packageBaseAddress);
                Console.WriteLine("  URL to operation mapping is therefore ambiguous.");
            }

            // Arbitrarily pick the first source.
            return sourceToIndex[matchedSources[0]];
        }

        private static OperationInfo Unknown(StartRequest request)
        {
            return new OperationInfo(null, request, Array.Empty<KeyValuePair<string, Uri>>());
        }

        private static bool TryParsePackageBaseAddressIndex(Uri uri, out (string packageBaseAddress, string id) parsed)
        {
            parsed = default;

            const string indexPath = "/index.json";
            if (!uri.AbsolutePath.EndsWith(indexPath, StringComparison.Ordinal) // The path ends in index.json
                || HasQueryOrFragment(uri))                                     // There is never a query or fragment
            {
                return false;
            }

            var pieces = uri.LocalPath.Split('/');
            var id = pieces[pieces.Length - 2];
            if (!PackageIdValidator.IsValidPackageId(id)                        // Must have a valid package ID
                || !IsLowercase(id))                                            // ID must be lowercase
            {
                return false;
            }

            var packageBaseAddress = uri.AbsoluteUri.Substring(
                0,
                uri.AbsoluteUri.Length - (id.Length + indexPath.Length));
            parsed = (packageBaseAddress, id);
            return true;
        }


        private static bool TryParsePackageBaseAddressNupkg(Uri uri, out (string packageBaseAddress, string id, string version) parsed)
        {
            parsed = default;

            const string nupkgExtension = ".nupkg";
            if (!uri.AbsolutePath.EndsWith(nupkgExtension, StringComparison.Ordinal) // The path ends in .nupkg
                || HasQueryOrFragment(uri))                                          // There is never a query or fragment
            {
                return false;
            }

            var pieces = uri.LocalPath.Split('/');
            if (pieces.Length < 3)                                                   // Path must have at least 3 slash separated pieces
            {
                return false;
            }

            var id = pieces[pieces.Length - 3];
            var version = pieces[pieces.Length - 2];
            var fileName = pieces[pieces.Length - 1];
            var expectedFileName = $"{id}.{version}{nupkgExtension}";

            if (fileName != expectedFileName                                         // File must be {id}.{version}.nupkg
                || !PackageIdValidator.IsValidPackageId(id)                          // Must have a valid package ID
                || !IsLowercase(id)                                                  // ID must be lowercase
                || !NuGetVersion.TryParse(version, out var parsedVersion)            // Version must be a valid NuGetVersion
                || parsedVersion.ToNormalizedString() != version                     // Version must be normalized
                || !IsLowercase(version))                                            // Version must be lowercase
            {
                return false;
            }

            var packageBaseAddress = uri.AbsoluteUri.Substring(
                0,
                uri.AbsoluteUri.Length - (id.Length + 1 + version.Length + 1 + fileName.Length));
            parsed = (packageBaseAddress, id, version);
            return true;
        }

        private static bool HasQueryOrFragment(Uri uri)
        {
            return !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment);
        }

        private static bool IsLowercase(string input)
        {
            return input == input.ToLowerInvariant();
        }
    }
}
