<<<<<<< HEAD
﻿using System;
=======
﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
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