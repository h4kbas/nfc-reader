using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFCReader.Utils;

/// <summary>
/// Utility class providing helper methods for NFC operations
/// </summary>
public static class NFCHelper
{
    /// <summary>
    /// Converts a byte array to a hexadecimal string
    /// </summary>
    /// <param name="bytes">The byte array to convert</param>
    /// <param name="separator">Optional separator between bytes</param>
    /// <returns>Hexadecimal string representation</returns>
    public static string ToHexString(this byte[] bytes, string separator = " ")
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        return BitConverter.ToString(bytes).Replace("-", separator);
    }

    /// <summary>
    /// Converts a hexadecimal string to a byte array
    /// </summary>
    /// <param name="hex">The hexadecimal string to convert</param>
    /// <returns>Byte array representation</returns>
    public static byte[] FromHexString(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return Array.Empty<byte>();

        // Remove common separators
        hex = hex.Replace(" ", "").Replace(":", "").Replace("-", "");

        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have an even number of characters", nameof(hex));

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    /// <summary>
    /// Converts a byte array to a UTF-8 string
    /// </summary>
    /// <param name="bytes">The byte array to convert</param>
    /// <returns>UTF-8 string representation</returns>
    public static string ToUtf8String(this byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Converts a string to a UTF-8 byte array
    /// </summary>
    /// <param name="text">The string to convert</param>
    /// <returns>UTF-8 byte array representation</returns>
    public static byte[] ToUtf8Bytes(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<byte>();

        return Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    /// Pads a byte array to a specified length
    /// </summary>
    /// <param name="bytes">The byte array to pad</param>
    /// <param name="length">The target length</param>
    /// <param name="paddingByte">The byte to use for padding</param>
    /// <returns>Padded byte array</returns>
    public static byte[] PadToLength(this byte[] bytes, int length, byte paddingByte = 0x00)
    {
        if (bytes == null)
            bytes = Array.Empty<byte>();

        if (bytes.Length >= length)
            return bytes;

        var result = new byte[length];
        Array.Copy(bytes, result, bytes.Length);
        
        for (int i = bytes.Length; i < length; i++)
        {
            result[i] = paddingByte;
        }

        return result;
    }

    /// <summary>
    /// Trims padding bytes from the end of a byte array
    /// </summary>
    /// <param name="bytes">The byte array to trim</param>
    /// <param name="paddingByte">The padding byte to remove</param>
    /// <returns>Trimmed byte array</returns>
    public static byte[] TrimPadding(this byte[] bytes, byte paddingByte = 0x00)
    {
        if (bytes == null || bytes.Length == 0)
            return Array.Empty<byte>();

        int endIndex = bytes.Length - 1;
        while (endIndex >= 0 && bytes[endIndex] == paddingByte)
        {
            endIndex--;
        }

        if (endIndex < 0)
            return Array.Empty<byte>();

        var result = new byte[endIndex + 1];
        Array.Copy(bytes, result, endIndex + 1);
        return result;
    }

    /// <summary>
    /// Splits a string into chunks of specified length
    /// </summary>
    /// <param name="text">The string to split</param>
    /// <param name="chunkSize">The size of each chunk</param>
    /// <returns>Enumerable of string chunks</returns>
    public static IEnumerable<string> SplitIntoChunks(this string text, int chunkSize)
    {
        if (string.IsNullOrEmpty(text) || chunkSize <= 0)
            yield break;

        for (int i = 0; i < text.Length; i += chunkSize)
        {
            yield return text.Substring(i, Math.Min(chunkSize, text.Length - i));
        }
    }

    /// <summary>
    /// Validates if a string is a valid hexadecimal string
    /// </summary>
    /// <param name="hex">The string to validate</param>
    /// <returns>True if valid hexadecimal, false otherwise</returns>
    public static bool IsValidHexString(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        // Remove common separators
        hex = hex.Replace(" ", "").Replace(":", "").Replace("-", "");

        if (hex.Length % 2 != 0)
            return false;

        return hex.All(c => char.IsDigit(c) || (char.ToUpper(c) >= 'A' && char.ToUpper(c) <= 'F'));
    }

    /// <summary>
    /// Calculates the CRC16 checksum of a byte array
    /// </summary>
    /// <param name="bytes">The byte array to calculate checksum for</param>
    /// <returns>CRC16 checksum</returns>
    public static ushort CalculateCRC16(this byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return 0;

        ushort crc = 0xFFFF;
        foreach (byte b in bytes)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }
        return crc;
    }

    /// <summary>
    /// Formats a byte array for display with optional formatting
    /// </summary>
    /// <param name="bytes">The byte array to format</param>
    /// <param name="format">The format to use (hex, bin, dec)</param>
    /// <param name="separator">The separator to use between values</param>
    /// <returns>Formatted string representation</returns>
    public static string FormatBytes(this byte[] bytes, string format = "hex", string separator = " ")
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        return format.ToLower() switch
        {
            "hex" => bytes.ToHexString(separator),
            "bin" => string.Join(separator, bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0'))),
            "dec" => string.Join(separator, bytes.Select(b => b.ToString())),
            _ => bytes.ToHexString(separator)
        };
    }
}
