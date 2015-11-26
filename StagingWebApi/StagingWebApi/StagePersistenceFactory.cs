<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
=======
﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d

namespace StagingWebApi
{
    public class StagePersistenceFactory
    {
        public IStagePersistence Create()
        {
            return new SqlStagePersistence();
        }
    }
}