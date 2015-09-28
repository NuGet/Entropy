// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using StagingWebApi.Resources;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace StagingWebApi.Controllers
{
    public class V3RegistrationController : ApiController
    {
        [Route("v3/registration/{owner}/{name}/{id}/index.json")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetIndex(string owner, string name, string id)
        {
            V3RegistrationResource resource = new V3RegistrationResource(Request.RequestUri, owner, name);
            return await resource.GetIndex(id);
        }

        [Route("v3/registration/{owner}/{name}/{id}/page/{lower}/{upper}.json")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetPage(string owner, string name, string id, string lower, string upper)
        {
            V3RegistrationResource resource = new V3RegistrationResource(Request.RequestUri, owner, name);
            return await resource.GetPage(id, lower, upper);
        }
    }
}
