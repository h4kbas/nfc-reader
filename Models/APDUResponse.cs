using System;

namespace NFCReader.Models;

/// <summary>
/// Represents an APDU response from an NFC card
/// </summary>
public class APDUResponse
{
    /// <summary>
    /// Gets or sets the response data
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the status word (SW1 and SW2)
    /// </summary>
    public ushort StatusWord { get; set; }

    /// <summary>
    /// Gets the first status byte (SW1)
    /// </summary>
    public byte SW1 => (byte)(StatusWord >> 8);

    /// <summary>
    /// Gets the second status byte (SW2)
    /// </summary>
    public byte SW2 => (byte)(StatusWord & 0xFF);

    /// <summary>
    /// Gets whether the command was successful
    /// </summary>
    public bool IsSuccess => StatusWord == 0x9000;

    /// <summary>
    /// Gets a human-readable description of the status
    /// </summary>
    public string StatusDescription => GetStatusDescription(StatusWord);

    /// <summary>
    /// Gets the response data as a hex string
    /// </summary>
    public string DataAsHex => BitConverter.ToString(Data).Replace("-", " ");

    /// <summary>
    /// Gets the response data as a UTF-8 string
    /// </summary>
    public string DataAsString => System.Text.Encoding.UTF8.GetString(Data);

    /// <summary>
    /// Creates an APDU response from raw bytes
    /// </summary>
    public static APDUResponse FromBytes(byte[] response)
    {
        if (response.Length < 2)
        {
            throw new ArgumentException("Response must be at least 2 bytes long", nameof(response));
        }

        var dataLength = response.Length - 2;
        var data = new byte[dataLength];
        if (dataLength > 0)
        {
            Array.Copy(response, 0, data, 0, dataLength);
        }

        var statusWord = (ushort)((response[^2] << 8) | response[^1]);

        return new APDUResponse
        {
            Data = data,
            StatusWord = statusWord
        };
    }

    private static string GetStatusDescription(ushort statusWord)
    {
        return statusWord switch
        {
            0x9000 => "Command completed successfully",
            0x6100 => "Response data available",
            0x6C00 => "Wrong length",
            0x6E00 => "Invalid instruction",
            0x6D00 => "Invalid instruction code",
            0x6F00 => "Unknown error",
            0x6A00 => "Wrong parameters",
            0x6A80 => "Incorrect parameters in data field",
            0x6A81 => "Function not supported",
            0x6A82 => "File not found",
            0x6A83 => "Record not found",
            0x6A84 => "Not enough memory space",
            0x6A85 => "Lc inconsistent with TLV structure",
            0x6A86 => "Incorrect parameters P1-P2",
            0x6A87 => "Lc inconsistent with P1-P2",
            0x6A88 => "Referenced data not found",
            0x6A89 => "File already exists",
            0x6A8A => "DF name already exists",
            0x6A8B => "DF name not found",
            0x6A8C => "Bad file name",
            0x6A8D => "Bad file name length",
            0x6A8E => "Bad file name format",
            0x6A8F => "Bad file name encoding",
            0x6A90 => "Bad file name encoding",
            0x6A91 => "Bad file name encoding",
            0x6A92 => "Bad file name encoding",
            0x6A93 => "Bad file name encoding",
            0x6A94 => "Bad file name encoding",
            0x6A95 => "Bad file name encoding",
            0x6A96 => "Bad file name encoding",
            0x6A97 => "Bad file name encoding",
            0x6A98 => "Bad file name encoding",
            0x6A99 => "Bad file name encoding",
            0x6A9A => "Bad file name encoding",
            0x6A9B => "Bad file name encoding",
            0x6A9C => "Bad file name encoding",
            0x6A9D => "Bad file name encoding",
            0x6A9E => "Bad file name encoding",
            0x6A9F => "Bad file name encoding",
            _ => $"Unknown status: 0x{statusWord:X4}"
        };
    }
}
