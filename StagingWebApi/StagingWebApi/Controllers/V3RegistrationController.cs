using StagingWebApi.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
