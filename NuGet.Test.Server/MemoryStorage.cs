using NuGet.Services.Metadata.Catalog.Persistence;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Test.Server
{
    public class MemoryStorage : Storage
    {
        MemoryStorageFactory _factory;

        public MemoryStorage(Uri baseAddress, MemoryStorageFactory factory) : base(baseAddress)
        {
            _factory = factory;
        }

        public override bool Exists(string fileName)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<Uri>> List(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task OnDelete(Uri resourceUri, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task<StorageContent> OnLoad(Uri resourceUri, CancellationToken cancellationToken)
        {
            return _factory.OnLoad(resourceUri, cancellationToken);
        }

        protected override Task OnSave(Uri resourceUri, StorageContent content, CancellationToken cancellationToken)
        {
            return _factory.OnSave(resourceUri, content, cancellationToken);
        }
    }
}
