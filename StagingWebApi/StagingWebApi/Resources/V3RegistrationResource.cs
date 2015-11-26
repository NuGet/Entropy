using JsonLD.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
<<<<<<< HEAD
=======
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
using NuGet.Services.Metadata.Catalog;
using NuGet.Services.Metadata.Catalog.JsonLDIntegration;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
<<<<<<< HEAD
using System.Web;
=======
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
using System.Web.Configuration;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace StagingWebApi.Resources
{
    public class V3RegistrationResource : StageResourceBase
    {
        public V3RegistrationResource(Uri registrationUri, string ownerName, string stageId)
            : base(ownerName, stageId)
        {
            RegistrationUri = registrationUri;

            Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/StagingWebApi");
            ConnectionString = rootWebConfig.ConnectionStrings.ConnectionStrings["PackageStaging"].ConnectionString;
        }

        public string ConnectionString
        {
            get;
            private set;
        }

        public Uri RegistrationUri
        {
            get;
            private set;
        }

        public async Task<IDictionary<string, PackageDetails>> GetPackageDetails()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
                    SELECT StagePackage.[Id], StagePackage.[Version]
                    FROM StagePackage
                    INNER JOIN Stage ON StagePackage.StageKey = Stage.[Key]
                    INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
                    INNER JOIN Owner ON Owner.[Key] = StageOwner.OwnerKey
                    WHERE Owner.Name = @OwnerName
                      AND Stage.Name = @StageName
                ", connection);

                command.Parameters.AddWithValue("OwnerName", OwnerName);
                command.Parameters.AddWithValue("StageName", StageName);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                IDictionary<string, PackageDetails> result = new Dictionary<string, PackageDetails>(StringComparer.OrdinalIgnoreCase);

                while (reader.Read())
                {
                    string id = reader.GetString(0);
                    string version = reader.GetString(1);

                    PackageDetails packageDetails;
                    if (!result.TryGetValue(id, out packageDetails))
                    {
                        packageDetails = new PackageDetails(id);
                        result.Add(id, packageDetails);
                    }

                    packageDetails.Versions.Add(version);
                }

                return result;
            }
        }

        public async Task<HttpResponseMessage> GetIndex(string id)
        {
            Uri baseServiceResource = await GetBaseServiceIndex(id);

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
                    SELECT StagePackage.[Version], StagePackage.NupkgLocation, StagePackage.NuspecLocation
                    FROM StagePackage
                    INNER JOIN Stage ON Stage.[Key] = StagePackage.StageKey
                    INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
                    INNER JOIN Owner ON Owner.[Key] = StageOwner.OwnerKey
                    WHERE Owner.Name = @OwnerName
                      AND Stage.Name = @StageName
                      AND StagePackage.[Id] = @Id
                ", connection);

                command.Parameters.AddWithValue("OwnerName", OwnerName);
                command.Parameters.AddWithValue("StageName", StageName);
                command.Parameters.AddWithValue("Id", id.ToLowerInvariant());

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    if (baseServiceResource == null)
                    {
                        return new HttpResponseMessage(HttpStatusCode.NotFound);
                    }
                    else
                    {
                        HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.Redirect);
                        redirect.Headers.Location = baseServiceResource;
                        return redirect;
                    }
                }
                else
                {
                    List<JObject> packageMetatdata = new List<JObject>();

                    IDictionary<string, string> packageContentLocations = new Dictionary<string, string>();

                    while (reader.Read())
                    {
                        string version = reader.GetString(0);
                        string nupkgLocation = reader.GetString(1);
                        string nuspecLocation = reader.GetString(2);

                        //packageMetatdata.Add(MakeCatalogEntry(id, version));
                        packageMetatdata.Add(await MakeCatalogEntry(nuspecLocation));

                        packageContentLocations[version] = nupkgLocation;
                    }

                    if (baseServiceResource != null)
                    {
                        IEnumerable<JObject> existingPackages = await GetPackageMetadata(baseServiceResource, packageContentLocations);
                        if (existingPackages != null)
                        {
                            packageMetatdata.AddRange(existingPackages);
                        }
                    }

                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = Utils.CreateJsonContent(MakeJson(packageMetatdata, packageContentLocations, RegistrationUri.AbsoluteUri));
                    return response;
                }
            }
        }

        public async Task<HttpResponseMessage> GetPage(string id, string lower, string upper)
        {
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
        }

        async Task<Uri> GetBaseServiceIndex(string id)
        {
            Uri address = await GetRegistrationBaseAddress();
            return (address == null) ? null : new Uri(address, string.Format("{0}/index.json", id).ToLowerInvariant());
        }

        async Task<Uri> GetRegistrationBaseAddress()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
                    SELECT Stage.BaseService
                    FROM Stage
                    INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
                    INNER JOIN Owner ON Owner.[Key] = StageOwner.OwnerKey
                    WHERE Owner.Name = @OwnerName
                      AND Stage.Name = @StageName
                ", connection);

                command.Parameters.AddWithValue("OwnerName", OwnerName);
                command.Parameters.AddWithValue("StageName", StageName);

                string result = (string)await command.ExecuteScalarAsync();

                if (result == null)
                {
                    return null;
                }

                Uri serviceBase = new Uri(result);

                Uri address = await Utils.GetService(serviceBase, "RegistrationsBaseUrl/3.0.0-beta");

                return (address == null) ? null : address;
            }
        }

        JObject MakeCatalogEntry(string id, string version)
        {
            JObject obj = new JObject();

            obj["@id"] = string.Format("http://tempuri.org#{0}/{1}", id, version).ToLowerInvariant();
            obj["@type"] = "PackageDetails";
            obj["authors"] = "";
            obj["description"] = id;
            obj["iconUrl"] = "";
            obj["id"] = id;
            obj["language"] = "";
            obj["licenseUrl"] = "";
            obj["listed"] = true;
            obj["minClientVersion"] = "";
            obj["projectUrl"] = "";
            obj["published"] = "1999-01-01T00:00:00.000Z";
            obj["requireLicenseAcceptance"] = false;
            obj["summary"] = id;
            obj["tags"] = new JArray("");
            obj["title"] = id;
            obj["version"] = version;

            return obj;
        }

        async Task<JObject> MakeCatalogEntry(string nuspecLocation)
        {
            Uri baseAddress = new Uri("http://tempuri.org/test");

            HttpClient client = new HttpClient();

            XDocument original = XDocument.Load(await client.GetStreamAsync(nuspecLocation));

            XDocument nuspec = NormalizeNuspecNamespace(original, GetXslt("XSLT.normalizeNuspecNamespace.xslt"));
            IGraph graph = CreateNuspecGraph(nuspec, baseAddress, GetXslt("XSLT.nuspec.xslt"));

            //  Compact JSON-LD projection of RDF

            JToken frame = GetJson("Context.package.json");

            using (var writer = new StringWriter())
            {
                IRdfWriter jsonLdWriter = new JsonLdWriter();
                jsonLdWriter.Save(graph, writer);
                writer.Flush();

                JToken flattened = JToken.Parse(writer.ToString());
                JObject framed = JsonLdProcessor.Frame(flattened, frame, new JsonLdOptions());
                JObject compacted = JsonLdProcessor.Compact(framed, frame["@context"], new JsonLdOptions());

                compacted.Remove("@context");

                return compacted;
            }
        }

        JToken GetJson(string name)
        {
            using (var stream = GetResourceStream(name))
            {
                using (var reader = new StreamReader(stream))
                {
                    return JToken.Parse(reader.ReadToEnd());
                }
            }
        }

        XslCompiledTransform GetXslt(string name)
        {
            using (var stream = GetResourceStream(name))
            {
                using (var reader = new StreamReader(stream))
                {
                    XslCompiledTransform xslt = new XslCompiledTransform();
                    xslt.Load(XmlReader.Create(reader));
                    return xslt;
                }
            }
        }

        public static Stream GetResourceStream(string resName)
        {
            foreach (string resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                string s = resourceName;
            }
            string name = Assembly.GetExecutingAssembly().GetName().Name;
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(name + "." + resName);
        }

        static XDocument NormalizeNuspecNamespace(XDocument original, XslCompiledTransform xslt)
        {
            XDocument result = new XDocument();
            using (XmlWriter writer = result.CreateWriter())
            {
                xslt.Transform(original.CreateReader(), writer);
            }
            return result;
        }

        static IGraph CreateNuspecGraph(XDocument nuspec, Uri baseAddress, XslCompiledTransform xslt)
        {
            XsltArgumentList arguments = new XsltArgumentList();
            arguments.AddParam("base", "", baseAddress.ToString());
            arguments.AddParam("extension", "", ".json");

            arguments.AddExtensionObject("urn:helper", new XsltHelper());

            XDocument rdfxml = new XDocument();
            using (XmlWriter writer = rdfxml.CreateWriter())
            {
                xslt.Transform(nuspec.CreateReader(), arguments, writer);
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(rdfxml.CreateReader());

            IGraph graph = new Graph();
            RdfXmlParser rdfXmlParser = new RdfXmlParser();
            rdfXmlParser.Load(graph, doc);

            return graph;
        }

        async static Task<IEnumerable<JObject>> LoadRanges(HttpClient httpClient, Uri registrationUri, CancellationToken token)
        {
            var index = await Utils.LoadResource(httpClient, registrationUri, token);
            if (index == null)
            {
                return null;
            }

            IList<Task<JObject>> rangeTasks = new List<Task<JObject>>();
            foreach (JObject item in index["items"])
            {
                var lower = NuGetVersion.Parse(item["lower"].ToString());
                var upper = NuGetVersion.Parse(item["upper"].ToString());
                JToken items;
                if (!item.TryGetValue("items", out items))
                {
                    var rangeUri = item["@id"].ToObject<Uri>();
                    rangeTasks.Add(Utils.LoadResource(httpClient, rangeUri, token));
                }
                else
                {
                    rangeTasks.Add(Task.FromResult(item));
                }
            }
            await Task.WhenAll(rangeTasks.ToArray());
            return rangeTasks.Select((t) => t.Result);
        }

        async Task<IEnumerable<JObject>> GetPackageMetadata(Uri registrationUri, IDictionary<string, string> packageContentLocations)
        {
            HttpClient client = new HttpClient();
            var ranges = await LoadRanges(client, registrationUri, CancellationToken.None);
            if (ranges == null)
            {
                return null;
            }

            var results = new List<JObject>();
            foreach (var rangeObj in ranges)
            {
                foreach (JObject packageObj in rangeObj["items"])
                {
                    var catalogEntry = (JObject)packageObj["catalogEntry"];
                    results.Add(catalogEntry);

                    packageContentLocations[packageObj["catalogEntry"]["version"].ToString()] = packageObj["packageContent"].ToString();
                }
            }
            return results;
        }

        static string MakeJson(List<JObject> packageMetadata, IDictionary<string, string> packageContentLocations, string registration)
        {
            NuGetVersion lower = null;
            NuGetVersion upper = null;
            foreach (JObject entry in packageMetadata)
            {
                NuGetVersion currentEntry = NuGetVersion.Parse(entry["version"].ToString());

                if (lower == null || lower > currentEntry)
                {
                    lower = currentEntry;
                }
                if (upper == null || upper < currentEntry)
                {
                    upper = currentEntry;
                }
            }

            using (StringWriter writer = new StringWriter())
            {
                using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
                {
                    jsonWriter.Formatting = Newtonsoft.Json.Formatting.Indented;

                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("items");
                    jsonWriter.WriteStartArray();

                    //  just a single range
                    jsonWriter.WriteStartObject();

                    jsonWriter.WritePropertyName("lower");
                    jsonWriter.WriteValue(lower.ToNormalizedString());

                    jsonWriter.WritePropertyName("upper");
                    jsonWriter.WriteValue(upper.ToNormalizedString());

                    jsonWriter.WritePropertyName("items");
                    jsonWriter.WriteStartArray();

                    foreach (JObject catalogEntry in packageMetadata)
                    {
                        string packageContent = packageContentLocations[catalogEntry["version"].ToString()];

                        jsonWriter.WriteStartObject();
                        jsonWriter.WritePropertyName("packageContent");
                        jsonWriter.WriteValue(packageContent);
                        jsonWriter.WritePropertyName("registration");
                        jsonWriter.WriteValue(registration);
                        jsonWriter.WritePropertyName("catalogEntry");
                        jsonWriter.WriteRawValue(catalogEntry.ToString());
                        jsonWriter.WriteEndObject();
                    }

                    jsonWriter.WriteEndArray();
                    jsonWriter.WriteEndObject();
                    jsonWriter.WriteEndArray();

                    jsonWriter.WriteEndObject();

                    jsonWriter.Flush();
                    writer.Flush();
                    return writer.ToString();
                }
            }
        }
    }
}