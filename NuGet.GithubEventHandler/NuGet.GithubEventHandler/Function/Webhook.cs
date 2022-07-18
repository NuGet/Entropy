using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Logging;

namespace NuGet.GithubEventHandler.Function
{
    /// <summary>
    /// GitHub webhook entry point.
    /// <para>Checks HMAC security, then saves body to blob storage.</para>
    /// </summary>
    public class Webhook
    {
        private const string SignaturePrefix = "sha256=";
        private IEnvironment _environment;

        public Webhook(IEnvironment environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        [FunctionName("Webhook")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hooks/{name:alpha}")] HttpRequest req,
            string name,
            ILogger log,
            IBinder binder) // https://docs.microsoft.com/azure/azure-functions/functions-dotnet-class-library#binding-at-runtime
        {
            string? signatureHeader = req.Headers["X-Hub-Signature-256"].SingleOrDefault();
            string? deliveryId = req.Headers["X-GitHub-Delivery"].SingleOrDefault();
            if (signatureHeader is null || deliveryId is null)
            {
                log.LogInformation("Request did not contain expected headers");
                return new BadRequestObjectResult("Expected X-Hub-Signature-256 and X-GitHub-Delivery headers");
            }

            byte[]? signature = ParseSignature(signatureHeader);
            if (signature == null)
            {
                log.LogInformation("X-Hub-Signature-256 header does not contain expected format");
                return new BadRequestObjectResult("X-Hub-Signature-256 header does not contain expected format");
            }

            // HttpRequest already buffered the HTTP request. Do not read req.Body into an array, to reduce allocations and therefore cost.
            // Azure Functions consumption plan costs = memory usage * duration, and webhook bodies are 10's of KB UTF8 encoded.
            if (!HMAC.Validate(signature, req.Body, name, _environment))
            {
                log.LogInformation("HMAC validation failed");
                return new ForbidResult();
            }

            // validation read stream, so need to reset to beginning of stream
            var blobPath = $"webhooks/incoming/{DateTime.UtcNow:yyy-MM-dd}/{deliveryId}.json";
            log.LogInformation("Saving HTTP request body to " + blobPath);
            req.Body.Position = 0;

            using (Stream blobStream = await binder.BindAsync<Stream>(new BlobAttribute(blobPath, FileAccess.Write)))
            {
                await req.Body.CopyToAsync(blobStream);
            }

            return new OkResult();
        }

        /// <summary>Parse GitHub's X-Hub-Signature-256 header.</summary>
        /// <param name="signatureHeader">The HTTP header value, including the 'sha2256=' prefix.</param>
        /// <returns>A byte array that represents the SHA256 hash.</returns>
        private byte[]? ParseSignature(string signatureHeader)
        {
            if (signatureHeader.Length != 64 + SignaturePrefix.Length)
            {
                return null;
            }

            try
            {
                byte[] signature = new byte[32];
                for (int i =0; i < signature.Length; i++)
                {
                    int offset = SignaturePrefix.Length + i * 2;
                    byte upper = FromHex(signatureHeader[offset]);
                    byte lower = FromHex(signatureHeader[offset + 1]);
                    int value = (upper << 4) | lower;
                    signature[i] = (byte)value;
                }
                return signature;
            }
            catch
            {
                return null;
            }
        }

        private static byte FromHex(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return (byte)(c - '0');
            }
            
            if (c >= 'A' && c <= 'F')
            {
                c = (char)(c - 'A' + 'a');
            }

            if (c >= 'a' && c <= 'f')
            {
                return (byte)(c - 'a' + 10);
            }

            throw new ArgumentException();
        }
    }
}
