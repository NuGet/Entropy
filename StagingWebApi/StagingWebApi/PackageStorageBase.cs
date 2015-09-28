using System;
using System.IO;
using System.Threading.Tasks;

namespace StagingWebApi
{
    public abstract class PackageStorageBase
    {
        string _root;
        public abstract Task<Uri> Save(Stream stream, string blobName, string contentDisposition, string contentType);
        public abstract Task Delete(Uri location);
        public string Root { get { return _root; } }
        protected PackageStorageBase()
        {
            _root = Guid.NewGuid().ToString() + "/";
        }
    }
}