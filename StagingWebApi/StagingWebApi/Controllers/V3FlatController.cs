using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);

            JObject obj = new JObject();
            obj["message"] = "hello world";
            response.Content = Utils.CreateJsonContent(obj.ToString());

            return response;
        }
    }
}
