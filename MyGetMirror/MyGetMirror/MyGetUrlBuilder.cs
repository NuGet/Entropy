using System;
using System.Linq;
using NuGet.Packaging.Core;

namespace MyGetMirror
{
    public class MyGetUrlBuilder
    {
        private readonly string _baseUrl;
        private readonly string _baseUrlWithoutPreAuth;

        public MyGetUrlBuilder(string source)
        {
            _baseUrl = GetBaseUrl(source);
            _baseUrlWithoutPreAuth = GetBaseUrlWithoutPreAuth(_baseUrl);
        }

        public string GetNuGetPushUrl()
        {
            return $"{_baseUrlWithoutPreAuth}/api/v2/package";
        }

        public string GetVsixUrl()
        {
            return $"{_baseUrl}/vsix";
        }

        public string GetVsixUrl(string id, string version)
        {
            return $"{_baseUrl}/vsix/{id}-{version}.vsix";
        }

        public string GetVsixPushUrl()
        {
            return $"{_baseUrlWithoutPreAuth}/vsix/upload";
        }

        public string GetSymbolsUrl(PackageIdentity identity)
        {
            // Example:
            // https://www.myget.org/F/nugetbuild/symbols/NuGet.ApplicationInsights.Owin/3.0.2633-r-master
            return $"{_baseUrl}/symbols/{identity.Id}/{identity.Version}";
        }

        private static string GetBaseUrlWithoutPreAuth(string baseUrl)
        {
            // Input examples:
            // - Public or auth: https://www.myget.org/F/nugetbuild
            // - Pre-auth:       https://www.myget.org/F/nuget-private-test/auth/API-KEY
            //
            // We need to strip off the last two pieces if the second to last is "auth", e.g. "auth/API-KEY".
            var reversedPieces = baseUrl.Split('/').Reverse().ToList();

            if (reversedPieces.Count < 2 || reversedPieces[1] != "auth")
            {
                return baseUrl;
            }

            var pieces = reversedPieces
                .Skip(2)
                .Reverse();

            // Input example:
            // - Public or auth: https://www.myget.org/F/nugetbuild
            // - Pre-auth:       https://www.myget.org/F/nuget-private-test
            return string.Join("/", pieces).TrimEnd('/');
        }

        private static string GetBaseUrl(string source)
        {
            // Verify the hostname is MyGet.
            Uri uri;
            if (!Uri.TryCreate(source, UriKind.Absolute, out uri) ||
                (!StringComparer.OrdinalIgnoreCase.Equals(uri.Host, "myget.org") &&
                 !uri.Host.EndsWith(".myget.org", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"To download MyGet symbols packages, the source '{source}' must be a HTTP MyGet source.");
            }

            string baseUrl;

            if (source.EndsWith("index.json", StringComparison.OrdinalIgnoreCase))
            {
                // V3 examples:
                // - Public or auth: https://www.myget.org/F/nugetbuild/api/v3/index.json
                // - Pre-auth:       https://www.myget.org/F/nuget-private-test/auth/API-KEY/api/v3/index.json
                //
                // We need to strip off the last three pieces, e.g. "api/v3/index.json".
                var pieces = source
                    .Split('/')
                    .Reverse()
                    .Skip(3)
                    .Reverse();

                baseUrl = string.Join("/", pieces).TrimEnd('/');
            }
            else
            {
                // V2 examples:
                // - Public or auth: https://www.myget.org/F/nugetbuild/api/v2
                // - Pre-auth:       https://www.myget.org/F/nuget-private-test/auth/API-KEY/api/v2
                //
                // We need to strip off the last two pieces, e.g. "api/v2".
                var pieces = source
                    .TrimEnd('/')
                    .Split('/')
                    .Reverse()
                    .Skip(2)
                    .Reverse();

                baseUrl = string.Join("/", pieces).TrimEnd('/');
            }

            // Output examples:
            // - Public or auth: https://www.myget.org/F/nugetbuild
            // - Pre-auth:       https://www.myget.org/F/nuget-private-test/auth/API-KEY

            return baseUrl;
        }
    }
}
