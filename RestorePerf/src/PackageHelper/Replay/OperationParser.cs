using System;
using System.Collections.Generic;
using NuGet.Packaging;
using NuGet.Versioning;
using PackageHelper.Replay.Operations;
using PackageHelper.Replay.Requests;

namespace PackageHelper.Replay
{
    public static class OperationParser
    {
        public static OperationInfo Parse(OperationParserContext ctx, StartRequest request)
        {
            if (request.Method != "GET")
            {
                return Unknown(request);
            }

            var uri = new Uri(request.Url, UriKind.Absolute);
            IReadOnlyList<KeyValuePair<string, Uri>> pairs;

            if (TryParsePackageBaseAddressIndex(uri, out var packageBaseAddressIndex)
                && ctx.PackageBaseAddressToPairs.TryGetValue(packageBaseAddressIndex.packageBaseAddress, out pairs))
            {
                return new OperationInfo(
                    new OperationWithId(
                        GetSourceIndex(ctx, packageBaseAddressIndex.packageBaseAddress),
                        OperationType.PackageBaseAddressIndex,
                        packageBaseAddressIndex.id),
                    request,
                    pairs);
            }

            if (TryParsePackageBaseAddressNupkg(uri, out var packageBaseAddressNupkg)
                && ctx.PackageBaseAddressToPairs.TryGetValue(packageBaseAddressNupkg.packageBaseAddress, out pairs))
            {
                return new OperationInfo(
                    new OperationWithIdVersion(
                        GetSourceIndex(ctx, packageBaseAddressNupkg.packageBaseAddress),
                        OperationType.PackageBaseAddressNupkg,
                        packageBaseAddressNupkg.id,
                        packageBaseAddressNupkg.version),
                    request,
                    pairs);
            }

            return Unknown(request);
        }

        private static int GetSourceIndex(OperationParserContext ctx, string packageBaseAddress)
        {
            var matchedSources = ctx.PackageBaseAddressToSources[packageBaseAddress];
            if (matchedSources.Count > 1)
            {
                Console.WriteLine("  WARNING: There are multiple resources with package base address:");
                Console.WriteLine("  " + packageBaseAddress);
                Console.WriteLine("  URL to operation mapping is therefore ambiguous.");
            }

            // Arbitrarily pick the first source.
            return ctx.SourceToIndex[matchedSources[0]];
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
