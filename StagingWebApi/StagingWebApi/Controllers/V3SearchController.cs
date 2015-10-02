using Newtonsoft.Json.Linq;
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
    public class V3SearchController : ApiController
    {
        [Route("v3/query/{owner}/{name}")]
        [HttpGet]
        public async Task<HttpResponseMessage> Query(string owner, string name)
        {
            V3Resource resource = new V3Resource(owner, name);

            Uri searchAddress = await resource.GetSearchQueryService();
            if (searchAddress == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            string query = Request.RequestUri.Query;

            Uri searchQuery = new Uri(searchAddress.AbsoluteUri + query);

            HttpClient client = new HttpClient();
            HttpResponseMessage searchResponse = await client.GetAsync(searchQuery);

            string json = await searchResponse.Content.ReadAsStringAsync();

            JObject obj = JObject.Parse(json);

            //  Dynamically merge the results as we pass them on through...
            //  (1) load local lucene
            //  (2) add local package metadata
            //  (3) query local lucene

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = Utils.CreateJsonContent(obj.ToString());

            return response;
        }
    }
}
