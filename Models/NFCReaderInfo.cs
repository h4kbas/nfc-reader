using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NFCReader.Models;

/// <summary>
/// Represents information about an NFC reader device
/// </summary>
public class NFCReaderInfo
{
    /// <summary>
    /// Gets or sets the unique name of the reader
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state of the reader
    /// </summary>
    public ReaderState State { get; set; }

    /// <summary>
    /// Gets or sets whether the reader is available
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Gets or sets the card currently in the reader, if any
    /// </summary>
    public NFCCard? CurrentCard { get; set; }

    /// <summary>
    /// Gets or sets additional reader properties
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Gets a user-friendly display name for the reader
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Name) ? "Unknown Reader" : Name;
}

/// <summary>
/// Represents the state of an NFC reader
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReaderState
{
    /// <summary>
    /// Reader state is unknown
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Reader is unavailable
    /// </summary>
    Unavailable = 1,

    /// <summary>
    /// Reader is available but empty
    /// </summary>
    Empty = 2,

    /// <summary>
    /// Reader has a card present
    /// </summary>
    Present = 3,

    /// <summary>
    /// Reader is in exclusive use
    /// </summary>
    Exclusive = 4,

    /// <summary>
    /// Reader is in use by other applications
    /// </summary>
    InUse = 5,

    /// <summary>
    /// Reader is muted
    /// </summary>
    Mute = 6,

    /// <summary>
    /// Reader is unpowered
    /// </summary>
    Unpowered = 7
}
