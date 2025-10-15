using System;

namespace NFCReader.Models;

/// <summary>
/// Represents an APDU (Application Protocol Data Unit) command
/// </summary>
public class APDUCommand
{
    /// <summary>
    /// Gets or sets the Class byte (CLA)
    /// </summary>
    public byte CLA { get; set; }

    /// <summary>
    /// Gets or sets the Instruction byte (INS)
    /// </summary>
    public byte INS { get; set; }

    /// <summary>
    /// Gets or sets the Parameter 1 byte (P1)
    /// </summary>
    public byte P1 { get; set; }

    /// <summary>
    /// Gets or sets the Parameter 2 byte (P2)
    /// </summary>
    public byte P2 { get; set; }

    /// <summary>
    /// Gets or sets the data to send
    /// </summary>
    public byte[]? Data { get; set; }

    /// <summary>
    /// Gets or sets the expected length of response data
    /// </summary>
    public byte? Le { get; set; }

    /// <summary>
    /// Gets the complete APDU command as a byte array
    /// </summary>
    public byte[] ToByteArray()
    {
        var dataLength = Data?.Length ?? 0;
        var hasLe = Le.HasValue;
        var totalLength = 4 + (dataLength > 0 ? 1 : 0) + dataLength + (hasLe ? 1 : 0);

        var result = new byte[totalLength];
        result[0] = CLA;
        result[1] = INS;
        result[2] = P1;
        result[3] = P2;

        var currentIndex = 4;

        if (dataLength > 0)
        {
            result[currentIndex] = (byte)dataLength;
            currentIndex++;
            Array.Copy(Data!, 0, result, currentIndex, dataLength);
            currentIndex += dataLength;
        }

        if (hasLe)
        {
            result[currentIndex] = Le.Value;
        }

        return result;
    }

    /// <summary>
    /// Creates a GET UID command
    /// </summary>
    public static APDUCommand GetUID => new()
    {
        CLA = 0xFF,
        INS = 0xCA,
        P1 = 0x00,
        P2 = 0x00,
        Le = 0x00
    };

    /// <summary>
    /// Creates a READ BLOCK command
    /// </summary>
    public static APDUCommand ReadBlock(byte blockNumber, byte length = 16) => new()
    {
        CLA = 0xFF,
        INS = 0xB0,
        P1 = 0x00,
        P2 = blockNumber,
        Le = length
    };

    /// <summary>
    /// Creates a WRITE BLOCK command
    /// </summary>
    public static APDUCommand WriteBlock(byte blockNumber, byte[] data) => new()
    {
        CLA = 0xFF,
        INS = 0xD6,
        P1 = 0x00,
        P2 = blockNumber,
        Data = data
    };

    /// <summary>
    /// Creates an AUTHENTICATE BLOCK command
    /// </summary>
    public static APDUCommand AuthenticateBlock(byte blockNumber, byte keyType = 0x61) => new()
    {
        CLA = 0xFF,
        INS = 0x86,
        P1 = 0x00,
        P2 = 0x00,
        Data = new byte[] { 0x01, 0x00, blockNumber, keyType, 0x01 }
    };
}
