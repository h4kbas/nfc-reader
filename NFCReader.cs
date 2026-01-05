using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFCReader.Interfaces;
using NFCReader.Models;
using PCSC;

namespace NFCReader;

/// <summary>
/// Cross-platform NFC reader implementation using PC/SC
/// </summary>
public class NFCReader : INFCReader
{
    private readonly ILogger<NFCReader>? _logger;
    private readonly IContextFactory _contextFactory;
    private ISCardContext? _context;
    private ICardReader? _reader;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the NFCReader class
    /// </summary>
    public NFCReader(ILogger<NFCReader>? logger = null, IContextFactory? contextFactory = null)
    {
        _logger = logger;
        _contextFactory = contextFactory ?? ContextFactory.Instance;
    }

    /// <inheritdoc/>
    public bool IsConnected => _reader?.IsConnected == true;

    /// <inheritdoc/>
    public NFCCard? CurrentCard { get; private set; }

    /// <inheritdoc/>
    public NFCReaderInfo ReaderInfo { get; private set; } = new();

    /// <inheritdoc/>
    public event EventHandler<NFCCard>? CardInserted;

    /// <inheritdoc/>
    public event EventHandler<NFCCard>? CardRemoved;

    /// <inheritdoc/>
    public event EventHandler<NFCReaderInfo>? ReaderStateChanged;

    /// <inheritdoc/>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var readers = await GetReadersAsync(cancellationToken);
            var firstReader = readers.FirstOrDefault(r => r.IsAvailable);
            
            if (firstReader == null)
            {
                _logger?.LogWarning("No available readers found");
                return false;
            }

            return await ConnectAsync(firstReader.Name, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to first available reader");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ConnectAsync(string readerName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NFCReader));

            if (IsConnected)
                await DisconnectAsync(cancellationToken);

            _context = _contextFactory.Establish(SCardScope.User);
            _reader = _context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any);

            if (_reader.IsConnected)
            {
                ReaderInfo = new NFCReaderInfo
                {
                    Name = readerName,
                    State = ReaderState.Present,
                    IsAvailable = true
                };

                _logger?.LogInformation("Connected to reader: {ReaderName}", readerName);
                return true;
            }

            _logger?.LogWarning("Failed to connect to reader: {ReaderName}", readerName);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to reader: {ReaderName}", readerName);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_reader != null && _reader.IsConnected)
            {
                _reader.Disconnect(SCardReaderDisposition.Reset);
                _reader = null;
            }

            if (_context != null)
            {
                _context.Release();
                _context = null;
            }

            ReaderInfo = new NFCReaderInfo();
            CurrentCard = null;

            _logger?.LogInformation("Disconnected from reader");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during disconnect");
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NFCReaderInfo>> GetReadersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_context == null)
            {
                _context = _contextFactory.Establish(SCardScope.User);
            }

            var readerNames = _context.GetReaders();
            var readers = new List<NFCReaderInfo>();

            foreach (var readerName in readerNames)
            {
                try
                {
                    var state = _context.GetReaderStatus(readerName);
                    var isAvailable = state.CurrentState.HasFlag(SCRState.Present);
                    
                    readers.Add(new NFCReaderInfo
                    {
                        Name = readerName,
                        State = ReaderState.Present,
                        IsAvailable = isAvailable
                    });
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to get status for reader: {ReaderName}", readerName);
                    readers.Add(new NFCReaderInfo
                    {
                        Name = readerName,
                        State = ReaderState.Unknown,
                        IsAvailable = false
                    });
                }
            }

            return readers;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get readers");
            return Enumerable.Empty<NFCReaderInfo>();
        }
    }

    /// <inheritdoc/>
    public async Task<APDUResponse> TransmitAsync(APDUCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NFCReader));

            if (!IsConnected)
                throw new InvalidOperationException("Not connected to a reader");

            var commandBytes = command.ToByteArray();
            var receiveBuffer = new byte[256]; // Standard APDU response buffer size
            var responseLength = _reader!.Transmit(commandBytes, receiveBuffer);
            
            // Extract the actual response data
            var actualResponse = new byte[responseLength];
            Array.Copy(receiveBuffer, actualResponse, responseLength);
            
            return APDUResponse.FromBytes(actualResponse);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to transmit APDU command");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetCardUIDAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsConnected)
                return null;

            var response = await TransmitAsync(APDUCommand.GetUID, cancellationToken);
            if (response.IsSuccess)
            {
                return response.DataAsHex;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get card UID");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]?> ReadBlockAsync(byte blockNumber, byte length = 16, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsConnected)
                return null;

            var command = APDUCommand.ReadBlock(blockNumber, length);
            var response = await TransmitAsync(command, cancellationToken);
            
            if (response.IsSuccess)
            {
                return response.Data;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to read block {BlockNumber}", blockNumber);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> WriteBlockAsync(byte blockNumber, byte[] data, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsConnected)
                return false;

            var command = APDUCommand.WriteBlock(blockNumber, data);
            var response = await TransmitAsync(command, cancellationToken);
            
            return response.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to write block {BlockNumber}", blockNumber);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AuthenticateBlockAsync(byte blockNumber, byte keyType = 0x61, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsConnected)
                return false;

            var command = APDUCommand.AuthenticateBlock(blockNumber, keyType);
            var response = await TransmitAsync(command, cancellationToken);
            
            return response.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to authenticate block {BlockNumber}", blockNumber);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        // Simple monitoring implementation
        _logger?.LogInformation("Started reader monitoring (basic implementation)");
    }

    /// <inheritdoc/>
    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Stopped reader monitoring");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                if (_reader != null && _reader.IsConnected)
                {
                    _reader.Disconnect(SCardReaderDisposition.Reset);
                }

                if (_context != null)
                {
                    _context.Release();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during disposal");
            }
            finally
            {
                _reader = null;
                _context = null;
                _disposed = true;
            }
        }
    }
}
