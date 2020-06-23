using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace PackageHelper.Replay
{
    static class PackageSourceUtility
    {
        public static async Task<Dictionary<string, ServiceIndexResourceV3>> GetSourceToServiceIndex(IReadOnlyList<string> sources)
        {
            var sourceToRepository = sources.ToDictionary(x => x, x => Repository.Factory.GetCoreV3(x));

            var sourceToFeedType = await GetSourceToFeedTypeAsync(sourceToRepository);
            if (sourceToFeedType.Values.Any(x => x != FeedType.HttpV3))
            {
                throw new ArgumentException("Only V3 HTTP sources are supported.");
            }

            var sourceToServiceIndex = await GetSourceToServiceIndexAsync(sourceToRepository);
            return sourceToServiceIndex;
        }

        private static async Task<Dictionary<string, FeedType>> GetSourceToFeedTypeAsync(Dictionary<string, SourceRepository> sourceToRepository)
        {
            var sourceToFeedTypeTask = sourceToRepository.ToDictionary(x => x.Key, x => x.Value.GetFeedType(CancellationToken.None));
            await Task.WhenAll(sourceToFeedTypeTask.Values);
            return sourceToFeedTypeTask.ToDictionary(x => x.Key, x => x.Value.Result);
        }

        private static async Task<Dictionary<string, ServiceIndexResourceV3>> GetSourceToServiceIndexAsync(Dictionary<string, SourceRepository> sourceToRepository)
        {
            var sourceToServiceIndexTask = sourceToRepository.ToDictionary(x => x.Key, x => x.Value.GetResourceAsync<ServiceIndexResourceV3>());
            await Task.WhenAll(sourceToServiceIndexTask.Values);
            return sourceToServiceIndexTask.ToDictionary(x => x.Key, x => x.Value.Result);
        }
    }
}
