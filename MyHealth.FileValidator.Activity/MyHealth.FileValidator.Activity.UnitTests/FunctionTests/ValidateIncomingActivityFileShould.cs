using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyHealth.Common;
using MyHealth.FileValidator.Activity.Functions;
using MyHealth.FileValidator.Activity.Parsers;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MyHealth.FileValidator.Activity.UnitTests.FunctionTests
{
    public class ValidateIncomingActivityFileShould
    {
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IAzureBlobHelpers> _mockAzureBlobHelpers;
        private Mock<IActivityRecordParser> _mockActivityRecordParser;
        private Mock<ITableHelpers> _mockTableHelpers;
        private Mock<Stream> _mockStream;
        private Mock<ILogger> _mockLogger;
        private ValidateIncomingActivityFile _func;

        public ValidateIncomingActivityFileShould()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockAzureBlobHelpers = new Mock<IAzureBlobHelpers>();
            _mockActivityRecordParser = new Mock<IActivityRecordParser>();
            _mockTableHelpers = new Mock<ITableHelpers>();
            _mockStream = new Mock<Stream>();
            _mockLogger = new Mock<ILogger>();

            _func = new ValidateIncomingActivityFile(
                _mockConfiguration.Object,
                _mockAzureBlobHelpers.Object,
                _mockActivityRecordParser.Object,
                _mockTableHelpers.Object);
        }

        [Fact]
        public async Task CatchAndLogExceptionWhenDownloadBlobAsStreamAsyncThrowsException()
        {
            // Arrange
            _mockTableHelpers.Setup(x => x.IsDuplicateAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            _mockAzureBlobHelpers.Setup(x => x.DownloadBlobAsStreamAsync(It.IsAny<string>())).ThrowsAsync(It.IsAny<Exception>());

            // Act
            Func<Task> functionAction = async () => await _func.Run(_mockStream.Object, "TestFileName", _mockLogger.Object);

            // Assert
            await functionAction.Should().ThrowAsync<Exception>(It.IsAny<string>());
        }

        [Fact]
        public async Task CatchAndLogExceptionWhenParseActivityStreamThrowsException()
        {
            // Arrange
            _mockActivityRecordParser.Setup(x => x.ParseActivityStream(It.IsAny<Stream>())).ThrowsAsync(It.IsAny<Exception>());

            // Act
            Func<Task> functionAction = async () => await _func.Run(_mockStream.Object, "TestFileName", _mockLogger.Object);

            // Assert
            await functionAction.Should().ThrowAsync<Exception>(It.IsAny<string>());
        }

        [Fact]
        public async Task CatchAndLogExceptionWhenIsDuplicateAsyncThrowsException()
        {
            // Arrage
            _mockTableHelpers.Setup(x => x.IsDuplicateAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(It.IsAny<Exception>());

            // Act
            Func<Task> functionAction = async () => await _func.Run(_mockStream.Object, "TestFileName", _mockLogger.Object);

            // Assert
            await functionAction.Should().ThrowAsync<Exception>(It.IsAny<string>());
        }

        [Fact]
        public async Task SuccessfullyProcessFile()
        {
            // Arrange
            _mockAzureBlobHelpers.Setup(x => x.DownloadBlobAsStreamAsync(It.IsAny<string>())).ReturnsAsync(It.IsAny<Stream>()).Verifiable();
            _mockActivityRecordParser.Setup(x => x.ParseActivityStream(It.IsAny<Stream>())).Returns(Task.CompletedTask).Verifiable();

            // Act
            Func<Task> functionAction = async () => await _func.Run(_mockStream.Object, "TestFileName", _mockLogger.Object);

            // Assert
            await functionAction.Should().NotThrowAsync<Exception>(It.IsAny<string>());
            _mockAzureBlobHelpers.Verify(x => x.DownloadBlobAsStreamAsync(It.IsAny<string>()), Times.Once);
            _mockAzureBlobHelpers.Verify(x => x.DeleteBlobAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteFileFromBlobStorageWhenIsDuplicateAsyncReturnsTrue()
        {
            // Arrange
            _mockTableHelpers.Setup(x => x.IsDuplicateAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            // Act
            Func<Task> functionAction = async () => await _func.Run(_mockStream.Object, "TestFileName", _mockLogger.Object);

            // Assert
            await functionAction.Should().NotThrowAsync<Exception>(It.IsAny<string>());
            _mockAzureBlobHelpers.Verify(x => x.DeleteBlobAsync(It.IsAny<string>()), Times.Once);
        }
    }
}
