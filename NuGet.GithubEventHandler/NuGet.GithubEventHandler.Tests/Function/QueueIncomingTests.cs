using Azure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.GithubEventHandler.Function;
using NuGet.GithubEventHandler.Model.GitHub;
using System.Text;
using System.Text.Json;

namespace NuGet.GithubEventHandler.Tests.Function
{
    public class QueueIncomingTests
    {
        [Fact]
        public async Task SuccessfullyQueue()
        {
            // Arrange
            using var memoryStream = new MemoryStream();
            JsonSerializer.Serialize(memoryStream, new WebhookPayload()
            {
                Action = "test"
            });
            memoryStream.Position = 0;

            var log = new Mock<ILogger>();

            var queue = new Mock<QueueClient>();

            string queueName = nameof(queueName);

            var binder = new Mock<IBinder>(MockBehavior.Strict);
            binder.Setup(b => b.BindAsync<QueueClient>(It.Is<QueueAttribute>(a => a.QueueName == queueName), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(queue.Object));

            var config = new List<QueueIncoming.Config>()
            {
                new QueueIncoming.Config(wh => wh.Action == "test", queueName)
            };
            var target = new QueueIncoming(config);

            // Act
            await target.RunAsync(memoryStream, "path/to/json", log.Object, binder.Object);

            // Assert
            queue.Verify(q => q.SendMessageAsync("webhooks/incoming/path/to/json"), Times.Once());
        }

        [Fact]
        public async Task NoQueueMessagesWhenNoPredicatesMatch()
        {
            // Arrange
            var log = new Mock<ILogger>();
            var binder = new Mock<IBinder>(MockBehavior.Strict);

            string queueName = nameof(queueName);
            var config = new List<QueueIncoming.Config>()
            {
                new QueueIncoming.Config(_ => false, queueName)
            };
            var target = new QueueIncoming(config);

            // Act
            await target.RunAsync(new MemoryStream(Encoding.UTF8.GetBytes("{}")), "path/to/json", log.Object, binder.Object);

            // Assert
            Assert.Equal(0, binder.Invocations.Count);
        }

        [Theory]
        [InlineData("")]
        [InlineData("{")]
        public async Task EarlyExitWhenBlobNotDeserializable(string contents)
        {
            // Arrange
            var log = new Mock<ILogger>();
            var binder = new Mock<IBinder>(MockBehavior.Strict);
            var config = new Mock<IReadOnlyList<QueueIncoming.Config>>(MockBehavior.Strict);

            Stream? stream;
            if (string.IsNullOrEmpty(contents))
            {
                stream = new MemoryStream();
            }
            else
            {
                stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));
            }

            var target = new QueueIncoming(config.Object);

            // Act
            await target.RunAsync(stream, "path/to/json", log.Object, binder.Object);

            // Assert
            Assert.Equal(0, binder.Invocations.Count);
        }
    }
}
