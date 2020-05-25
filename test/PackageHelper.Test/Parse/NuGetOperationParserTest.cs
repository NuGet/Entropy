using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PackageHelper.Replay.Requests;
using Xunit;

namespace PackageHelper.Parse
{
    public class NuGetOperationParserTest
    {
        private const string NuGetSourceUrl = "https://api.nuget.org/v3/index.json";
        private const string NuGetPackageBaseAddress = "https://api.nuget.org/v3-flatcontainer/";

        [Theory]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/index.json", NuGetOperationType.PackageBaseAddressIndex)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-beta/newtonsoft.json.9.0.1-beta.nupkg", NuGetOperationType.PackageBaseAddressNupkg)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/other.json", NuGetOperationType.Unknown)]
        [InlineData("POST", NuGetPackageBaseAddress + "newtonsoft.json/index.json", NuGetOperationType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "invalid..id/index.json", NuGetOperationType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "invalid..id/9.0.1.index.json", NuGetOperationType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "Newtonsoft.Json/index.json", NuGetOperationType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "Newtonsoft.Json/9.0.1-beta/newtonsoft.json.9.0.1-beta.json", NuGetOperationType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-BETA/newtonsoft.json.9.0.1-BETA.json", NuGetOperationType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-beta/newtonsoft.json.0.0.0.json", NuGetOperationType.Unknown)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1.0-beta/newtonsoft.json.9.0.1.0-beta.json", NuGetOperationType.Unknown)]
        public async Task HasExpectedType(string method, string url, NuGetOperationType type)
        {
            var request = new StartRequest(method, url);
            var sources = new List<string> { NuGetSourceUrl };

            var operationInfos = await NuGetOperationParser.ParseAsync(sources, new[] { request });

            var operationInfo = Assert.Single(operationInfos);
            Assert.Equal(type, operationInfo.Operation.Type);
        }

        [Fact]
        public async Task CanParsePackageBaseAddressIndex()
        {
            var request = new StartRequest("GET", NuGetPackageBaseAddress + "newtonsoft.json/index.json");
            var sources = new List<string> { NuGetSourceUrl };

            var operationInfos = await NuGetOperationParser.ParseAsync(sources, new[] { request });

            var operationInfo = Assert.Single(operationInfos);

            var pair = Assert.Single(operationInfo.SourceResourceUris);
            Assert.Equal(NuGetSourceUrl, pair.Key);
            Assert.Equal(new Uri(NuGetPackageBaseAddress, UriKind.Absolute), pair.Value);

            Assert.Equal(NuGetOperationType.PackageBaseAddressIndex, operationInfo.Operation.Type);
            Assert.Same(request, operationInfo.Request);
            var operationWithId = Assert.IsType<NuGetOperationWithId>(operationInfo.Operation);
            Assert.Equal("newtonsoft.json", operationWithId.Id);
        }

        [Fact]
        public async Task CanParsePackageBaseAddressNupkg()
        {
            var request = new StartRequest("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-beta/newtonsoft.json.9.0.1-beta.nupkg");
            var sources = new List<string> { NuGetSourceUrl };

            var operationInfos = await NuGetOperationParser.ParseAsync(sources, new[] { request });

            var operationInfo = Assert.Single(operationInfos);

            var pair = Assert.Single(operationInfo.SourceResourceUris);
            Assert.Equal(NuGetSourceUrl, pair.Key);
            Assert.Equal(new Uri(NuGetPackageBaseAddress, UriKind.Absolute), pair.Value);

            Assert.Equal(NuGetOperationType.PackageBaseAddressNupkg, operationInfo.Operation.Type);
            Assert.Same(request, operationInfo.Request);
            var operationWithIdVersion = Assert.IsType<NuGetOperationWithIdVersion>(operationInfo.Operation);
            Assert.Equal("newtonsoft.json", operationWithIdVersion.Id);
            Assert.Equal("9.0.1-beta", operationWithIdVersion.Version);
        }
    }
}
