<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
=======
﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Generic;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d

namespace StagingWebApi
{
    public class PackageDetails
    {
        public string Id { get; private set; }
        public List<string> Versions { get; private set; }

        public PackageDetails(string id)
        {
            Id = id;
            Versions = new List<string>();
        }
    }
}