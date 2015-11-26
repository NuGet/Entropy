using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Gallery.Staging.Web.Code.Api
{
    public class StageClient
    {
        private readonly string _connectionString;
        private readonly string _baseServiceUrl;
        private readonly string _baseApiUrl;
        private readonly HttpClient _httpClient;

        public StageClient(string connectionString)
        {
            _connectionString = connectionString;
            _baseServiceUrl = ConfigurationManager.AppSettings["Stage.BaseServiceUrl"];
            _baseApiUrl = ConfigurationManager.AppSettings["Stage.BaseApiUrl"];

            _httpClient = new HttpClient();
        }

        private Uri CreateApiUri(string path)
        {
            return new Uri(_baseApiUrl.TrimEnd('/') + "/" + path.TrimStart('/'));
        }

        public async Task<string> GetApiKey(string ownerName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SqlCommand("SELECT [ApiKey] FROM [dbo].[Owner] WHERE [Name] = @OwnerName", connection);
                command.Parameters.AddWithValue("@OwnerName", ownerName);

                var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return reader["ApiKey"].ToString();
                }
            }

            return null;
        }

        public async Task<bool> EnsureOwnerCreated(string ownerName)
        {
            var response = await _httpClient.PostAsync(CreateApiUri("/create/owner"), new StringContent(new JObject(
                new JProperty("ownerName", ownerName)).ToString()));

            return response.IsSuccessStatusCode;
        }

        public async Task<List<Stage>> List(string ownerName)
        {
            var response = await _httpClient.GetAsync(
                CreateApiUri(string.Format("/stage/{0}", ownerName)));

            if (response.IsSuccessStatusCode)
            {
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                var stages = (JArray)json["stages"];

                return stages
                    .Select(s => s.ToObject<Stage>())
                    .ToList();
            }

            return new List<Stage>();
        }

        public async Task<bool> Create(string ownerName, string stageName)
        {
            var response = await _httpClient.PostAsync(CreateApiUri("/create/stage"), new StringContent(new JObject(
                new JProperty("ownerName", ownerName),
                new JProperty("stageName", stageName),
                new JProperty("baseService", _baseServiceUrl)).ToString()));

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> Exists(string ownerName, string stageName)
        {
            var response = await _httpClient.GetAsync(
                CreateApiUri(string.Format("/stage/{0}/{1}", ownerName, stageName)));

            return response.IsSuccessStatusCode;
        }

        public async Task<Stage> Get(string ownerName, string stageName)
        {
            var response = await _httpClient.GetAsync(
                CreateApiUri(string.Format("/stage/{0}/{1}", ownerName, stageName)));

            if (response.IsSuccessStatusCode)
            {
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                return json.ToObject<Stage>();
            }

            return null;
        }

        public async Task<bool> Delete(string ownerName, string stageName)
        {
            var response = await _httpClient.DeleteAsync(
                CreateApiUri(string.Format("/stage/{0}/{1}", ownerName, stageName)));

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UploadPackage(string ownerName, string stageName, Stream packageStream)
        {
            var apiKey = await GetApiKey(ownerName);
            var pushUrl = CreateApiUri(string.Format("/push/package/{0}/{1}", ownerName, stageName));

            using (var multipartFormDataContent = new MultipartFormDataContent())
            {
                multipartFormDataContent.Headers.Add("X-NuGet-ApiKey", apiKey);
                multipartFormDataContent.Add(new StreamContent(packageStream));

                var response = await _httpClient.PutAsync(pushUrl, multipartFormDataContent);

                return response.IsSuccessStatusCode;
            }
        }

        public async Task<bool> DeletePackage(string ownerName, string stageName, string packageId)
        {
            var response = await _httpClient.DeleteAsync(
                CreateApiUri(string.Format("/stage/{0}/{1}/{2}", ownerName, stageName, packageId)));

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeletePackageVersion(string ownerName, string stageName, string packageId, string packageVersion)
        {
            var response = await _httpClient.DeleteAsync(
                CreateApiUri(string.Format("/stage/{0}/{1}/{2}/{3}", ownerName, stageName, packageId, packageVersion)));

            return response.IsSuccessStatusCode;
        }
    }
}