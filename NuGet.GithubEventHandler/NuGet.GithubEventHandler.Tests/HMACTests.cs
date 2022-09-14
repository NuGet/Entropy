using Moq;

namespace NuGet.GithubEventHandler.Tests
{
    public class HMACTests
    {
        [Fact]
        public void ReturnsTrueForCorrectHMAC()
        {
            // Arrange
            var config = new MockConfiguration();

            // Arrange & Act
            var actual = Validate(config);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public void ReturnsFalseForWhenUnknownSecretName()
        {
            // Arrange
            var config = new MockConfiguration();
            config.SecretValue = null;

            // Act
            var actual = Validate(config);

            // Assert
            Assert.False(actual);
        }

        [Fact]
        public void ReturnsFalseForWrongHash()
        {
            // Arrange
            var config = new MockConfiguration();
            config.Signature[0] = (byte)~config.Signature[0];

            // Act
            var actual = Validate(config);

            // Assert
            Assert.False(actual);
        }

        private bool Validate(MockConfiguration config)
        {
            string environmentVariableName = "WEBHOOK_SECRET_" + config.SecretName;
            Mock<IEnvironment> env = new(MockBehavior.Strict);
            env.Setup(e => e.Get(environmentVariableName))
                .Returns(config.SecretValue);

            var result = HMAC.Validate(config.Signature, new MemoryStream(config.StreamData), config.SecretName, env.Object);
            return result;
        }

        private class MockConfiguration
        {
            public MockConfiguration()
            {
                StreamData = Enumerable.Range(0, 256).Select(v => (byte)v).ToArray();
                SecretName = "name";
                SecretValue = "secret";
                Signature = new byte[]
                {
                    0x05, 0x1e, 0xcd, 0x1b, 0xee, 0xb2, 0x2b, 0xbd, 0x4b, 0x9c, 0x78, 0x99, 0xa8, 0x42, 0x5a, 0xd7,
                    0x69, 0xbd, 0x34, 0x2e, 0x9c, 0x42, 0x0e, 0x00, 0x46, 0x24, 0x41, 0x7f, 0x37, 0xdb, 0xdf, 0x9e
                };
            }

            public byte[] StreamData { get; set; }
            public string SecretName { get; set; }
            public string? SecretValue { get; set; }
            public byte[] Signature { get; set; }
        }
    }
}