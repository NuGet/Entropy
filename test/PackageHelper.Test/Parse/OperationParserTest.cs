using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PackageHelper.Replay;
using PackageHelper.Replay.Operations;
using PackageHelper.Replay.Requests;
using Xunit;

namespace PackageHelper.Parse
{
    public class OperationParserTest
    {
        private const string NuGetSourceUrl = "https://api.nuget.org/v3/index.json";
        private const string NuGetPackageBaseAddress = "https://api.nuget.org/v3-flatcontainer/";

        [Theory]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/index.json", OperationType.PackageBaseAddressIndex)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-beta/newtonsoft.json.9.0.1-beta.nupkg", OperationType.PackageBaseAddressNupkg)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/other.json", null)]
        [InlineData("POST", NuGetPackageBaseAddress + "newtonsoft.json/index.json", null)]
        [InlineData("GET", NuGetPackageBaseAddress + "invalid..id/index.json", null)]
        [InlineData("GET", NuGetPackageBaseAddress + "invalid..id/9.0.1.index.json", null)]
        [InlineData("GET", NuGetPackageBaseAddress + "Newtonsoft.Json/index.json", null)]
        [InlineData("GET", NuGetPackageBaseAddress + "Newtonsoft.Json/9.0.1-beta/newtonsoft.json.9.0.1-beta.json", null)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-BETA/newtonsoft.json.9.0.1-BETA.json", null)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-beta/newtonsoft.json.0.0.0.json", null)]
        [InlineData("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1.0-beta/newtonsoft.json.9.0.1.0-beta.json", null)]
        public async Task HasExpectedType(string method, string url, OperationType? type)
        {
            var request = new StartRequest(method, url);
            var sources = new List<string> { NuGetSourceUrl };

            var operationInfos = await OperationParser.ParseAsync(sources, new[] { request });

            var operationInfo = Assert.Single(operationInfos);
            Assert.Equal(type, operationInfo.Operation?.Type);
        }

        [Fact]
        public async Task CanParsePackageBaseAddressIndex()
        {
            var request = new StartRequest("GET", NuGetPackageBaseAddress + "newtonsoft.json/index.json");
            var sources = new List<string> { NuGetSourceUrl };

            var operationInfos = await OperationParser.ParseAsync(sources, new[] { request });

            var operationInfo = Assert.Single(operationInfos);

            var pair = Assert.Single(operationInfo.SourceResourceUris);
            Assert.Equal(NuGetSourceUrl, pair.Key);
            Assert.Equal(new Uri(NuGetPackageBaseAddress, UriKind.Absolute), pair.Value);

            Assert.Equal(OperationType.PackageBaseAddressIndex, operationInfo.Operation.Type);
            Assert.Same(request, operationInfo.Request);
            var operationWithId = Assert.IsType<OperationWithId>(operationInfo.Operation);
            Assert.Equal("newtonsoft.json", operationWithId.Id);
        }

        [Fact]
        public async Task CanParsePackageBaseAddressNupkg()
        {
            var request = new StartRequest("GET", NuGetPackageBaseAddress + "newtonsoft.json/9.0.1-beta/newtonsoft.json.9.0.1-beta.nupkg");
            var sources = new List<string> { NuGetSourceUrl };

            var operationInfos = await OperationParser.ParseAsync(sources, new[] { request });

            var operationInfo = Assert.Single(operationInfos);

            var pair = Assert.Single(operationInfo.SourceResourceUris);
            Assert.Equal(NuGetSourceUrl, pair.Key);
            Assert.Equal(new Uri(NuGetPackageBaseAddress, UriKind.Absolute), pair.Value);

            Assert.Equal(OperationType.PackageBaseAddressNupkg, operationInfo.Operation.Type);
            Assert.Same(request, operationInfo.Request);
            var operationWithIdVersion = Assert.IsType<OperationWithIdVersion>(operationInfo.Operation);
            Assert.Equal("newtonsoft.json", operationWithIdVersion.Id);
            Assert.Equal("9.0.1-beta", operationWithIdVersion.Version);
        }
    }
}
