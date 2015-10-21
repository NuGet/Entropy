using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace StagingWebApi
{
    public interface IStagePersistence
    {
        Task<HttpResponseMessage> GetOwner(string ownerName);
        Task<HttpResponseMessage> GetStage(string ownerName, string stageName);
        Task<HttpResponseMessage> GetPackage(string ownerName, string stageName, string packageId);
        Task<HttpResponseMessage> GetPackageVersion(string ownerName, string stageName, string packageId, string packageVersion);

        Task<bool> ExistsStage(string ownerName, string stageName);

        Task<Tuple<HttpResponseMessage, List<Uri>>> DeleteStage(string ownerName, string stageName);
        Task<Tuple<HttpResponseMessage, List<Uri>>> DeletePackage(string ownerName, string stageName, string packageId);
        Task<Tuple<HttpResponseMessage, List<Uri>>> DeletePackageVersion(string ownerName, string stageName, string packageId, string packageVersion);

        Task<HttpResponseMessage> CreateStage(string ownerName, string stageName, string parentAddress);
        Task<HttpResponseMessage> CreatePackage(Uri baseAddress, string ownerName, string stageName, string packageId, string packageVersion, string packageOwner, Uri nupkgLocation, Uri nuspecLocation);
    }
}
