using CompressRegistration;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

[assembly: OwinStartup("MinimizerWebApplication", typeof(MinimizerWebApplication.Startup))]

namespace MinimizerWebApplication
{
    public class Startup
    {
        const string Query = "/query/340";
        const string RegistrationBaseUrl = "/registration/340/";

        Uri _remote;

        public void Configuration(IAppBuilder app)
        {
            _remote = new Uri("https://api.nuget.org/v3/index.json");

            app.Run(Invoke);
        }

        async Task Invoke(IOwinContext context)
        {
            //  First check that if this is the 3.4.0 client we are definitely getting an Accept-Encoding header

            string userAgent = context.Request.Headers["User-Agent"];
            string acceptEncoding = context.Request.Headers["Accept-Encoding"];
            if (userAgent.StartsWith("NuGet Client V3/3.4.0.0") && !acceptEncoding.Contains("gzip"))
            {
                await context.Response.WriteAsync("NuGet 3.4 client must be able to accept gzip");
                context.Response.StatusCode = (int)HttpStatusCode.NotAcceptable;
                return;
            }

            //  Now process teh actual request

            if (context.Request.Uri.PathAndQuery == "/")
            {
                await context.Response.WriteAsync("READY");
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }

            else if (context.Request.Uri.PathAndQuery == "/v3/index.json")
            {
                await InterceptIndex(context);
            }

            else if (context.Request.Path.Value == Query)
            {
                await InterceptQuery(context);
            }

            else if (context.Request.Path.Value.StartsWith(RegistrationBaseUrl))
            {
                await InterceptRegistration(context);
            }

            else if (context.Request.Path.Value.StartsWith("/flat/"))
            {
                await InterceptFlat(context);
            }

            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        async Task InterceptIndex(IOwinContext context)
        {
            var resources = await ReadRemoteIndex();

            string local = context.Request.Uri.GetLeftPart(UriPartial.Authority);

            // search

            resources["SearchQueryService"] = new List<string> { local + "/query" };
            resources["SearchQueryService/3.0.0-beta"] = new List<string> { local + "/query" };
            resources["SearchQueryService/3.0.0-rc"] = new List<string> { local + "/query" };
            //resources["SearchQueryService"] = new List<string> { };
            //resources["SearchQueryService/3.0.0-beta"] = new List<string> { };
            //resources["SearchQueryService/3.0.0-rc"] = new List<string> { };
            resources["SearchQueryService/3.4.0"] = new List<string> { local + Query };

            // registration 

            resources["RegistrationsBaseUrl"] = new List<string> { local + "/registration/" };
            resources["RegistrationsBaseUrl/3.0.0-beta"] = new List<string> { local + "/registration/" };
            resources["RegistrationsBaseUrl/3.0.0-rc"] = new List<string> { local + "/registration/" };
            //resources["RegistrationsBaseUrl"] = new List<string> { };
            //resources["RegistrationsBaseUrl/3.0.0-beta"] = new List<string> { };
            //resources["RegistrationsBaseUrl/3.0.0-rc"] = new List<string> { };
            resources["RegistrationsBaseUrl/3.4.0"] = new List<string> { local + RegistrationBaseUrl };

            // flat-container

            resources["PackageBaseAddress/3.0.0"] = new List<string> { local + "/flat/" };

            await WriteIndex(context, resources);
        }
        async Task InterceptQuery(IOwinContext context)
        {
            var resourceId = await GetRemoteAddress("SearchQueryService");
            if (resourceId == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            string local = context.Request.Uri.GetLeftPart(UriPartial.Authority);

            var remoteUri = string.Format("{0}?{1}", resourceId, context.Request.QueryString.Value ?? string.Empty);

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(remoteUri);

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (var textReader = new StreamReader(stream))
            {
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    JToken queryResult = JToken.Load(jsonReader);

                    foreach (var resultObj in queryResult["data"])
                    {
                        string normalizedId = resultObj["id"].ToString().ToLowerInvariant();
                        string normalizedVersion = NuGetVersion.Parse(resultObj["version"].ToString()).ToNormalizedString().ToLowerInvariant();

                        resultObj["@id"] = local + RegistrationBaseUrl + normalizedId + "/" + normalizedVersion + ".json";
                        resultObj["registration"] = local + RegistrationBaseUrl + normalizedId + "/index.json";

                        foreach (var versionResultObj in resultObj["versions"])
                        {
                            string otherNormalizedVersion = NuGetVersion.Parse(resultObj["version"].ToString()).ToNormalizedString().ToLowerInvariant();

                            versionResultObj["@id"] = local + RegistrationBaseUrl + normalizedId + "/" + otherNormalizedVersion + ".json";
                        }
                    }

                    context.Response.ContentType = response.Content.Headers.ContentType.MediaType;
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync(queryResult.ToString(Formatting.None));
                }
            }
        }
        async Task InterceptRegistration(IOwinContext context)
        {
            var resourceId = await GetRemoteAddress("RegistrationsBaseUrl");
            if (resourceId == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            string path = context.Request.Path.Value.Substring(RegistrationBaseUrl.Length);

            string remoteResourceId = resourceId + path;

            if (remoteResourceId.EndsWith("/index.json"))
            {
                await InterceptRegistrationIndex(remoteResourceId, context);
            }
            else
            {
                await InterceptRegistrationPackage(remoteResourceId, context);
            }
        }

        async Task InterceptRegistrationIndex(string remoteResourceId, IOwinContext context)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(remoteResourceId);

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (var textReader = new StreamReader(stream))
            {
                using (var jsonReader = new JsonSkipReader(new JsonTextReader(textReader), RegistrationIndexJsonPathToSkip()))
                {
                    var obj = JObject.Load(jsonReader);

                    await InlineRegistration(obj);

                    context.Response.ContentType = response.Content.Headers.ContentType.MediaType;
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync(obj.ToString(Formatting.None));
                }
            }
        }
        async Task InterceptRegistrationPackage(string remoteResourceId, IOwinContext context)
        {
            // this is not currently used by the client - just make this a pass through for now

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(remoteResourceId);

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (var textReader = new StreamReader(stream))
            {
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    var obj = JObject.Load(jsonReader);

                    context.Response.ContentType = response.Content.Headers.ContentType.MediaType;
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync(obj.ToString(Formatting.None));
                }
            }
        }

