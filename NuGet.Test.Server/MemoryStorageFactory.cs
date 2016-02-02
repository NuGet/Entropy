using NuGet.Services.Metadata.Catalog.Persistence;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Test.Server
{
    public class MemoryStorageFactory : StorageFactory
    {
        IDictionary<Uri, MemoryStorageEntry> _store;

        public MemoryStorageFactory(Uri baseAddress, IDictionary<Uri, MemoryStorageEntry> store)
        {
            BaseAddress = new Uri(baseAddress.ToString().TrimEnd('/') + '/');
            _store = store;
        }

        public override Storage Create(string name = null)
        {
            return new MemoryStorage(new Uri(BaseAddress, name ?? string.Empty), this);
        }

        public Task<StorageContent> OnLoad(Uri resourceUri, CancellationToken cancellationToken)
        {
            MemoryStorageEntry entry;
            if (_store.TryGetValue(resourceUri, out entry))
            {
                return Task.FromResult<StorageContent>(entry.CreateStorageContent());
            }
            return Task.FromResult<StorageContent>(null);
        }

        public Task OnSave(Uri resourceUri, StorageContent content, CancellationToken cancellationToken)
        {
            _store[resourceUri] = new MemoryStorageEntry(content, content.ContentType, content.CacheControl);
            var tcs = new TaskCompletionSource<int>();
            tcs.SetResult(0);
            return tcs.Task;
        }
    }
}