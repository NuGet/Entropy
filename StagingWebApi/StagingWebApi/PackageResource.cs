using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace StagingWebApi
{
    public class PackageResource : ResourceBase
    {
        string _ownerName;
        string _stageId;
        StagePackage _stagePackage;

        public PackageResource(string ownerName, string stageId, StagePackage stagePackage, Uri nupkgLocation, Uri nuspecLocation)
        {
            _ownerName = ownerName;
            _stageId = stageId;
            _stagePackage = stagePackage;
            NupkgLocation = nupkgLocation;
            NuspecLocation = nuspecLocation;

            Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/StagingWebApi");
            ConnectionString = rootWebConfig.ConnectionStrings.ConnectionStrings["PackageStaging"].ConnectionString;
        }
        public PackageResource(string ownerName, string stageId, StagePackage stagePackage)
            : this(ownerName, stageId, stagePackage, null, null)
        {
        }

        public string ConnectionString
        {
            get;
            set;
        }

        public Uri NupkgLocation
        {
            get;
            private set;
        }

        public Uri NuspecLocation
        {
            get;
            private set;
        }

        public override async Task<HttpResponseMessage> Delete()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("DeletePackage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", _ownerName);
                command.Parameters.AddWithValue("StageId", _stageId);
                command.Parameters.AddWithValue("Id", _stagePackage.Id);
                command.Parameters.AddWithValue("Version", _stagePackage.Version);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                while (reader.Read())
                {
                    NupkgLocation = new Uri(reader.GetString(0));
                    NuspecLocation = new Uri(reader.GetString(1));
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        public override async Task<HttpResponseMessage> Load()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
                    SELECT
                        [Owner].[Name], 
                        Stage.[Id],
                        StagePackage.[Id],
                        StagePackage.[Version],
                        StagePackage.NupkgLocation,
                        StagePackage.NuspecLocation
                    FROM StagePackage
                    INNER JOIN Stage ON Stage.[Key] = StagePackage.StageKey
                    INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
                    INNER JOIN [Owner] ON [Owner].[Key] = StageOwner.OwnerKey
                    WHERE [Owner].[Name] = @OwnerName
                      AND Stage.[Id] = @StageId
                      AND StagePackage.[Id] = @Id
                      AND StagePackage.[Version] = @Version
                ", connection);

                command.Parameters.AddWithValue("OwnerName", _ownerName);
                command.Parameters.AddWithValue("StageId", _stageId);
                command.Parameters.AddWithValue("Id", _stagePackage.Id);
                command.Parameters.AddWithValue("Version", _stagePackage.Version);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                int rowCount = 0;

                while (reader.Read())
                {
                    _stagePackage = new StagePackage();

                    _ownerName = reader.GetString(0);
                    _stageId = reader.GetString(1);
                    string id = reader.GetString(2);
                    string version = reader.GetString(3);
                    _stagePackage.Load(id, version);
                    NupkgLocation = new Uri(reader.GetString(4));
                    NuspecLocation = new Uri(reader.GetString(5));

                    rowCount++;
                }

                if (rowCount > 1)
                {
                    Trace.TraceError("attempt to load {0} {1} {2} {3} returned multiple rows", _ownerName, _stageId, _stagePackage.Id, _stagePackage.Version);
                    HttpResponseMessage errResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                    errResponse.Content = Utils.CreateErrorContent("multiple rows in the package table conflicted");
                    return errResponse;
                }

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = Utils.CreateJsonContent(ToJson());
                return response;
            }
        }

        public override async Task<HttpResponseMessage> Save()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("CreatePackage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", _ownerName);
                command.Parameters.AddWithValue("StageId", _stageId);
                command.Parameters.AddWithValue("Id", _stagePackage.Id);
                command.Parameters.AddWithValue("Version", _stagePackage.Version);
                command.Parameters.AddWithValue("NupkgLocation", NupkgLocation.AbsoluteUri);
                command.Parameters.AddWithValue("NuspecLocation", NuspecLocation.AbsoluteUri);

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

        string ToJson()
        {
            using (StringWriter textWriter = new StringWriter())
            {
                using (JsonTextWriter jsonWriter = new JsonTextWriter(textWriter))
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("id");
                    jsonWriter.WriteValue(_stagePackage.Id);
                    jsonWriter.WritePropertyName("version");
                    jsonWriter.WriteValue(_stagePackage.Version);
                    jsonWriter.WriteEndObject();

                    jsonWriter.Flush();
                    textWriter.Flush();

                    return textWriter.ToString();
                }
            }
        }
    }
}
