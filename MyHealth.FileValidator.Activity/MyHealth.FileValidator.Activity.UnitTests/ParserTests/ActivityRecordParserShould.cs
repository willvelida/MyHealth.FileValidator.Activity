using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using MyHealth.Common;
using MyHealth.FileValidator.Activity.Parsers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using mdl = MyHealth.Common.Models;

namespace MyHealth.FileValidator.Activity.UnitTests.ParserTests
{
    public class ActivityRecordParserShould
    {
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IServiceBusHelpers> _mockServiceBusHelpers;
        private Mock<Stream> _mockStream;

        private ActivityRecordParser _sut;

        public ActivityRecordParserShould()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockServiceBusHelpers = new Mock<IServiceBusHelpers>();
            _mockStream = new Mock<Stream>();

            _sut = new ActivityRecordParser(
                _mockConfiguration.Object,
                _mockServiceBusHelpers.Object);
        }

        [Fact]
        public async Task ThrowExceptionWhenStreamStartFails()
        {
            // Arrange
            _mockStream.Setup(x => x.Seek(It.IsAny<long>(), It.IsAny<SeekOrigin>())).Throws(new Exception());

            // Act
            Func<Task> parserAction = async () => await _sut.ParseActivityStream(_mockStream.Object);

            // Assert
            await parserAction.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task ThrowExceptionWhenInputDataIsInvalid()
        {
            // Arrange
            var testActivity = new mdl.Activity();

            byte[] byteArray = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(testActivity));
            MemoryStream memoryStream = new MemoryStream(byteArray);

            // Act
            Func<Task> parserAction = async () => await _sut.ParseActivityStream(memoryStream);

            // Assert
            await parserAction.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task ParseValidFileSuccessfullyToActivityObjectAndSendToActivityTopic()
        {
            // Arrange
            StreamReader streamReader = new StreamReader("TestData.csv");

            // Act
            Func<Task> parseAction = async () => await _sut.ParseActivityStream(streamReader.BaseStream);

            // Assert
            await parseAction.Should().NotThrowAsync<Exception>();
            _mockServiceBusHelpers.Verify(sb => sb.SendMessageToTopic(It.IsAny<string>(), It.IsAny<mdl.Activity>()), Times.Once);
        }
    }
}
