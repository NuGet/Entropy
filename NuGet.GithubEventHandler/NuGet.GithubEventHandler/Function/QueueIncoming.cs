using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NuGet.GithubEventHandler.Model.GitHub;

namespace NuGet.GithubEventHandler.Function
{
    public class QueueIncoming
    {
        private readonly IReadOnlyList<Config> _config;

        private static readonly IReadOnlyList<Config> Default = new List<Config>()
        {
            new Config(BuildPullRequestOnAzDO.ShouldQueue, BuildPullRequestOnAzDO.QueueName)
        };

        public QueueIncoming(IReadOnlyList<Config> config)
        {
            _config = config;
        }

        [FunctionName(nameof(QueueIncoming))]
        public async Task RunAsync([BlobTrigger("webhooks/incoming/{name}")] Stream myBlob, string name, ILogger log, IBinder binder)
        {
            log.LogInformation($"{nameof(QueueIncoming)} processing {name}");
            string blobPath = "webhooks/incoming/" + name;

            WebhookPayload? payload;
            try
            {
                payload = await JsonSerializer.DeserializeAsync<WebhookPayload>(myBlob);
            }
            catch (JsonException)
            {
                payload = null;
            }

            if (payload == null)
            {
                log.LogError("Unable to deserialize " + blobPath);
                return;
            }

            foreach (var function in _config)
            {
                if (function.Predicate(payload))
                {
                    var queueClient = await binder.BindAsync<QueueClient>(new QueueAttribute(function.QueueName));
                    await queueClient.SendMessageAsync(blobPath);
                    log.LogInformation("Queued on " + function.QueueName);
                }
            }
        }

        public record Config(Func<WebhookPayload, bool> Predicate, string QueueName);
    }
}
