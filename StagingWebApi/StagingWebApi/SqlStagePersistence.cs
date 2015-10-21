using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace StagingWebApi
{
    public class SqlStagePersistence : IStagePersistence
    {
        public SqlStagePersistence()
        {
            Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/StagingWebApi");
            ConnectionString = rootWebConfig.ConnectionStrings.ConnectionStrings["PackageStaging"].ConnectionString;
        }

        public string ConnectionString { get; set; }

        public async Task<HttpResponseMessage> GetOwner(string ownerName)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("GetOwner", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                Owner owner = new Owner("", ownerName);

                while (reader.Read())
                {
                    string ownerNameDB = reader.GetString(0);
                    string stageName = reader.GetString(1);

                    owner.Name = ownerNameDB;

                    if (reader.IsDBNull(2))
                    {
                        owner.Add(stageName);
                    }
                    else
                    {
                        string id = reader.GetString(2);
                        string version = reader.GetString(3);
                        DateTime staged = reader.GetDateTime(4);
                        string nuspecLocation = reader.GetString(5);
                        string packageOwner = reader.GetString(6);

                        owner.Add(stageName, id, version, staged, nuspecLocation, packageOwner);
                    }
                }

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = Utils.CreateJsonContent(owner.ToJson());
                return response;
            }
        }

        public async Task<HttpResponseMessage> GetStage(string ownerName, string stageName)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("GetStage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                Stage stage = new Stage("", stageName);

                while (reader.Read())
                {
                    string stageOwner = reader.GetString(0);
                    string stageNameDB = reader.GetString(1);

                    stage.Name = stageNameDB;

                    if (!reader.IsDBNull(2))
                    {
                        string id = reader.GetString(2);
                        string version = reader.GetString(3);
                        DateTime staged = reader.GetDateTime(4);
                        string nuspecLocation = reader.GetString(5);
                        string packageOwner = reader.GetString(6);

                        stage.Add(id, version, staged, nuspecLocation, packageOwner);
                    }
                }

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = Utils.CreateJsonContent(stage.ToJson());
                return response;
            }
        }

        public async Task<HttpResponseMessage> GetPackage(string ownerName, string stageName, string packageId)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("GetPackage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("Id", packageId);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                Package package = new Package("", packageId);

                while (reader.Read())
                {
                    string idDB = reader.GetString(0);

                    package.Id = idDB;

                    string version = reader.GetString(1);
                    DateTime staged = reader.GetDateTime(2);
                    string nuspecLocation = reader.GetString(3);
                    string packageOwner = reader.GetString(4);
                    package.Add(version, staged, nuspecLocation, packageOwner);
                }

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = Utils.CreateJsonContent(package.ToJson());
                return response;
            }
        }

        public async Task<HttpResponseMessage> GetPackageVersion(string ownerName, string stageName, string packageId, string packageVersion)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("GetPackageVersion", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("Id", packageId);
                command.Parameters.AddWithValue("Version", packageVersion);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                PackageVersion packageVer = null;

                while (reader.Read())
                {
                    string version = reader.GetString(0);
                    DateTime staged = reader.GetDateTime(1);
                    string nuspecLocation = reader.GetString(2);

                    packageVer = new PackageVersion("", packageVersion, staged, nuspecLocation);
                }

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = Utils.CreateJsonContent(packageVer.ToJson());
                return response;
            }
        }

        public async Task<bool> ExistsStage(string ownerName, string stageName)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("ExistsStage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);

                return await command.ExecuteScalarAsync() != null;
            }
        }

        public async Task<Tuple<HttpResponseMessage, List<Uri>>> DeleteStage(string ownerName, string stageName)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("DeleteStage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                List<Uri> storageArtifacts = new List<Uri>();

                if (!reader.HasRows)
                {
                    return Tuple.Create(new HttpResponseMessage(HttpStatusCode.NotFound), storageArtifacts);
                }

                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        Uri nupkgLocation = new Uri(reader.GetString(2));
                        Uri nuspecLocation = new Uri(reader.GetString(3));

                        storageArtifacts.Add(nupkgLocation);
                        storageArtifacts.Add(nuspecLocation);
                    }
                }

                return Tuple.Create(new HttpResponseMessage(HttpStatusCode.OK), storageArtifacts);
            }
        }

        public async Task<Tuple<HttpResponseMessage, List<Uri>>> DeletePackage(string ownerName, string stageName, string packageId)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("DeletePackage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("Id", packageId);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                List<Uri> storageArtifacts = new List<Uri>();

                if (!reader.HasRows)
                {
                    return Tuple.Create(new HttpResponseMessage(HttpStatusCode.NotFound), storageArtifacts);
                }

                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        Uri nupkgLocation = new Uri(reader.GetString(2));
                        Uri nuspecLocation = new Uri(reader.GetString(3));

                        storageArtifacts.Add(nupkgLocation);
                        storageArtifacts.Add(nuspecLocation);
                    }
                }

                return Tuple.Create(new HttpResponseMessage(HttpStatusCode.OK), storageArtifacts);
            }
        }

        public async Task<Tuple<HttpResponseMessage, List<Uri>>> DeletePackageVersion(string ownerName, string stageName, string packageId, string packageVersion)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("DeletePackageVersion", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("Id", packageId);
                command.Parameters.AddWithValue("Version", packageVersion);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                List<Uri> storageArtifacts = new List<Uri>();

                if (!reader.HasRows)
                {
                    return Tuple.Create(new HttpResponseMessage(HttpStatusCode.NotFound), storageArtifacts);
                }

                while (reader.Read())
                {
                    Uri nupkgLocation = new Uri(reader.GetString(0));
                    Uri nuspecLocation = new Uri(reader.GetString(1));

                    storageArtifacts.Add(nupkgLocation);
                    storageArtifacts.Add(nuspecLocation);
                }

                return Tuple.Create(new HttpResponseMessage(HttpStatusCode.OK), storageArtifacts);
            }
        }

        public async Task<HttpResponseMessage> CreateStage(string ownerName, string stageName, string parentAddress)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("CreateStage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("BaseService", parentAddress);

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

        public async Task<HttpResponseMessage> CreatePackage(Uri baseAddress, string ownerName, string stageName, string packageId, string packageVersion, string packageOwner, Uri nupkgLocation, Uri nuspecLocation)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var staged = DateTime.UtcNow;

                SqlCommand command = new SqlCommand("CreatePackage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("Id", packageId);
                command.Parameters.AddWithValue("Version", packageVersion);
                command.Parameters.AddWithValue("PackageOwner", packageOwner);
                command.Parameters.AddWithValue("NupkgLocation", nupkgLocation.AbsoluteUri);
                command.Parameters.AddWithValue("NuspecLocation", nuspecLocation.AbsoluteUri);
                command.Parameters.AddWithValue("Staged", staged);

                int result = (int)await command.ExecuteScalarAsync();
                switch (result)
                {
                    case 0: return PackageVersion.HttpCreateResponse(baseAddress, ownerName, stageName, packageId, packageVersion, staged, nuspecLocation);
                    case 1: return Utils.CreateErrorResponse(HttpStatusCode.Conflict, "package exists");
                    case 2: return Utils.CreateErrorResponse(HttpStatusCode.BadRequest, "owner not recognized");
                }

                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
    }
}
