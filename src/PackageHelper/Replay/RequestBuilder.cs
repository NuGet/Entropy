using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Protocol;
using PackageHelper.Replay.Operations;
using PackageHelper.Replay.Requests;

namespace PackageHelper.Replay
{
    public static class RequestBuilder
    {
        public static async Task<Dictionary<Operation, StartRequest>> BuildAsync(IReadOnlyList<string> sources, IEnumerable<Operation> operations)
        {
            var sourceToServiceIndex = await PackageSourceUtility.GetSourceToServiceIndex(sources);
            var packageBaseAddresses = sourceToServiceIndex
                .Select(x => x.Value.GetServiceEntryUri(ServiceTypes.PackageBaseAddress).AbsoluteUri.TrimEnd('/') + '/')
                .ToList();

            var output = new Dictionary<Operation, StartRequest>();
            
            foreach (var operation in operations)
            {
                if (output.ContainsKey(operation))
                {
                    continue;
                }

                var packageBaseAddress = packageBaseAddresses[operation.SourceIndex];

                StartRequest request;
                string id;
                string version;
                switch (operation.Type)
                {
                    case OperationType.PackageBaseAddressIndex:
                        var packageBaseAddressIndex = (OperationWithId)operation;
                        id = packageBaseAddressIndex.Id.ToLowerInvariant();
                        request = new StartRequest("GET", $"{packageBaseAddress}{id}/index.json");
                        break;
                    case OperationType.PackageBaseAddressNupkg:
                        var packageBaseAddressNupkg = (OperationWithIdVersion)operation;
                        id = packageBaseAddressNupkg.Id.ToLowerInvariant();
                        version = packageBaseAddressNupkg.Version.ToLowerInvariant();
                        request = new StartRequest("GET", $"{packageBaseAddress}{id}/{version}/{id}.{version}.nupkg");
                        break;
                    default:
                        throw new NotSupportedException($"The operation type {operation.Type} is not supported.");
                }

                output.Add(operation, request);
            }

            return output;
        }
    }
}
