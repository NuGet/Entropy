using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NuGet.GithubEventHandler.Model.GitHub;

namespace NuGet.GithubEventHandler.Function
{
    public class BuildPullRequestOnAzDO
    {
        internal const string QueueName = "pull-request-build-azure-pipelines";

        private readonly IAzDOClient _azdoClient;

        public BuildPullRequestOnAzDO(IAzDOClient azdoClient)
        {
            _azdoClient = azdoClient;
        }

        public static bool ShouldQueue(WebhookPayload webhook)
        {
            return webhook?.Action == "labeled"
                && webhook?.PullRequest != null;
        }

        [FunctionName(nameof(BuildPullRequestOnAzDO))]
        public async Task Run([QueueTrigger("pull-request-build-azure-pipelines")]string blobPath,
            ILogger log, IBinder binder)
        {
            log.LogInformation($"BuildPullRequest processing: {blobPath}");

            // Get webhook payload, and validate expected inputs
            WebhookData? webhookData = await GetWebhookDataAsync(binder, blobPath, log);
            if (webhookData == null)
            {
                return;
            }

            SubscriptionData? subscription = await GetSubscriptionDataAsync(binder, webhookData.Owner, webhookData.Repo, log);
            if (subscription == null)
            {
                return;
            }

            if (string.Equals(subscription.Label, webhookData.Label, StringComparison.Ordinal))
            {
                string gitRef = "refs/pull/" + webhookData.PullRequest + "/head";
                string url = await _azdoClient.QueuePipeline(subscription.Org, subscription.Project, subscription.Pipeline, gitRef);
                log.LogInformation("Queued build " + url);
            }
            else
            {
                log.LogInformation($"Webhook label '{webhookData.Label}' does not match subscription label '{subscription.Label}'");
            }
        }

        private async Task<WebhookData?> GetWebhookDataAsync(IBinder binder, string blobPath, ILogger log)
        {
            WebhookPayload? webhookPayload;
            using (var stream = await binder.BindAsync<Stream?>(new BlobAttribute(blobPath, FileAccess.Read)))
            {
                if (stream == null)
                {
                    log.LogError("blob does not exist and therefore should not have been queued");
                    return null;
                }
                webhookPayload = JsonSerializer.Deserialize<WebhookPayload>(stream);
            }

            string? owner = webhookPayload?.Repository?.Owner?.Login;
            string? repo = webhookPayload?.Repository?.Name;
            string? label = webhookPayload?.Label?.Name;
            int? pullRequest = webhookPayload?.PullRequest?.Number;

            if (webhookPayload == null)
            {
                log.LogError("WebhookPayload could not be read or deserialized.");
                return null;
            }

            if (!string.Equals("labeled", webhookPayload?.Action))
            {
                log.LogError("Webhook action is not 'labeled'. This webhook payload should not have been added to this queue.");
                return null;
            }

            if (pullRequest == null)
            {
                log.LogError("Webhook is not from a pull request. This webhook payload should not have been added to this queue.");
                return null;
            }

            if (owner == null || repo == null || label == null)
            {
                log.LogInformation("Owner, repo, or label is null. Invalid test data?");
                return null;
            }

            var data = new WebhookData(owner, repo, label, pullRequest.Value);

            return data;
        }

        private async Task<SubscriptionData?> GetSubscriptionDataAsync(IBinder binder, string owner, string repo, ILogger log)
        {
            SubscriptionTableEntry? subscription;
            try
            {
                TableClient tableClient = await binder.BindAsync<TableClient>(new TableAttribute(nameof(BuildPullRequestOnAzDO)));
                subscription = await tableClient.GetEntityAsync<SubscriptionTableEntry>(owner, repo);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                subscription = null;
            }

            if (subscription == null)
            {
                log.LogInformation($"Repro {owner}/{repo} is not subscribed to any builds");
                return null;
            }

            if (subscription.Label == null
                || subscription.AzDO_Org == null
                || subscription.AzDO_Project == null
                || subscription.AzDO_Pipeline == null)
            {
                List<string>? nullColumns = new();
                if (subscription.Label == null) { nullColumns.Add(nameof(subscription.Label)); }
                if (subscription.AzDO_Org == null) { nullColumns.Add(nameof(subscription.AzDO_Org)); }
                if (subscription.AzDO_Project == null) { nullColumns.Add(nameof(subscription.AzDO_Project)); }
                if (subscription.AzDO_Pipeline == null) { nullColumns.Add(nameof(subscription.AzDO_Pipeline)); }
                log.LogError($"BuildPullRequest row for {owner}/{repo} has null values in column(s): " + string.Join(", ", nullColumns));
                return null;
            }

            var subscriptionData = new SubscriptionData(subscription.Label, subscription.AzDO_Org, subscription.AzDO_Project, subscription.AzDO_Pipeline.Value);
            return subscriptionData;
        }

        private record WebhookData(string Owner, string Repo, string Label, int PullRequest);
        private record SubscriptionData(string Label, string Org, string Project, int Pipeline);

        public class SubscriptionTableEntry : ITableEntity
        {
            public string? PartitionKey { get; set; } // owner
            public string? RowKey { get; set; } // repo
            public string? Label { get; set; }
            public string? AzDO_Org { get; set; }
            public string? AzDO_Project { get; set; }
            public int? AzDO_Pipeline { get; set; }

            // These properties are generated and should not be created when inserting/editing a row in the table.
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
        }
    }
}
