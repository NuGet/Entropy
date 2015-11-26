<<<<<<< HEAD
﻿using System;
=======
﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace StagingWebApi
{
    public interface IStagePersistence
    {
<<<<<<< HEAD
        Task<HttpResponseMessage> GetOwner(string ownerName);
        Task<HttpResponseMessage> GetStage(string ownerName, string stageName);
=======
<<<<<<< HEAD
<<<<<<< HEAD
        Task<HttpResponseMessage> CreateOwner(string ownerName);
        Task<HttpResponseMessage> GetOwner(string ownerName);
        Task<HttpResponseMessage> GetStage(string ownerName, string stageName);
=======
=======
>>>>>>> 8b54aa2... push working form nuget.exe
        Task<HttpResponseMessage> GetOwner(string ownerName, string v3SourceBaseAddress);
        Task<HttpResponseMessage> GetStage(string ownerName, string stageName, string v3SourceBaseAddress);
>>>>>>> 697ed21... added angularjs test page
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
        Task<HttpResponseMessage> GetPackage(string ownerName, string stageName, string packageId);
        Task<HttpResponseMessage> GetPackageVersion(string ownerName, string stageName, string packageId, string packageVersion);

        Task<bool> ExistsStage(string ownerName, string stageName);

        Task<Tuple<HttpResponseMessage, List<Uri>>> DeleteStage(string ownerName, string stageName);
        Task<Tuple<HttpResponseMessage, List<Uri>>> DeletePackage(string ownerName, string stageName, string packageId);
        Task<Tuple<HttpResponseMessage, List<Uri>>> DeletePackageVersion(string ownerName, string stageName, string packageId, string packageVersion);

<<<<<<< HEAD
        Task<HttpResponseMessage> CreateStage(string ownerName, string stageName, string parentAddress);
        Task<HttpResponseMessage> CreatePackage(Uri baseAddress, string ownerName, string stageName, string packageId, string packageVersion, string packageOwner, Uri nupkgLocation, Uri nuspecLocation);
=======
        Task<HttpResponseMessage> CreateOwner(string ownerName);
        Task<HttpResponseMessage> CreateStage(string ownerName, string stageName, string parentAddress);
        Task<HttpResponseMessage> CreatePackage(Uri baseAddress, string ownerName, string stageName, string packageId, string packageVersion, string packageOwner, Uri nupkgLocation, Uri nuspecLocation);

        Task<string> CheckAccess(string stageName, string apiKey);

        Task<HttpResponseMessage> AddStageOwner(string ownerName, string stageName, string newOwnerName);
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
    }
}
