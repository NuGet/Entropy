using NuGet.Services.Metadata.Catalog.Persistence;
using System.IO;

namespace NuGet.Test.Server
{
    public class MemoryStorageEntry
    {
        byte[] _data;
        string _contentType;
        string _cacheControl;

        public MemoryStorageEntry(StorageContent content, string contentType, string cacheControl)
        {
            var stream = new MemoryStream();
            content.GetContentStream().CopyToAsync(stream);
            _data = stream.ToArray();
            _contentType = contentType;
            _cacheControl = cacheControl;
        }

        public StorageContent CreateStorageContent()
        {
            return new StreamStorageContent(new MemoryStream(_data), _contentType, _cacheControl);
        }
    }
}
