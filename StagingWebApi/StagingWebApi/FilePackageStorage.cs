// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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