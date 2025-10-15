using FluentAssertions;
using NFCReader.Models;
using Xunit;

namespace NFCReader.Tests.Models;

public class APDUCommandTests
{
    [Fact]
    public void ToByteArray_WithNoDataAndNoLe_ShouldReturnCorrectBytes()
    {
        // Arrange
        var command = new APDUCommand
        {
            CLA = 0xFF,
            INS = 0xCA,
            P1 = 0x00,
            P2 = 0x00
        };

        // Act
        var result = command.ToByteArray();

        // Assert
        result.Should().HaveCount(4);
        result[0].Should().Be(0xFF);
        result[1].Should().Be(0xCA);
        result[2].Should().Be(0x00);
        result[3].Should().Be(0x00);
    }

    [Fact]
    public void ToByteArray_WithData_ShouldReturnCorrectBytes()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var command = new APDUCommand
        {
            CLA = 0xFF,
            INS = 0xD6,
            P1 = 0x00,
            P2 = 0x04,
            Data = data
        };

        // Act
        var result = command.ToByteArray();

        // Assert
        result.Should().HaveCount(8);
        result[0].Should().Be(0xFF);
        result[1].Should().Be(0xD6);
        result[2].Should().Be(0x00);
        result[3].Should().Be(0x04);
        result[4].Should().Be(0x03); // Data length
        result[5].Should().Be(0x01);
        result[6].Should().Be(0x02);
        result[7].Should().Be(0x03);
    }

    [Fact]
    public void ToByteArray_WithLe_ShouldReturnCorrectBytes()
    {
        // Arrange
        var command = new APDUCommand
        {
            CLA = 0xFF,
            INS = 0xB0,
            P1 = 0x00,
            P2 = 0x04,
            Le = 16
        };

        // Act
        var result = command.ToByteArray();

        // Assert
        result.Should().HaveCount(5);
        result[0].Should().Be(0xFF);
        result[1].Should().Be(0xB0);
        result[2].Should().Be(0x00);
        result[3].Should().Be(0x04);
        result[4].Should().Be(16);
    }

    [Fact]
    public void ToByteArray_WithDataAndLe_ShouldReturnCorrectBytes()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02 };
        var command = new APDUCommand
        {
            CLA = 0xFF,
            INS = 0xD6,
            P1 = 0x00,
            P2 = 0x04,
            Data = data,
            Le = 16
        };

        // Act
        var result = command.ToByteArray();

        // Assert
        result.Should().HaveCount(8);
        result[0].Should().Be(0xFF);
        result[1].Should().Be(0xD6);
        result[2].Should().Be(0x00);
        result[3].Should().Be(0x04);
        result[4].Should().Be(0x02); // Data length
        result[5].Should().Be(0x01);
        result[6].Should().Be(0x02);
        result[7].Should().Be(16); // Le
    }

    [Fact]
    public void GetUID_ShouldReturnCorrectCommand()
    {
        // Act
        var command = APDUCommand.GetUID;

        // Assert
        command.CLA.Should().Be(0xFF);
        command.INS.Should().Be(0xCA);
        command.P1.Should().Be(0x00);
        command.P2.Should().Be(0x00);
        command.Le.Should().Be(0x00);
        command.Data.Should().BeNull();
    }

    [Fact]
    public void ReadBlock_ShouldReturnCorrectCommand()
    {
        // Act
        var command = APDUCommand.ReadBlock(4, 16);

        // Assert
        command.CLA.Should().Be(0xFF);
        command.INS.Should().Be(0xB0);
        command.P1.Should().Be(0x00);
        command.P2.Should().Be(4);
        command.Le.Should().Be(16);
        command.Data.Should().BeNull();
    }

    [Fact]
    public void WriteBlock_ShouldReturnCorrectCommand()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var command = APDUCommand.WriteBlock(4, data);

        // Assert
        command.CLA.Should().Be(0xFF);
        command.INS.Should().Be(0xD6);
        command.P1.Should().Be(0x00);
        command.P2.Should().Be(4);
        command.Data.Should().BeEquivalentTo(data);
        command.Le.Should().BeNull();
    }

    [Fact]
    public void AuthenticateBlock_ShouldReturnCorrectCommand()
    {
        // Act
        var command = APDUCommand.AuthenticateBlock(4, 0x61);

        // Assert
        command.CLA.Should().Be(0xFF);
        command.INS.Should().Be(0x86);
        command.P1.Should().Be(0x00);
        command.P2.Should().Be(0x00);
        command.Data.Should().BeEquivalentTo(new byte[] { 0x01, 0x00, 0x04, 0x61, 0x01 });
        command.Le.Should().BeNull();
    }
}
