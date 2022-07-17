using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.GithubEventHandler.Function;
using NuGet.GithubEventHandler.Model.GitHub;
using System.Text.Json;

namespace NuGet.GithubEventHandler.Tests.Function
{
    public class BuildPullRequestOnAzDOTest
    {
        [Fact]
        public async Task BuildQueued()
        {
            // Arrange
            var config = new TestConfig();

            // Act
            await Run(config);

            // Assert
            config.Client.Verify(c => c.QueuePipeline(config.Subscription.AzDO_Org, config.Subscription.AzDO_Project, config.Subscription.AzDO_Pipeline.Value, $"refs/pull/{config.BlobData.PullRequest.Number}/head"), Times.Once);
            config.Log.Verify(l => l.Log<object>(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.AtLeastOnce);
            config.Log.Verify(l => l.Log<object>(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Never);
        }

        [Fact]
        public async Task ErrorLoggedWhenBlobNotFound()
        {
            // Arrange
            var config = new TestConfig()
            {
                BlobData = null
            };

            // Act
            await Run(config);

            // Assert
            config.Client.Verify(c => c.QueuePipeline(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            config.Log.Verify(l => l.Log<object>(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task BuildNotQueedWhenNoSubscription()
        {
            // Arrange
            var config = new TestConfig()
            {
                Subscription = null
            };

            // Act
            await Run(config);

            // Assert
            config.Client.Verify(c => c.QueuePipeline(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            config.Log.Verify(l => l.Log<object>(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.AtLeastOnce);
        }

        private static async Task Run(TestConfig config)
        {
            var target = new BuildPullRequestOnAzDO(config.Client.Object);

            var binder = new Mock<IBinder>();
            if (config.BlobData != null)
            {
                binder.Setup(b => b.BindAsync<Stream?>(It.IsAny<BlobAttribute>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        var memoryStream = new MemoryStream();
                        JsonSerializer.Serialize(memoryStream, config.BlobData);
                        memoryStream.Position = 0;
                        return Task.FromResult<Stream?>(memoryStream);
                    });
            }
            else
            {
                binder.Setup(b => b.BindAsync<Stream?>(It.IsAny<BlobAttribute>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<Stream?>(null));
            }

            var tableClient = new Mock<TableClient>(MockBehavior.Strict);

            binder.Setup(b => b.BindAsync<TableClient>(It.IsAny<TableAttribute>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(tableClient.Object));

            if (config.Subscription?.PartitionKey != null && config.Subscription?.RowKey != null)
            {
                Response<BuildPullRequestOnAzDO.SubscriptionTableEntry> response = Response.FromValue(config.Subscription, Mock.Of<Response>());
                tableClient.Setup(tc => tc.GetEntityAsync<BuildPullRequestOnAzDO.SubscriptionTableEntry>(config.Subscription.PartitionKey, config.Subscription.RowKey, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
            }
            else
            {
                tableClient.Setup(tc => tc.GetEntityAsync<BuildPullRequestOnAzDO.SubscriptionTableEntry>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                    .Throws(new RequestFailedException(404, "not found"));
            }

            await target.Run(config.QueueMessage, config.Log.Object, binder.Object);
        }

        private class TestConfig
        {
            public TestConfig()
            {
                Client = new Mock<IAzDOClient>();
                Log = new Mock<ILogger>();
                QueueMessage = "webhooks/incoming/2022-07-17/data.json";

                BlobData = new WebhookPayload()
                {
                    Action = "labeled",
                    Label = new Label()
                    {
                        Name = "Approved for CI"
                    },
                    PullRequest = new PullRequest()
                    {
                        Number = 1
                    },
                    Repository = new Repository()
                    {
                        Owner = new User()
                        {
                            Login = "NuGet"
                        },
                        Name = "NuGet.Client",
                        FullName = "NuGet/NuGet.Client"
                    }
                };

                Subscription = new BuildPullRequestOnAzDO.SubscriptionTableEntry()
                {
                    PartitionKey = "NuGet",
                    RowKey = "NuGet.Client",
                    AzDO_Org = "DevDiv",
                    AzDO_Project = "DevDiv",
                    AzDO_Pipeline = 1234,
                    Label = "Approved for CI"
                };
            }

            public Mock<IAzDOClient> Client { get; init; }
            public Mock<ILogger> Log { get; init; }
            public string QueueMessage { get; init; }
            public WebhookPayload? BlobData { get; init; }
            public BuildPullRequestOnAzDO.SubscriptionTableEntry? Subscription { get; init; }
        }
    }
}
