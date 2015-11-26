<<<<<<< HEAD
﻿using Newtonsoft.Json.Linq;
using StagingWebApi.Resources;
using System.Net;
=======
﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using StagingWebApi.Resources;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace StagingWebApi.Controllers
{
    public class V3FlatController : ApiController
    {
        [Route("v3/flat/{owner}/{name}/{id}/index.json")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetIndex(string owner, string name, string id)
        {
            V3FlatResource resource = new V3FlatResource(owner, name);
            return await resource.Get(id);
        }

        [Route("v3/flat/{owner}/{name}/{id}/{version}/{file}.nupkg")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetNupkg(string owner, string name, string id, string version)
        {
            V3FlatResource resource = new V3FlatResource(owner, name);
            return await resource.GetNupkg(id, version);
        }

        [Route("v3/flat/{owner}/{name}/{id}/{version}/{file}.nuspec")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetArtifact(string owner, string name, string id, string version)
        {
            V3FlatResource resource = new V3FlatResource(owner, name);
            return await resource.GetNuspec(id, version);
        }
    }
}