        async Task InterceptFlat(IOwinContext context)
        {
            var resourceId = await GetRemoteAddress("PackageBaseAddress/3.0.0");
            if (resourceId == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        async Task InlineRegistration(JObject registrationIndex)
        {
            var missingPageUris = new List<string>();

            foreach (JObject pageItem in registrationIndex["items"])
            {
                JToken items;
                if (!pageItem.TryGetValue("items", out items))
                {
                    missingPageUris.Add(pageItem["@id"].ToString());
                }
            }

            var missingPages = new Dictionary<string, Task<JObject>>();

            foreach (var pageUri in missingPageUris)
            {
                missingPages.Add(pageUri, FetchMissingPage(pageUri));
            }

            await Task.WhenAll(missingPages.Values);

            foreach (JObject pageItem in registrationIndex["items"])
            {
                Task<JObject> pageObj;
                if (missingPages.TryGetValue(pageItem["@id"].ToString(), out pageObj))
                {
                    pageItem["items"] = pageObj.Result["items"];
                }
            }

            RemoveEmptyFields(registrationIndex);
        }

        void RemoveEmptyFields(JObject registrationIndex)
        {
            foreach (JObject pageItem in registrationIndex["items"])
            {
                foreach (JObject packageItem in pageItem["items"])
                {
                    JObject catalogEntry = (JObject)packageItem["catalogEntry"];

                    RemoveEmptyField(catalogEntry, "iconUrl");
                    RemoveEmptyField(catalogEntry, "projectUrl");
                    RemoveEmptyField(catalogEntry, "licenseUrl");
                    RemoveEmptyField(catalogEntry, "tags");
                }
            }
        }

        void RemoveEmptyField(JObject catalogEntry, string name)
        {
            JToken field = catalogEntry[name];

            if (field.Type == JTokenType.Array)
            {
                var array = (JArray)field;
                if (array.Count == 1 && string.IsNullOrEmpty((string)array[0]))
                {
                    catalogEntry.Remove(name);
                }
            }
            else
            {
                var prop = (string)field;
                if (string.IsNullOrEmpty(prop))
                {
                    catalogEntry.Remove(name);
                }
            }
        }

        async Task<JObject> FetchMissingPage(string pageUri)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(pageUri);

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (var textReader = new StreamReader(stream))
            {
                using (var jsonReader = new JsonSkipReader(new JsonTextReader(textReader), RegistrationPageJsonPathToSkip()))
                {
                    return JObject.Load(jsonReader);
                }
            }
        }

        async Task<string> GetRemoteAddress(string type)
        {
            var resources = await ReadRemoteIndex();
            return resources[type].FirstOrDefault();
        }

        async Task<IDictionary<string, List<string>>> ReadRemoteIndex()
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(_remote);

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (var textReader = new StreamReader(stream))
            {
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    JToken index = JToken.Load(jsonReader);

                    var resources = new Dictionary<string, List<string>>();

                    foreach (var resourceObj in index["resources"])
                    {
                        string resourceType = resourceObj["@type"].ToString();
                        string resourceId = resourceObj["@id"].ToString();

                        List<string> resourceIds;
                        if (!resources.TryGetValue(resourceType, out resourceIds))
                        {
                            resourceIds = new List<string>();
                            resources.Add(resourceType, resourceIds);
                        }

                        resourceIds.Add(resourceId);
                    }

                    return resources;
                }
            }
        }
        async Task WriteIndex(IOwinContext context, IDictionary<string, List<string>> resources)
        {
            var resourceArray = new JArray();

            foreach (var resource in resources)
            {
                foreach (var id in resource.Value)
                {
                    var resourceObj = new JObject
                    {
                        { "@type", resource.Key },
                        { "@id", id }
                    };
                    resourceArray.Add(resourceObj);
                }
            }

            var index = new JObject
            {
                { "version", "3.0.0-beta.1" },
                { "resources", resourceArray }
            };

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(index.ToString(Formatting.None));
        }

