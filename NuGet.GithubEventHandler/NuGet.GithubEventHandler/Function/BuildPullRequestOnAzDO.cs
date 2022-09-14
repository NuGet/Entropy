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

            IReadOnlyList<SubscriptionData>? subscriptions = await GetSubscriptionDataAsync(binder, webhookData.Repo, log);
            if (subscriptions == null)
            {
                return;
            }

            foreach (var subscription in subscriptions)
            {
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

            string? repo = webhookPayload?.Repository?.FullName;
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

            if (repo == null || label == null)
            {
                log.LogInformation("Repo, or label is null. Invalid test data?");
                return null;
            }

            var data = new WebhookData(repo, label, pullRequest.Value);

            return data;
        }

        private async Task<IReadOnlyList<SubscriptionData>?> GetSubscriptionDataAsync(IBinder binder, string repo, ILogger log)
        {
            repo = repo.Replace("/", "__");
            List<SubscriptionData>? subscriptions = null;
            try
            {
                TableClient tableClient = await binder.BindAsync<TableClient>(new TableAttribute(nameof(BuildPullRequestOnAzDO)));
                var filter = "PartitionKey eq '" + repo + "'";

                var results = tableClient.QueryAsync<SubscriptionTableEntry>(filter: filter);
                await foreach (SubscriptionTableEntry subscription in results)
                {
                    if (subscription.Enabled != true)
                    {
                        log.LogInformation("Subscription {0} {1} is not enabled.", subscription.PartitionKey, subscription.RowKey);
                        continue;
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
                        log.LogError($"BuildPullRequest row for {subscription.PartitionKey} {subscription.RowKey} has null values in column(s): " + string.Join(", ", nullColumns));
                        continue;
                    }

                    SubscriptionData subscribed = new SubscriptionData(subscription.Label, subscription.AzDO_Org, subscription.AzDO_Project, subscription.AzDO_Pipeline.Value);
                    if (subscriptions == null)
                    {
                        subscriptions = new List<SubscriptionData>();
                    }
                    subscriptions.Add(subscribed);
                }
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                subscriptions = null;
            }

            if (subscriptions == null)
            {
                log.LogInformation($"Repro {repo} is not subscribed to any builds");
                return null;
            }

            return subscriptions;
        }

        private record WebhookData(string Repo, string Label, int PullRequest);
        private record SubscriptionData(string Label, string Org, string Project, int Pipeline);

        public class SubscriptionTableEntry : ITableEntity
        {
            /// <summary>The GitHub owner and repo separated by double underscore.</summary>
            /// <remarks>For example https://github.com/contoso/sample should use 'contoso__sample' (without quotes).</remarks>
            public string? PartitionKey { get; set; }

            /// <summary>Any arbitrary string</summary>
            /// <remarks>This allows one repo to have multiple build subscriptions</remarks>
            public string? RowKey { get; set; } // repo

            /// <summary>Flag to enable/disable the build subscription.</summary>
            /// <remarks>Allows to build to be skipped without deleting the configuration.</remarks>
            public bool? Enabled { get; set; }

            /// <summary>The label name required to trigger the build.</summary>
            public string? Label { get; set; }

            /// <summary>The Azure DevOps organization where the build will be queued.</summary>
            /// <remarks>For example, if the Azure DevOps URL is https://dev.azure.com/contoso, then the value for this column must be contoso.</remarks>
            public string? AzDO_Org { get; set; }

            /// <summary>The Azure DevOps project where the build will be queued.</summary>
            /// <remarks>For example, if the Azure DevOps URL is https://dev.azure.com/contoso/widgets, then the value for this column must be widgets.</remarks>
            public string? AzDO_Project { get; set; }

            /// <summary>The Azure DevOps build definition ID.</summary>
            /// <remarks>For example, if the Azure DevOps URL is https://dev.azure.com/contoso/widgets/_build?definitionId=1234, then the value for this column must be 1234.</remarks>
            public int? AzDO_Pipeline { get; set; }

            // These properties are generated and should not be created when inserting/editing a row in the table.
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
        }
    }
}
