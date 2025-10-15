using FluentAssertions;
using NFCReader.Models;
using System;
using Xunit;

namespace NFCReader.Tests.Models;

public class APDUResponseTests
{
    [Fact]
    public void FromBytes_WithValidResponse_ShouldParseCorrectly()
    {
        // Arrange
        var responseData = new byte[] { 0x01, 0x02, 0x03, 0x90, 0x00 };

        // Act
        var response = APDUResponse.FromBytes(responseData);

        // Assert
        response.Data.Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
        response.StatusWord.Should().Be(0x9000);
        response.SW1.Should().Be(0x90);
        response.SW2.Should().Be(0x00);
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void FromBytes_WithNoData_ShouldParseCorrectly()
    {
        // Arrange
        var responseData = new byte[] { 0x6A, 0x00 };

        // Act
        var response = APDUResponse.FromBytes(responseData);

        // Assert
        response.Data.Should().BeEmpty();
        response.StatusWord.Should().Be(0x6A00);
        response.SW1.Should().Be(0x6A);
        response.SW2.Should().Be(0x00);
        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void FromBytes_WithTooShortResponse_ShouldThrowException()
    {
        // Arrange
        var responseData = new byte[] { 0x90 };

        // Act & Assert
        var action = () => APDUResponse.FromBytes(responseData);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*at least 2 bytes long*");
    }

    [Fact]
    public void StatusDescription_WithSuccessStatus_ShouldReturnCorrectDescription()
    {
        // Arrange
        var response = new APDUResponse { StatusWord = 0x9000 };

        // Act
        var description = response.StatusDescription;

        // Assert
        description.Should().Be("Command completed successfully");
    }

    [Fact]
    public void StatusDescription_WithErrorStatus_ShouldReturnCorrectDescription()
    {
        // Arrange
        var response = new APDUResponse { StatusWord = 0x6A00 };

        // Act
        var description = response.StatusDescription;

        // Assert
        description.Should().Be("Wrong parameters");
    }

    [Fact]
    public void StatusDescription_WithUnknownStatus_ShouldReturnUnknownMessage()
    {
        // Arrange
        var response = new APDUResponse { StatusWord = 0x9999 };

        // Act
        var description = response.StatusDescription;

        // Assert
        description.Should().Be("Unknown status: 0x9999");
    }

    [Fact]
    public void DataAsHex_WithData_ShouldReturnFormattedHex()
    {
        // Arrange
        var response = new APDUResponse 
        { 
            Data = new byte[] { 0x01, 0x02, 0x03 } 
        };

        // Act
        var hex = response.DataAsHex;

        // Assert
        hex.Should().Be("01 02 03");
    }

    [Fact]
    public void DataAsHex_WithNoData_ShouldReturnEmptyString()
    {
        // Arrange
        var response = new APDUResponse { Data = Array.Empty<byte>() };

        // Act
        var hex = response.DataAsHex;

        // Assert
        hex.Should().BeEmpty();
    }

    [Fact]
    public void DataAsString_WithValidUtf8Data_ShouldReturnCorrectString()
    {
        // Arrange
        var response = new APDUResponse 
        { 
            Data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F } // "Hello"
        };

        // Act
        var str = response.DataAsString;

        // Assert
        str.Should().Be("Hello");
    }

    [Fact]
    public void DataAsString_WithNoData_ShouldReturnEmptyString()
    {
        // Arrange
        var response = new APDUResponse { Data = Array.Empty<byte>() };

        // Act
        var str = response.DataAsString;

        // Assert
        str.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0x9000, true)]
    [InlineData(0x6100, false)]
    [InlineData(0x6A00, false)]
    [InlineData(0x6E00, false)]
    public void IsSuccess_WithDifferentStatuses_ShouldReturnCorrectValue(ushort statusWord, bool expected)
    {
        // Arrange
        var response = new APDUResponse { StatusWord = statusWord };

        // Act
        var isSuccess = response.IsSuccess;

        // Assert
        isSuccess.Should().Be(expected);
    }
}
