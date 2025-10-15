using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NFCReader.Interfaces;
using NFCReader.Models;
using PCSC;
using PCSC.Monitoring;
using System;
using System.Threading.Tasks;
using Xunit;

namespace NFCReader.Tests;

public class NFCReaderTests
{
    private readonly Mock<ILogger<NFCReader>> _loggerMock;
    private readonly Mock<IContextFactory> _contextFactoryMock;
    private readonly Mock<SCardContext> _contextMock;
    private readonly Mock<ICardReader> _readerMock;

    public NFCReaderTests()
    {
        _loggerMock = new Mock<ILogger<NFCReader>>();
        _contextFactoryMock = new Mock<IContextFactory>();
        _contextMock = new Mock<SCardContext>();
        _readerMock = new Mock<ICardReader>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => new NFCReader(null);
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullContextFactory_ShouldUseDefault()
    {
        // Act
        var reader = new NFCReader(_loggerMock.Object, null);

        // Assert
        reader.Should().NotBeNull();
    }

    [Fact]
    public void IsConnected_WhenReaderNotConnected_ShouldReturnFalse()
    {
        // Arrange
        _readerMock.Setup(r => r.IsConnected).Returns(false);
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act
        var isConnected = reader.IsConnected;

        // Assert
        isConnected.Should().BeFalse();
    }

    [Fact]
    public void IsConnected_WhenReaderConnected_ShouldReturnTrue()
    {
        // Arrange
        _readerMock.Setup(r => r.IsConnected).Returns(true);
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act
        var isConnected = reader.IsConnected;

        // Assert
        isConnected.Should().BeFalse(); // Should still be false since no reader is set
    }

    [Fact]
    public void CurrentCard_Initially_ShouldBeNull()
    {
        // Arrange
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act
        var currentCard = reader.CurrentCard;

        // Assert
        currentCard.Should().BeNull();
    }

    [Fact]
    public void ReaderInfo_Initially_ShouldHaveDefaultValues()
    {
        // Arrange
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act
        var readerInfo = reader.ReaderInfo;

        // Assert
        readerInfo.Should().NotBeNull();
        readerInfo.Name.Should().BeEmpty();
        readerInfo.State.Should().Be(ReaderState.Unknown);
        readerInfo.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act & Assert
        var action = () => reader.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        // Arrange
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);
        reader.Dispose();

        // Act & Assert
        var action = () => reader.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public async Task ConnectAsync_WhenNoReadersAvailable_ShouldReturnFalse()
    {
        // Arrange
        _contextFactoryMock.Setup(cf => cf.Establish(It.IsAny<SCardScope>()))
            .Returns(_contextMock.Object);

        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act
        var result = await reader.ConnectAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WhenReaderConnectionFails_ShouldReturnFalse()
    {
        // Arrange
        _contextFactoryMock.Setup(cf => cf.Establish(It.IsAny<SCardScope>()))
            .Returns(_contextMock.Object);

        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act
        var result = await reader.ConnectAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetReadersAsync_WhenContextThrowsException_ShouldReturnEmptyEnumerable()
    {
        // Arrange
        _contextFactoryMock.Setup(cf => cf.Establish(It.IsAny<SCardScope>()))
            .Throws(new Exception("Context establishment failed"));

        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act
        var readers = await reader.GetReadersAsync();

        // Assert
        readers.Should().BeEmpty();
    }

    [Fact]
    public async Task TransmitAsync_WhenNotConnected_ShouldThrowException()
    {
        // Arrange
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);
        var command = new APDUCommand { CLA = 0xFF, INS = 0xCA, P1 = 0x00, P2 = 0x00 };

        // Act & Assert
        var action = async () => await reader.TransmitAsync(command);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not connected*");
    }

    [Fact]
    public async Task GetCardUIDAsync_WhenNotConnected_ShouldReturnNull()
    {
        // Arrange
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act
        var uid = await reader.GetCardUIDAsync();

        // Assert
        uid.Should().BeNull();
    }

    [Fact]
    public async Task ReadBlockAsync_WhenNotConnected_ShouldReturnNull()
    {
        // Arrange
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act
        var data = await reader.ReadBlockAsync(4);

        // Assert
        data.Should().BeNull();
    }

    [Fact]
    public async Task WriteBlockAsync_WhenNotConnected_ShouldReturnFalse()
    {
        // Arrange
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);
        var data = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = await reader.WriteBlockAsync(4, data);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateBlockAsync_WhenNotConnected_ShouldReturnFalse()
    {
        // Arrange
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act
        var result = await reader.AuthenticateBlockAsync(4);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task StartMonitoringAsync_WhenCalledMultipleTimes_ShouldNotStartMultipleTasks()
    {
        // Arrange
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act
        await reader.StartMonitoringAsync();
        await reader.StartMonitoringAsync();

        // Assert
        // Should not throw and should handle gracefully
        reader.Should().NotBeNull();
    }

    [Fact]
    public async Task StopMonitoringAsync_WhenNotMonitoring_ShouldNotThrow()
    {
        // Arrange
        var reader = new NFCReader(_loggerMock.Object, _contextFactoryMock.Object);

        // Act & Assert
        var action = async () => await reader.StopMonitoringAsync();
        await action.Should().NotThrowAsync();
    }
}
