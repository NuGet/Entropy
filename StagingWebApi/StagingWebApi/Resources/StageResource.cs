using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace StagingWebApi.Resources
{
    //TODO: addresses the potential duplication coming out across these "resource" classes.

    public class StageResource : StageResourceBase, IResource
    {
        List<StagePackage> _packages;

        public StageResource(string ownerName, string stageName)
            : this(ownerName, stageName, null)
        {
        }

        public StageResource(string ownerName, string stageName, string baseService)
            : base(ownerName, stageName)
        {
            BaseService = baseService;

            //TODO: this allocation can be lazy
            _packages = new List<StagePackage>();
            Packages = new List<PackageResource>();

            Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/StagingWebApi");
            ConnectionString = rootWebConfig.ConnectionStrings.ConnectionStrings["PackageStaging"].ConnectionString;
        }

        public string ConnectionString { get; set; }
        public string BaseService { get; private set; }
        public List<PackageResource> Packages { get; private set; }

        public async Task<bool> Exists()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
                    SELECT 1
                    FROM Stage
                    INNER JOIN [StageOwner] ON[Stage].[Key] = [StageOwner].[StageKey]
                    INNER JOIN [Owner] ON[Owner].[Key] = [StageOwner].[OwnerKey]
                    WHERE [Owner].[Name] = @OwnerName
                      AND [Stage].[Name] = @StageName
                ", connection);

                command.Parameters.AddWithValue("OwnerName", OwnerName);
                command.Parameters.AddWithValue("StageName", StageName);

                return await command.ExecuteScalarAsync() != null;
            }
        }

        public async Task<HttpResponseMessage> Save()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("CreateStage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", OwnerName);
                command.Parameters.AddWithValue("StageName", StageName);
                command.Parameters.AddWithValue("BaseService", BaseService);

                int result = (int)await command.ExecuteScalarAsync();

                if (result == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.Created);
                }
                else if (result == 2)
                {
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    response.Content = Utils.CreateErrorContent("owner not found");
                    return response;
                }
                else if (result == 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.Conflict);
                }
                else
                {
                    Trace.TraceError("unexpected error from database executing CreateStage");
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
            }
        }

        public async Task<HttpResponseMessage> Load()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
                    SELECT [StagePackage].[Id], [StagePackage].[Version]
                    FROM Stage
                    INNER JOIN [StageOwner] ON[Stage].[Key] = [StageOwner].[StageKey]
                    INNER JOIN [Owner] ON[Owner].[Key] = [StageOwner].[OwnerKey]
                    LEFT OUTER JOIN [StagePackage] ON[Stage].[Key] = [StagePackage].[StageKey]
                    WHERE [Owner].[Name] = @OwnerName
                      AND [Stage].[Name] = @StageName
                ", connection);

                command.Parameters.AddWithValue("OwnerName", OwnerName);
                command.Parameters.AddWithValue("StageName", StageName);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        string id = reader.GetString(0);
                        string version = reader.GetString(1);

                        StagePackage package = new StagePackage();
                        package.Load(id, version);

                        _packages.Add(package);
                    }
                }

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = Utils.CreateJsonContent(ToJson());
                return response;
            }
        }

        public async Task<HttpResponseMessage> Delete()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("DeleteStage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", OwnerName);
                command.Parameters.AddWithValue("StageName", StageName);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        string id = reader.GetString(0);
                        string version = reader.GetString(1);
                        Uri nupkgLocation = new Uri(reader.GetString(2));
                        Uri nuspecLocation = new Uri(reader.GetString(3));

                        StagePackage package = new StagePackage();
                        package.Load(id, version);

                        Packages.Add(new PackageResource(
                            OwnerName,
                            StageName,
                            package,
                            nupkgLocation,
                            nuspecLocation));
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        string ToJson()
        {
            using (StringWriter textWriter = new StringWriter())
            {
                using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter))
                {
                    jsonWriter.Formatting = Formatting.Indented;

                    jsonWriter.WriteStartArray();
                    foreach (StagePackage package in _packages)
                    {
                        jsonWriter.WriteStartObject();
                        jsonWriter.WritePropertyName("id");
                        jsonWriter.WriteValue(package.Id);
                        jsonWriter.WritePropertyName("version");
                        jsonWriter.WriteValue(package.Version);
                        jsonWriter.WriteEndObject();
                    }
                    jsonWriter.WriteEndArray();

                    jsonWriter.Flush();
                    textWriter.Flush();

                    return textWriter.ToString();
                }
            }
        }
    }
}
