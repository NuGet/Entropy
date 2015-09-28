using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace StagingWebApi
{
    public class StageResource : ResourceBase
    {
        string _ownerName;
        string _stageId;
        List<StagePackage> _packages;

        public StageResource(string ownerName, string stageId)
        {
            _ownerName = ownerName;
            _stageId = stageId;
            _packages = new List<StagePackage>();

            Packages = new List<PackageResource>();

            Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/StagingWebApi");
            ConnectionString = rootWebConfig.ConnectionStrings.ConnectionStrings["PackageStaging"].ConnectionString;
        }

        public string ConnectionString
        {
            get;
            set;
        }

        public List<PackageResource> Packages
        {
            get; private set;
        }

        public override async Task<HttpResponseMessage> Save()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("CreateStage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", _ownerName);
                command.Parameters.AddWithValue("StageId", _stageId);

                int result = (int)await command.ExecuteScalarAsync();

                if (result == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.Created);
                }
                else
                {
                    //TODO: add other error scenarios
                    return new HttpResponseMessage(HttpStatusCode.Conflict);
                }
            }
        }

        public override async Task<HttpResponseMessage> Load()
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
                      AND [Stage].[Id] = @StageId
                ", connection);

                command.Parameters.AddWithValue("OwnerName", _ownerName);
                command.Parameters.AddWithValue("StageId", _stageId);

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

                        _packages.Add(new StagePackage { Id = id, Version = version });
                    }
                }

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = Utils.CreateJsonContent(ToJson());
                return response;
            }
        }

        public override async Task<HttpResponseMessage> Delete()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("DeleteStage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", _ownerName);
                command.Parameters.AddWithValue("StageId", _stageId);

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

                        Packages.Add(new PackageResource(
                            _ownerName,
                            _stageId,
                            new StagePackage { Id = id, Version = version },
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