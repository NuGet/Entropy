// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
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

            Uri searchQuery = new Uri(searchAddress.AbsoluteUri + Request.RequestUri.Query);

            HttpClient client = new HttpClient();
            HttpResponseMessage baseServiceResponse = await client.GetAsync(searchQuery);

            SearchQuery query = ProcessQuery(Request.GetQueryNameValuePairs());

            V3RegistrationResource registration = new V3RegistrationResource(null, owner, name);

            string authority = Request.RequestUri.GetLeftPart(UriPartial.Authority);
            string registrationsBaseAddress = string.Format("{0}/v3/registration/{1}/{2}/", authority, owner, name);

            string json = await baseServiceResponse.Content.ReadAsStringAsync();
            return await RewriteResponse(registration, registrationsBaseAddress, query, json);
        }

        static async Task<HttpResponseMessage> RewriteResponse(V3RegistrationResource registration, string registrationsBaseAddress, SearchQuery query, string json)
        {
            IDictionary<string, PackageDetails> stagePackages = await registration.GetPackageDetails();

            JObject obj = JObject.Parse(json);

            HashSet<string> packageIdsFromRemote = new HashSet<string>(obj["data"].Select((p) => p["id"].ToString()), StringComparer.OrdinalIgnoreCase);

            // simple hack for now - just add the local stage packages into the result from the server
            foreach (var stagePackage in stagePackages.Values)
            {
                if (!packageIdsFromRemote.Contains(stagePackage.Id))
                {
                    ((JArray)obj["data"]).Add(MakeSearchResultEntry(registrationsBaseAddress, stagePackage));
                }
            }

            RAMDirectory directory = new RAMDirectory();

            //TODO: need to use same Analyzer as NuGet.org otherwise this drops data
            IndexWriter writer = new IndexWriter(directory, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), IndexWriter.MaxFieldLength.UNLIMITED);
            
            IDictionary<string, JObject> stash = new Dictionary<string, JObject>();
           
            foreach (JObject entry in obj["data"])
            {
                string id = entry["id"].ToString();

                stash.Add(id, entry);

                Document document = new Document();
                document.Add(new Field("id", id, Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("version", entry["version"].ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("description", entry["description"].ToString(), Field.Store.YES, Field.Index.ANALYZED));
                foreach (JToken tag in entry["tags"])
                {
                    document.Add(new Field("tag", tag.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                }
                writer.AddDocument(document);
            }

            writer.Commit();

            IndexSearcher searcher = new IndexSearcher(directory, true);

            TopDocs topDocs = searcher.Search(MakeLuceneQuery(query.Q), query.Take);

            JArray data = new JArray();

            foreach (var scoreDoc in topDocs.ScoreDocs)
            {
                Document document = searcher.Doc(scoreDoc.Doc);
                string id = document.Get("id");

                JObject stashedEntry;
                if (stash.TryGetValue(id, out stashedEntry))
                {
                    string originalVersion = document.Get("version");
                    NuGetVersion originalNuGetVersion = NuGetVersion.Parse(originalVersion);

                    string entryId = stashedEntry["@id"].ToString();

                    //  really getting quite hacky: only go in here if we are **modifying** a result from the remote service then
                    PackageDetails stagePackage;
                    if (stagePackages.TryGetValue(id, out stagePackage) && packageIdsFromRemote.Contains(stashedEntry["id"].ToString()))
                    {
                        foreach (var stagePackageVersion in stagePackage.Versions)
                        {
                            string versionAddress = string.Format("{0}{1}/{2}.json", registrationsBaseAddress, id, stagePackageVersion);

                            JObject version = new JObject
                            {
                                { "version", stagePackageVersion },
                                { "downloads", 0 },
                                { "@id", versionAddress }
                            };

                            ((JArray)stashedEntry["versions"]).Add(version);

                            if (NuGetVersion.Parse(stagePackageVersion) > originalNuGetVersion)
                            {
                                stashedEntry["version"] = stagePackageVersion;
                                entryId = versionAddress;
                            }
                        }

                        stashedEntry["@id"] = entryId;
                        stashedEntry["registration"] = string.Format("{0}{1}/index.json", registrationsBaseAddress, id).ToLowerInvariant();
                    }

                    data.Add(stashedEntry);
                }
            }

            obj["index"] = obj["index"].ToString() + " (routed and enriched)";
            obj["data"] = data;

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = Utils.CreateJsonContent(obj.ToString());

            return await Task.FromResult(response);
        }

        static JObject MakeSearchResultEntry(string registrationBaseAddress, PackageDetails packageDetails)
        {
            var highestVersion = NuGetVersion.Parse(packageDetails.Versions.First());
            foreach (var packageDetailsVersion in packageDetails.Versions)
            {
                var current = NuGetVersion.Parse(packageDetailsVersion);
                if (highestVersion < current)
                {
                    highestVersion = current;
                }
            }

            string version = highestVersion.ToNormalizedString();

            JObject obj = new JObject();

            string entryId = string.Format("{0}{1}/{2}.json", registrationBaseAddress, packageDetails.Id, version).ToLowerInvariant();

            obj["@id"] = entryId;
            obj["@type"] = "Package";
            obj["registration"] = string.Format("{0}{1}/index.json", registrationBaseAddress, packageDetails.Id).ToLowerInvariant();
            obj["id"] = packageDetails.Id;
            obj["description"] = packageDetails.Id;
            obj["Summary"] = packageDetails.Id;
            obj["title"] = packageDetails.Id;
            obj["iconUrl"] = "";
            obj["licenseUrl"] = "";
            obj["projectUrl"] = "";
            obj["tags"] = new JArray();
            obj["authors"] = "";
            obj["version"] = version;
            obj["versions"] = new JArray();

            foreach (var packageDetailsVersion in packageDetails.Versions)
            {
                JObject versionDetail = new JObject();

                versionDetail["version"] = packageDetailsVersion;
                versionDetail["downloads"] = 0;
                versionDetail["@id"] = string.Format("{0}{1}/{2}.json", registrationBaseAddress, packageDetails.Id, packageDetailsVersion).ToLowerInvariant();

                ((JArray)obj["versions"]).Add(versionDetail);
            }

            return obj;
        }

        static Query MakeLuceneQuery(string requestQuery)
        {
            if (string.IsNullOrWhiteSpace(requestQuery))
            {
                return new MatchAllDocsQuery();
            }
            else
            {
                //TODO: need to use same Analyzer as NuGet.org otherwise this drops data
                QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "id", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));
                string luceneQuery = string.Format("id:{0} description:{0} tag:{0}", requestQuery);
                return parser.Parse(luceneQuery);
            }
        }

        static SearchQuery ProcessQuery(IEnumerable<KeyValuePair<string, string>> query)
        {
            SearchQuery result = new SearchQuery();

            foreach (var item in query)
            {
                if (item.Key.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    result.Q = item.Value;
                }
                else if (item.Key.Equals("skip", StringComparison.OrdinalIgnoreCase))
                {
                    int t = 0;
                    if (int.TryParse(item.Value, out t))
                    {
                        result.Skip = t;
                    }
                }
                else if (item.Key.Equals("take", StringComparison.OrdinalIgnoreCase))
                {
                    int t = 0;
                    if (int.TryParse(item.Value, out t))
                    {
                        result.Take = t;
                    }
                }
                else if (item.Key.Equals("prerelease", StringComparison.OrdinalIgnoreCase))
                {
                    bool t = false;
                    if (bool.TryParse(item.Value, out t))
                    {
                        result.Prerelease = t;
                    }
                }
            }

            return result;
        }

        class SearchQuery
        {
            public SearchQuery()
            {
                Q = string.Empty;
                Skip = 0;
                Take = 30;
                Prerelease = false;
            }

            public string Q { get; set; }
            public int Skip { get; set; }
            public int Take { get; set; }
            public bool Prerelease { get; set; }
        }
    }
}
