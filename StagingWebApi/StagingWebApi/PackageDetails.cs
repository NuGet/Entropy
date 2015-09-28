// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Generic;

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