        IEnumerable<string> RegistrationIndexJsonPathToSkip()
        {
            var jsonPathsToSkip = new[]
            {
                "@id",
                "@type",
                "commitId",
                "commitTimeStamp",
                "count",
                "@context"
            };

            return jsonPathsToSkip.Concat(RegistrationPageJsonPathToSkip().Select(s => "items[*]." + s)).ToArray();
        }
        IEnumerable<string> RegistrationPageJsonPathToSkip()
        {
            var jsonPathsToSkip = new []
            {
                "@type",
                "commitId",
                "commitTimeStamp",
                "count",
                "parent",
                "items[*].@id",
                "items[*].@type",
                "items[*].commitId",
                "items[*].commitTimeStamp",
                "items[*].registration",
                "items[*].catalogEntry.@type",
                "items[*].catalogEntry.language",
                "items[*].catalogEntry.minClientVersion",
                "items[*].catalogEntry.summary",
                "items[*].catalogEntry.title",
                "items[*].catalogEntry.commitId",
                "items[*].catalogEntry.commitTimeStamp",
                "items[*].catalogEntry.packageContent",
                "items[*].catalogEntry.registration",
                "items[*].catalogEntry.dependencyGroups[*].@id",
                "items[*].catalogEntry.dependencyGroups[*].@type",
                "items[*].catalogEntry.dependencyGroups[*].dependencies[*].@id",
                "items[*].catalogEntry.dependencyGroups[*].dependencies[*].@type",
                "items[*].catalogEntry.dependencyGroups[*].dependencies[*].registration",
                "@context"
            };

            return jsonPathsToSkip;
        }
    }
}