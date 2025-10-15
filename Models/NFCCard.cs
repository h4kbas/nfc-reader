using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace NFCReader.Models;

/// <summary>
/// Represents an NFC card with its properties and capabilities
/// </summary>
public class NFCCard
{
    /// <summary>
    /// Gets or sets the unique identifier of the card
    /// </summary>
    public string? UID { get; set; }

    /// <summary>
    /// Gets or sets the Answer To Reset (ATR) data
    /// </summary>
    public byte[]? ATR { get; set; }

    /// <summary>
    /// Gets or sets the card type identifier
    /// </summary>
    public string? CardType { get; set; }

    /// <summary>
    /// Gets or sets the protocol being used (T0, T1, etc.)
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// Gets or sets the card state
    /// </summary>
    public CardState State { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the card was last accessed
    /// </summary>
    public DateTime LastAccessed { get; set; }

    /// <summary>
    /// Gets or sets additional card properties
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Gets a formatted string representation of the card UID
    /// </summary>
    public string FormattedUID => UID != null ? string.Join(":", UID.Chunk(2).Select(chunk => new string(chunk))) : string.Empty;

    /// <summary>
    /// Gets a formatted string representation of the ATR
    /// </summary>
    public string FormattedATR => ATR != null ? BitConverter.ToString(ATR).Replace("-", " ") : string.Empty;
}

/// <summary>
/// Represents the state of an NFC card
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardState
{
    /// <summary>
    /// Card state is unknown
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// No card is present
    /// </summary>
    Absent = 1,

    /// <summary>
    /// Card is present but not powered
    /// </summary>
    Present = 2,

    /// <summary>
    /// Card is powered and ready
    /// </summary>
    Powered = 3,

    /// <summary>
    /// Card is in use by another application
    /// </summary>
    InUse = 4,

    /// <summary>
    /// Card is muted or unresponsive
    /// </summary>
    Muted = 5
}
