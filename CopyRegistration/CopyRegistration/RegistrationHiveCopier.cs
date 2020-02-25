using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Catalog;
using NuGet.Protocol.Registration;
using NuGetGallery;

namespace CopyRegistration
{
    public class RegistrationHiveCopier
    {
        private static SemaphoreSlim Throttle = new SemaphoreSlim(64);

        private readonly IRegistrationClient _registrationClient;
        private readonly ISimpleHttpClient _simpleHttpClient;
        private readonly ILogger<RegistrationHiveCopier> _logger;

        public RegistrationHiveCopier(
            IRegistrationClient registrationClient,
            ISimpleHttpClient simpleHttpClient,
            ILogger<RegistrationHiveCopier> logger)
        {
            _registrationClient = registrationClient;
            _simpleHttpClient = simpleHttpClient;
            _logger = logger;
        }

        public async Task CopyAsync(ICloudBlobContainer container, string oldBaseUrl, string newBaseUrl, string id, bool gzip)
        {
            var normalizedOldBaseUrl = oldBaseUrl.TrimEnd('/');
            var normalizedNewBaseUrl = newBaseUrl.TrimEnd('/');

            var indexUrl = RegistrationUrlBuilder.GetIndexUrl(normalizedOldBaseUrl, id);
            _logger.LogInformation("Downloading index {IndexUrl}...", indexUrl);

            await Throttle.WaitAsync();
            RegistrationIndex index;
            try
            {
                index = await _registrationClient.GetIndexOrNullAsync(indexUrl);
            }
            finally
            {
                Throttle.Release();
            }

            if (index == null)
            {
                return;
            }

            var pageUrls = new List<string>();
            var itemUrls = new List<string>();

            var pageTasks = index
                .Items
                .Select(async pageItem =>
                {
                    await Task.Yield();
                    await Throttle.WaitAsync();
                    try
                    {
                        List<RegistrationLeafItem> leafItems;
                        if (pageItem.Items != null)
                        {
                            leafItems = pageItem.Items;
                        }
                        else
                        {
                            pageUrls.Add(pageItem.Url);
                            _logger.LogInformation("Downloading page {PageUrl}...", pageItem.Url);
                            var page = await _registrationClient.GetPageAsync(pageItem.Url);
                            leafItems = page.Items;
                        }

                        foreach (var leafItem in leafItems)
                        {
                            itemUrls.Add(leafItem.Url);
                        }
                    }
                    finally
                    {
                        Throttle.Release();
                    }
                })
                .ToList();
            await Task.WhenAll(pageTasks);

            var copyTasks = itemUrls
                .Concat(pageUrls)
                .Concat(new[] { indexUrl })
                .Select(async url =>
                {
                    await Task.Yield();
                    await CopyUrlAsync(container, normalizedOldBaseUrl, normalizedNewBaseUrl, url, gzip);
                })
                .ToList();
            await Task.WhenAll(copyTasks);
        }

        private async Task CopyUrlAsync(ICloudBlobContainer container, string oldBaseUrl, string newBaseUrl, string oldUrl, bool gzip)
        {
            await Throttle.WaitAsync();
            try
            {
                _logger.LogInformation("Copying {OldUrl}...", oldUrl);

                var result = await _simpleHttpClient.DeserializeUrlAsync<JToken>(oldUrl);
                var json = result.GetResultOrThrow();
                var fixedJson = FixUrls(oldBaseUrl, newBaseUrl, json);

                if (!TryGetPath(oldBaseUrl, oldUrl, out var path))
                {
                    throw new InvalidOperationException("The URL does not start with the base URL.");
                }

                var blob = container.GetBlobReference(path);

                blob.Properties.ContentType = "application/json";
                var jsonString = fixedJson.ToString(Formatting.None);
                var bytes = Encoding.UTF8.GetBytes(jsonString);

                if (gzip)
                {
                    blob.Properties.ContentEncoding = "gzip";
                    using (var compressedStream = new MemoryStream())
                    {
                        using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
                        {
                            gzipStream.Write(bytes, 0, bytes.Length);
                        }

                        bytes = compressedStream.ToArray();
                    }
                }

                using (var memoryStream = new MemoryStream(bytes))
                {
                    await blob.UploadFromStreamAsync(memoryStream, overwrite: true);
                }
            }
            finally
            {
                Throttle.Release();
            }
        }

        private JToken FixUrls(string oldBaseUrl, string newBaseUrl, JToken token)
        {
            switch (token)
            {
                case JArray array:
                    for (var i = 0; i < array.Count; i++)
                    {
                        array[i] = FixUrls(oldBaseUrl, newBaseUrl, array[i]);
                    }
                    return array;
                case JObject obj:
                    foreach (var prop in obj.Properties())
                    {
                        obj[prop.Name] = FixUrls(oldBaseUrl, newBaseUrl, prop.Value);
                    }
                    return obj;
                case JValue value:
                    switch (value.Value)
                    {
                        case string str:
                            return new JValue(ChangeBaseUrl(oldBaseUrl, newBaseUrl, str, out var _));
                        default:
                            return value;
                    }
                default:
                    return token;
            }
        }

        private bool TryGetPath(string oldBaseUrl, string oldUrl, out string path)
        {
            path = null;

            if (!Uri.TryCreate(oldUrl, UriKind.Absolute, out var _))
            {
                return false;
            }

            if (!oldUrl.StartsWith(oldBaseUrl))
            {
                return false;
            }

            path = oldUrl.Substring((oldBaseUrl + '/').Length);
            return true;
        }

        private string ChangeBaseUrl(string oldBaseUrl, string newBaseUrl, string oldUrl, out string path)
        {
            if (TryGetPath(oldBaseUrl, oldUrl, out path))
            {
                return $"{newBaseUrl.TrimEnd('/')}/{path}";
            }
            else
            {
                return oldUrl;
            }
        }
    }


}
