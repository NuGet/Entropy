using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PackageHelper.Replay;
using Xunit;

namespace PackageHelper.Parse
{
    public class NuGetUrlParserTest
    {
        private const string NuGetSourceUrl = "https://api.nuget.org/v3/index.json";
        private const string NuGetPackageBaseAddress = "https://api.nuget.org/v3-flatcontainer/";

        [Theory]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/index.json", ParsedUrlType.PackageBaseAddressIndex)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-beta/newtonsoft.json.9.0.1-beta.nupkg", ParsedUrlType.PackageBaseAddressNupkg)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/other.json", ParsedUrlType.Unknown)]
        [InlineData("POST", NuGetPackageBaseAddress + "newtonsoft.json/index.json", ParsedUrlType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "invalid..id/index.json", ParsedUrlType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "invalid..id/9.0.1.index.json", ParsedUrlType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "Newtonsoft.Json/index.json", ParsedUrlType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "Newtonsoft.Json/9.0.1-beta/newtonsoft.json.9.0.1-beta.json", ParsedUrlType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-BETA/newtonsoft.json.9.0.1-BETA.json", ParsedUrlType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-beta/newtonsoft.json.0.0.0.json", ParsedUrlType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1.0-beta/newtonsoft.json.9.0.1.0-beta.json", ParsedUrlType.Unknown)]
        public async Task HasExpectedType(string method, string url, ParsedUrlType type)
        {
            var request = new StartRequest(method, url);
            var sources = new List<string> { NuGetSourceUrl };

            var parsedUrls = await NuGetUrlParser.ParseUrlsAsync(sources, new[] { request });

            var parsedUrl = Assert.Single(parsedUrls);
            Assert.Equal(type, parsedUrl.ParsedUrl.Type);
        }

        [Fact]
        public async Task CanParsePackageBaseAddressIndex()
        {
            var request = new StartRequest("GET", NuGetPackageBaseAddress + "newtonsoft.json/index.json");
            var sources = new List<string> { NuGetSourceUrl };

            var parsedUrls = await NuGetUrlParser.ParseUrlsAsync(sources, new[] { request });

            var parsedUrl = Assert.Single(parsedUrls);

            var pair = Assert.Single(parsedUrl.SourceResourceUris);
            Assert.Equal(NuGetSourceUrl, pair.Key);
            Assert.Equal(new Uri(NuGetPackageBaseAddress, UriKind.Absolute), pair.Value);

            Assert.Equal(ParsedUrlType.PackageBaseAddressIndex, parsedUrl.ParsedUrl.Type);
            Assert.Same(request, parsedUrl.ParsedUrl.Request);
            var parsedUrlWithId = Assert.IsType<ParsedUrlWithId>(parsedUrl.ParsedUrl);
            Assert.Equal("newtonsoft.json", parsedUrlWithId.Id);
        }

        [Fact]
        public async Task CanParsePackageBaseAddressNupkg()
        {
            var request = new StartRequest("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-beta/newtonsoft.json.9.0.1-beta.nupkg");
            var sources = new List<string> { NuGetSourceUrl };

            var parsedUrls = await NuGetUrlParser.ParseUrlsAsync(sources, new[] { request });

            var parsedUrl = Assert.Single(parsedUrls);

            var pair = Assert.Single(parsedUrl.SourceResourceUris);
            Assert.Equal(NuGetSourceUrl, pair.Key);
            Assert.Equal(new Uri(NuGetPackageBaseAddress, UriKind.Absolute), pair.Value);

            Assert.Equal(ParsedUrlType.PackageBaseAddressNupkg, parsedUrl.ParsedUrl.Type);
            Assert.Same(request, parsedUrl.ParsedUrl.Request);
            var parsedUrlWithIdVersion = Assert.IsType<ParsedUrlWithIdVersion>(parsedUrl.ParsedUrl);
            Assert.Equal("newtonsoft.json", parsedUrlWithIdVersion.Id);
            Assert.Equal("9.0.1-beta", parsedUrlWithIdVersion.Version);
        }
    }
}
