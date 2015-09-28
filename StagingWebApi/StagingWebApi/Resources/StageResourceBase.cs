// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace StagingWebApi.Resources
{
    public class StageResourceBase
    {
        public StageResourceBase(string ownerName, string stageName)
        {
            OwnerName = ownerName;
            StageName = stageName;
        }

        protected string OwnerName { get; private set; }
        protected string StageName { get; private set; }
    }
}