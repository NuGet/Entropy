using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace StagingWebApi.Resources
{
    class V3FlatResource : StageResourceBase
    {
        public V3FlatResource(string ownerName, string stageId)
            : base(ownerName, stageId)
        {
            Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/StagingWebApi");
            ConnectionString = rootWebConfig.ConnectionStrings.ConnectionStrings["PackageStaging"].ConnectionString;
        }

        public string ConnectionString
        {
            get;
            set;
        }

        public async Task<HttpResponseMessage> Get(string id)
        {
            Uri baseServiceResource = await GetBaseServiceIndex(id);

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
                    SELECT StagePackage.[Version]
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
                    HashSet<string> versions = new HashSet<string>();

                    while (reader.Read())
                    {
                        string version = reader.GetString(0);

                        versions.Add(version);
                    }

                    if (baseServiceResource != null)
                    {
                        await FetchBaseServiceVersions(baseServiceResource, versions);
                    }

                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = Utils.CreateJsonContent(MakeJson(versions));
                    return response;
                }
            }
        }

        public async Task<HttpResponseMessage> GetNupkg(string id, string version)
        {
            NuGetVersion nugetVersion;
            if (!NuGetVersion.TryParse(version, out nugetVersion))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            PackageLocation location = await GetPackageLocation(id, nugetVersion.ToNormalizedString());

            if (location == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            else
            {
                HttpResponseMessage redirectResponse = new HttpResponseMessage(HttpStatusCode.Redirect);
                redirectResponse.Headers.Location = location.NupkgLocation;
                return redirectResponse;
            }
        }

        public async Task<HttpResponseMessage> GetNuspec(string id, string version)
        {
            NuGetVersion nugetVersion;
            if (!NuGetVersion.TryParse(version, out nugetVersion))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            PackageLocation location = await GetPackageLocation(id, nugetVersion.ToNormalizedString());

            if (location == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            else
            {
                HttpResponseMessage redirectResponse = new HttpResponseMessage(HttpStatusCode.Redirect);
                redirectResponse.Headers.Location = location.NuspecLocation;
                return redirectResponse;
            }
        }

        async Task<PackageLocation> GetPackageLocation(string id, string normalizedVersion)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
                    SELECT StagePackage.NupkgLocation, StagePackage.NuspecLocation
                    FROM StagePackage
                    INNER JOIN Stage ON Stage.[Key] = StagePackage.StageKey
                    INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
                    INNER JOIN Owner ON Owner.[Key] = StageOwner.OwnerKey
                    WHERE Owner.Name = @OwnerName
                      AND Stage.Name = @StageName
                      AND StagePackage.[Id] = @Id
                      AND StagePackage.Version = @Version
                ", connection);

                command.Parameters.AddWithValue("OwnerName", OwnerName);
                command.Parameters.AddWithValue("StageName", StageName);
                command.Parameters.AddWithValue("Id", id.ToLowerInvariant());
                command.Parameters.AddWithValue("Version", normalizedVersion);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return await GetBaseServicePackageLocation(id, normalizedVersion);
                }
                else
                {
                    string nupkgLocation = null;
                    string nuspecLocation = null;
                    while (reader.Read())
                    {
                        nupkgLocation = reader.GetString(0);
                        nuspecLocation = reader.GetString(1);
                    }
                    return new PackageLocation { NupkgLocation = new Uri(nupkgLocation), NuspecLocation = new Uri(nuspecLocation) };
                }
            }
        }

        async Task<Uri> GetBaseServiceIndex(string id)
        {
            Uri address = await GetPackageBaseAddress();
            return (address == null) ? null : new Uri(address, string.Format("{0}/index.json", id).ToLowerInvariant());
        }

        async Task<PackageLocation> GetBaseServicePackageLocation(string id, string normalizedVersion)
        {
            Uri address = await GetPackageBaseAddress();
            return (address == null) ? null : PackageLocation.Create(address, id, normalizedVersion);
        }

        async Task<Uri> GetPackageBaseAddress()
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

                Uri address = await Utils.GetService(serviceBase, "PackageBaseAddress/3.0.0");

                return (address == null) ? null : address;
            }
        }

        static async Task FetchBaseServiceVersions(Uri baseServiceResource, HashSet<string> versions)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(baseServiceResource);

            if (response.IsSuccessStatusCode)
            {
                JObject obj = JObject.Parse(await response.Content.ReadAsStringAsync());
                foreach (string version in obj["versions"])
                {
                    versions.Add(version);
                }
            }
        }

        static string MakeJson(IEnumerable<string> versions)
        {
            using (StringWriter writer = new StringWriter())
            {
                using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
                {
                    jsonWriter.Formatting = Formatting.Indented;

                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("versions");
                    jsonWriter.WriteStartArray();
                    foreach (string version in versions)
                    {
                        jsonWriter.WriteValue(version);
                    }
                    jsonWriter.WriteEndArray();
                    jsonWriter.WriteEndObject();

                    jsonWriter.Flush();
                    writer.Flush();
                    return writer.ToString();
                }
            }
        }

        class PackageLocation
        {
            public Uri NupkgLocation { get; set; }
            public Uri NuspecLocation { get; set; }

            public static PackageLocation Create(Uri baseAddress, string id, string normalizedVersion)
            {
                return new PackageLocation
                {
                    NupkgLocation = new Uri(baseAddress, string.Format("{0}/{1}/{0}.{1}.nupkg", id, normalizedVersion)),
                    NuspecLocation = new Uri(baseAddress, string.Format("{0}/{1}/{0}.nuspec", id, normalizedVersion))
                };
            }
        }
    }
}
