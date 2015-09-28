// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
            var rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/StagingWebApi");
            ConnectionString = rootWebConfig.ConnectionStrings.ConnectionStrings["PackageStaging"].ConnectionString;
        }

        public string ConnectionString { get; set; }

<<<<<<< HEAD
<<<<<<< HEAD
        public async Task<HttpResponseMessage> CreateOwner(string ownerName)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                
                SqlCommand command = new SqlCommand("INSERT INTO [dbo].[Owner](Name) VALUES(@OwnerName)", connection);
                command.Parameters.AddWithValue("OwnerName", ownerName);

                int result = (int)await command.ExecuteNonQueryAsync();

                if (result == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.Created);
                }
                else if (result == 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.Conflict);
                }
                else
                {
                    Trace.TraceError("unexpected error from database executing CreateOwner");
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
            }
        }
        
        public async Task<HttpResponseMessage> GetOwner(string ownerName)
=======
=======
>>>>>>> 8b54aa2... push working form nuget.exe
        public async Task<HttpResponseMessage> GetOwner(string ownerName, string v3SourceBaseAddress)
>>>>>>> 697ed21... added angularjs test page
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand("GetOwner", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);

                var reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                var owner = new Owner("", ownerName, v3SourceBaseAddress);

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

                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = Utils.CreateJsonContent(owner.ToJson());
                return response;
            }
        }

        public async Task<HttpResponseMessage> GetStage(string ownerName, string stageName, string v3SourceBaseAddress)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand("GetStage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);

                var reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                var stage = new Stage("", stageName, v3SourceBaseAddress + ownerName.ToLowerInvariant() + "/");

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

                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = Utils.CreateJsonContent(stage.ToJson());
                return response;
            }
        }

        public async Task<HttpResponseMessage> GetPackage(string ownerName, string stageName, string packageId)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand("GetPackage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("Id", packageId);

                var reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                var package = new Package("", packageId);

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

                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = Utils.CreateJsonContent(package.ToJson());
                return response;
            }
        }

        public async Task<HttpResponseMessage> GetPackageVersion(string ownerName, string stageName, string packageId, string packageVersion)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand("GetPackageVersion", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("Id", packageId);
                command.Parameters.AddWithValue("Version", packageVersion);

                var reader = await command.ExecuteReaderAsync();

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

                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = Utils.CreateJsonContent(packageVer.ToJson());
                return response;
            }
        }

        public async Task<bool> ExistsStage(string ownerName, string stageName)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand("ExistsStage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);

                return await command.ExecuteScalarAsync() != null;
            }
        }

        public async Task<Tuple<HttpResponseMessage, List<Uri>>> DeleteStage(string ownerName, string stageName)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand("DeleteStage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);

                var reader = await command.ExecuteReaderAsync();

                var storageArtifacts = new List<Uri>();

                if (!reader.HasRows)
                {
                    return Tuple.Create(new HttpResponseMessage(HttpStatusCode.NotFound), storageArtifacts);
                }

                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        storageArtifacts.Add(new Uri(reader.GetString(2)));
                        storageArtifacts.Add(new Uri(reader.GetString(3)));
                    }
                }

                return Tuple.Create(new HttpResponseMessage(HttpStatusCode.OK), storageArtifacts);
            }
        }

        public async Task<Tuple<HttpResponseMessage, List<Uri>>> DeletePackage(string ownerName, string stageName, string packageId)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand("DeletePackage", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("Id", packageId);

                var reader = await command.ExecuteReaderAsync();

                var storageArtifacts = new List<Uri>();

                if (!reader.HasRows)
                {
                    return Tuple.Create(new HttpResponseMessage(HttpStatusCode.NotFound), storageArtifacts);
                }

                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        storageArtifacts.Add(new Uri(reader.GetString(2)));
                        storageArtifacts.Add(new Uri(reader.GetString(3)));
                    }
                }

                return Tuple.Create(new HttpResponseMessage(HttpStatusCode.OK), storageArtifacts);
            }
        }

        public async Task<Tuple<HttpResponseMessage, List<Uri>>> DeletePackageVersion(string ownerName, string stageName, string packageId, string packageVersion)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand("DeletePackageVersion", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("Id", packageId);
                command.Parameters.AddWithValue("Version", packageVersion);

                var reader = await command.ExecuteReaderAsync();

                var storageArtifacts = new List<Uri>();

                if (!reader.HasRows)
                {
                    return Tuple.Create(new HttpResponseMessage(HttpStatusCode.NotFound), storageArtifacts);
                }

                while (reader.Read())
                {
                    storageArtifacts.Add(new Uri(reader.GetString(0)));
                    storageArtifacts.Add(new Uri(reader.GetString(1)));
                }

                return Tuple.Create(new HttpResponseMessage(HttpStatusCode.OK), storageArtifacts);
            }
        }

        public async Task<HttpResponseMessage> CreateOwner(string ownerName)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("CreateOwner", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);

                string apiKey = (string)await command.ExecuteScalarAsync();

                if (apiKey == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.Conflict);
                }
                else
                {
                    var obj = new JObject { { "apiKey", apiKey } };
                    return Utils.CreateJsonResponse(HttpStatusCode.Created, obj.ToString());
                }
            }
        }

        public async Task<HttpResponseMessage> CreateStage(string ownerName, string stageName, string parentAddress)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand("CreateStage", connection);
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
                    var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
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
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var staged = DateTime.UtcNow;

                var command = new SqlCommand("CreatePackage", connection);
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

        public async Task<string> CheckAccess(string stageName, string apiKey)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("CheckAccess", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("ApiKey", apiKey);

                return (string)await command.ExecuteScalarAsync();
            }
        }

        public async Task<HttpResponseMessage> AddStageOwner(string ownerName, string stageName, string newOwnerName)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand("AddStageOwner", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("OwnerName", ownerName);
                command.Parameters.AddWithValue("StageName", stageName);
                command.Parameters.AddWithValue("NewOwnerName", newOwnerName);

                int result = (int)await command.ExecuteScalarAsync();

                if (result == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.Created);
                }
                else if (result == 2)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
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
    }
}
