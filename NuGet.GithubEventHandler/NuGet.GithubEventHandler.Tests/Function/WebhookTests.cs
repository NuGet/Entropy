using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Primitives;
using Moq;
using NuGet.GithubEventHandler.Function;

namespace NuGet.GithubEventHandler.Tests.Function
{
    public class WebhookTests
    {
        [Fact]
        public async Task SuccessfulResponseAndBlobSaved()
        {
            // Arrange
            var config = new TestConfig();

            // Act
            var actual = await Run(config);

            // Assert
            var statusResultObject = Assert.IsAssignableFrom<StatusCodeResult>(actual.ActionResult);
            Assert.InRange(statusResultObject.StatusCode, 200, 299);

            var expectedBlobPath = $"webhooks/incoming/{DateTime.UtcNow:yyyy-MM-dd}/{config.Delivery}.json";
            Assert.Equal(expectedBlobPath, actual.BlobPath);

            Assert.Equal(config.Content, actual.BlobData);
        }

        [Fact]
        public async Task NoSecretReturnsError()
        {
            // Arrange
            var config = new TestConfig();
            config.Secret = null;

            // Act
            var actual = await Run(config);

            // Assert
            Assert.IsType<ForbidResult>(actual.ActionResult);
            Assert.Null(actual.BlobPath);
        }

        [Fact]
        public async Task NoDeliveryIdReturnsError()
        {
            // Arrange
            var config = new TestConfig();
            config.Delivery = null;

            // Act
            var actual = await Run(config);

            // Assert
            var statusResultObject = Assert.IsAssignableFrom<ObjectResult>(actual.ActionResult);
            Assert.Equal(StatusCodes.Status400BadRequest, statusResultObject.StatusCode);
            Assert.Null(actual.BlobPath);
        }

        [Fact]
        public async Task NoSignatureIdReturnsError()
        {
            // Arrange
            var config = new TestConfig();
            config.Signature = null;

            // Act
            var actual = await Run(config);

            // Assert
            var statusResultObject = Assert.IsAssignableFrom<ObjectResult>(actual.ActionResult);
            Assert.Equal(StatusCodes.Status400BadRequest, statusResultObject.StatusCode);
            Assert.Null(actual.BlobPath);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SignatureWrongLengthReturnsError(bool shorter)
        {
            // Arrange
            var config = new TestConfig();
            if (shorter)
            {
                config.Signature = config.Signature?.Substring(0, config.Signature.Length - 1);
            }
            else
            {
                config.Signature += "00";
            }

            // Act
            var actual = await Run(config);

            // Assert
            var statusResultObject = Assert.IsAssignableFrom<ObjectResult>(actual.ActionResult);
            Assert.Equal(StatusCodes.Status400BadRequest, statusResultObject.StatusCode);
            Assert.Null(actual.BlobPath);
        }

        [Fact]
        public async Task SignatureContainsNonHexCharacterReturnsError()
        {
            // Arrange
            var config = new TestConfig();
            config.Signature = config.Signature?.Replace('0', 'Z');

            // Act
            var actual = await Run(config);

            // Assert
            var statusResultObject = Assert.IsAssignableFrom<ObjectResult>(actual.ActionResult);
            Assert.Equal(StatusCodes.Status400BadRequest, statusResultObject.StatusCode);
            Assert.Null(actual.BlobPath);
        }

        [Fact]
        public async Task WrongSignatureReturnsForbiden()
        {
            // Arrange
            var config = new TestConfig();
            config.Signature = config.Signature?.Replace('0', '1');

            // Act
            var actual = await Run(config);

            // Assert
            Assert.IsType<ForbidResult>(actual.ActionResult);
            Assert.Null(actual.BlobPath);
        }

        private async Task<Result> Run(TestConfig config)
        {
            string name = nameof(name);
            Mock<IEnvironment> env = new Mock<IEnvironment>(MockBehavior.Strict);
            env.Setup(e => e.Get("WEBHOOK_SECRET_" + name))
                .Returns(config.Secret);
            Webhook target = new Webhook(env.Object);

            HeaderDictionary headers = new HeaderDictionary();
            if (config.Signature != null)
            {
                headers.Add("X-Hub-Signature-256", new StringValues(config.Signature));
            }
            if (config.Delivery != null)
            {
                headers.Add("X-GitHub-Delivery", new StringValues(config.Delivery));
            }

            Mock<HttpRequest> req = new Mock<HttpRequest>(MockBehavior.Strict);
            req.SetupGet(r => r.Headers)
                .Returns(headers);
            req.SetupGet(r => r.Body)
                .Returns(new MemoryStream(config.Content));

            string? blobPath = null;
            using (var blobData = new MemoryStream())
            {

                Mock<IBinder> binder = new Mock<IBinder>(MockBehavior.Strict);
                binder.Setup(b =>b.BindAsync<Stream>(It.IsAny<BlobAttribute>(), It.IsAny<CancellationToken>()))
                    .Callback<Attribute, CancellationToken>((attribute, _) =>
                    {
                        var blobAttribute = (BlobAttribute)attribute;
                        blobPath = blobAttribute.BlobPath;
                    })
                    .Returns(Task.FromResult<Stream>(blobData));

                IActionResult actual = await target.Run(req.Object, name, binder.Object);

                byte[]? data = blobData.ToArray();
                var result = new Result(actual, blobPath, data);
                return result;
            }
        }

        private class Result
        {
            public Result(IActionResult actionResult, string? blobPath, byte[]? blobData)
            {
                ActionResult = actionResult;
                BlobPath = blobPath;
                BlobData = blobData;
            }

            public IActionResult ActionResult { get; }
            public string? BlobPath { get; }
            public byte[]? BlobData { get; }
        }

        private class TestConfig
        {
            public TestConfig()
            {
                Secret = "secret";
                Signature = "sha256=051ecd1beeb22bbd4b9c7899a8425ad769bd342e9c420e004624417f37dbdf9e";
                Delivery = "b49cb89f-f3cd-4442-a744-1fb6ddc35bbb";
                Content = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
            }

            public string? Secret { get; set; }
            public string? Signature { get; set; }
            public string? Delivery { get; set; }
            public byte[] Content { get; set; }
        }
    }
}
