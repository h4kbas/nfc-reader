using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NFCReader.Models;

namespace NFCReader.Interfaces;

/// <summary>
/// Defines the interface for NFC reader operations
/// </summary>
public interface INFCReader : IDisposable
{
    /// <summary>
    /// Gets whether the reader is connected
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the currently connected card, if any
    /// </summary>
    NFCCard? CurrentCard { get; }

    /// <summary>
    /// Gets the current reader information
    /// </summary>
    NFCReaderInfo ReaderInfo { get; }

    /// <summary>
    /// Event raised when a card is inserted
    /// </summary>
    event EventHandler<NFCCard>? CardInserted;

    /// <summary>
    /// Event raised when a card is removed
    /// </summary>
    event EventHandler<NFCCard>? CardRemoved;

    /// <summary>
    /// Event raised when the reader state changes
    /// </summary>
    event EventHandler<NFCReaderInfo>? ReaderStateChanged;

    /// <summary>
    /// Connects to the first available reader
    /// </summary>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to a specific reader by name
    /// </summary>
    Task<bool> ConnectAsync(string readerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the current reader
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of available readers
    /// </summary>
    Task<IEnumerable<NFCReaderInfo>> GetReadersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Transmits an APDU command to the card
    /// </summary>
    Task<APDUResponse> TransmitAsync(APDUCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the UID of the current card
    /// </summary>
    Task<string?> GetCardUIDAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a block from the card
    /// </summary>
    Task<byte[]?> ReadBlockAsync(byte blockNumber, byte length = 16, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes data to a block on the card
    /// </summary>
    Task<bool> WriteBlockAsync(byte blockNumber, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a block on the card
    /// </summary>
    Task<bool> AuthenticateBlockAsync(byte blockNumber, byte keyType = 0x60, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts monitoring for card insertion/removal
    /// </summary>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring for card insertion/removal
    /// </summary>
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);
}
