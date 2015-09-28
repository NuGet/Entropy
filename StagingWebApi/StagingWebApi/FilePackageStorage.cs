using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace StagingWebApi
{
    public class FilePackageStorage : PackageStorageBase
    {
        public override Task Delete(Uri location)
        {
            throw new NotImplementedException();
        }

        public override Task<Uri> Save(Stream stream, string blobName, string contentDisposition, string contentType)
        {
            throw new NotImplementedException();
        }
    }
}