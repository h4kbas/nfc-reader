using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFCReader.Interfaces;
using NFCReader.Models;
using PCSC;
using PCSC.Exceptions;
using PCSC.Extensions;
using PCSC.Monitoring;

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
    private SCardMonitor? _cardMonitor;
    private bool _isMonitoring;
    private readonly SemaphoreSlim _cardSessionLock = new(1, 1);

    private static readonly byte[] DefaultMifareKey = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

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
        catch (NoSmartcardException)
        {
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
                    var current = state.CurrentState;
                    var readerState = ReaderState.Unknown;
                    if (current.HasFlag(SCRState.Unavailable))
                        readerState = ReaderState.Unavailable;
                    else if (current.HasFlag(SCRState.Unpowered))
                        readerState = ReaderState.Unpowered;
                    else if (current.HasFlag(SCRState.Mute))
                        readerState = ReaderState.Mute;
                    else if (current.HasFlag(SCRState.Exclusive))
                        readerState = ReaderState.Exclusive;
                    else if (current.HasFlag(SCRState.InUse))
                        readerState = ReaderState.InUse;
                    else if (current.CardIsPresent())
                        readerState = ReaderState.Present;
                    else if (current.CardIsAbsent())
                        readerState = ReaderState.Empty;

                    var isAvailable = !current.HasFlag(SCRState.Unavailable)
                        && !current.HasFlag(SCRState.Unpowered)
                        && !current.HasFlag(SCRState.Mute);

                    if (readerState == ReaderState.Unknown && isAvailable)
                        readerState = ReaderState.Empty;
                    
                    readers.Add(new NFCReaderInfo
                    {
                        Name = readerName,
                        State = readerState,
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
        catch (RemovedCardException)
        {
            try
            {
                await DisconnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disconnecting after card removal");
            }

            throw;
        }
        catch (NoSmartcardException)
        {
            throw;
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
        catch (RemovedCardException)
        {
            return null;
        }
        catch (NoSmartcardException)
        {
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

            if (!await AuthenticateBlockAsync(GetSectorAuthBlock(blockNumber), cancellationToken: cancellationToken))
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

            if (data.Length != 16)
                return false;

            if (!await AuthenticateBlockAsync(GetSectorAuthBlock(blockNumber), cancellationToken: cancellationToken))
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
    public async Task<bool> AuthenticateBlockAsync(byte blockNumber, byte keyType = 0x60, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsConnected)
                return false;

            const byte keySlot = 0x00;
            var loadKeyResponse = await TransmitAsync(APDUCommand.LoadAuthenticationKey(DefaultMifareKey), cancellationToken);
            if (!loadKeyResponse.IsSuccess)
                return false;

            var command = APDUCommand.AuthenticateBlock(blockNumber, keyType, keySlot);
            var response = await TransmitAsync(command, cancellationToken);
            
            return response.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to authenticate block {BlockNumber}", blockNumber);
            return false;
        }
    }

    private static byte GetSectorAuthBlock(byte blockNumber)
    {
        return (byte)(blockNumber / 4 * 4);
    }

    /// <inheritdoc/>
    public Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NFCReader));

        if (_isMonitoring)
            return Task.CompletedTask;

        try
        {
            using var listContext = _contextFactory.Establish(SCardScope.User);
            if (listContext == null)
            {
                _logger?.LogWarning("No PC/SC context for monitoring");
                return Task.CompletedTask;
            }

            var readerNames = listContext.GetReaders()?.ToArray() ?? Array.Empty<string>();
            if (readerNames.Length == 0)
            {
                _logger?.LogWarning("No readers to monitor");
                return Task.CompletedTask;
            }

            _cardMonitor = new SCardMonitor(_contextFactory, SCardScope.User);
            _cardMonitor.CardInserted += OnCardMonitorInserted;
            _cardMonitor.CardRemoved += OnCardMonitorRemoved;
            _cardMonitor.Initialized += OnCardMonitorInitialized;
            _cardMonitor.MonitorException += OnCardMonitorException;
            _cardMonitor.Start(readerNames);

            _isMonitoring = true;
            _logger?.LogInformation("Monitoring {ReaderCount} reader(s)", readerNames.Length);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to start reader monitoring");
            if (_cardMonitor != null)
            {
                _cardMonitor.Dispose();
                _cardMonitor = null;
            }

            _isMonitoring = false;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (!_isMonitoring || _cardMonitor == null)
            return;

        _cardMonitor.Cancel();
        _cardMonitor.CardInserted -= OnCardMonitorInserted;
        _cardMonitor.CardRemoved -= OnCardMonitorRemoved;
        _cardMonitor.Initialized -= OnCardMonitorInitialized;
        _cardMonitor.MonitorException -= OnCardMonitorException;
        _cardMonitor.Dispose();
        _cardMonitor = null;
        _isMonitoring = false;

        await DisconnectAsync(cancellationToken);
        _logger?.LogInformation("Stopped reader monitoring");
    }

    private void OnCardMonitorInserted(object sender, CardStatusEventArgs e)
    {
        _ = HandleCardInsertedAsync(e);
    }

    private void OnCardMonitorInitialized(object sender, CardStatusEventArgs e)
    {
        if (e.State.CardIsPresent())
            _ = HandleCardInsertedAsync(e);
        else
            RaiseReaderStateChanged(e.ReaderName, ReaderState.Empty);
    }

    private void OnCardMonitorRemoved(object sender, CardStatusEventArgs e)
    {
        _ = HandleCardRemovedAsync(e);
    }

    private void OnCardMonitorException(object sender, PCSCException exception)
    {
        _logger?.LogWarning(exception, "Reader monitor error");
    }

    private async Task HandleCardInsertedAsync(CardStatusEventArgs e)
    {
        await _cardSessionLock.WaitAsync();
        try
        {
            if (!await ConnectAsync(e.ReaderName))
                return;

            var uid = await GetCardUIDAsync();
            var card = new NFCCard
            {
                UID = uid,
                ATR = e.Atr is { Length: > 0 } ? e.Atr : null,
                State = CardState.Powered,
                LastAccessed = DateTime.UtcNow
            };

            CurrentCard = card;
            RaiseReaderStateChanged(e.ReaderName, ReaderState.Present);
            CardInserted?.Invoke(this, card);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to handle card inserted on {ReaderName}", e.ReaderName);
        }
        finally
        {
            _cardSessionLock.Release();
        }
    }

    private async Task HandleCardRemovedAsync(CardStatusEventArgs e)
    {
        await _cardSessionLock.WaitAsync();
        try
        {
            var card = CurrentCard;
            await DisconnectAsync();
            RaiseReaderStateChanged(e.ReaderName, ReaderState.Empty);

            if (card != null)
                CardRemoved?.Invoke(this, card);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to handle card removed on {ReaderName}", e.ReaderName);
        }
        finally
        {
            _cardSessionLock.Release();
        }
    }

    private void RaiseReaderStateChanged(string readerName, ReaderState state)
    {
        var info = new NFCReaderInfo
        {
            Name = readerName,
            State = state,
            IsAvailable = state != ReaderState.Unavailable && state != ReaderState.Unpowered && state != ReaderState.Mute,
            CurrentCard = CurrentCard
        };

        ReaderInfo = info;
        ReaderStateChanged?.Invoke(this, info);
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
                if (_isMonitoring && _cardMonitor != null)
                {
                    _cardMonitor.Cancel();
                    _cardMonitor.Dispose();
                    _cardMonitor = null;
                    _isMonitoring = false;
                }

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
