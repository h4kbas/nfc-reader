# NFC Reader Library

A modern, cross-platform .NET library for NFC (Near Field Communication) operations using PC/SC (Personal Computer/Smart Card) technology.

## Features

- **Cross-platform support**: Works on Windows, macOS, and Linux
- **Modern .NET**: Built with .NET 6+ and .NET Standard 2.1
- **Async/await support**: All operations are asynchronous for better performance
- **Event-driven architecture**: Real-time card insertion/removal detection
- **Comprehensive error handling**: Proper exception handling and logging
- **Extensible design**: Interface-based architecture for easy testing and mocking
- **PC/SC integration**: Uses industry-standard PC/SC for reliable smart card communication

## Requirements

- .NET 6.0 or later
- PC/SC service running on your system
- NFC reader hardware (USB, Bluetooth, or built-in)

### Platform-specific requirements:

- **Windows**: Windows Smart Card service (usually enabled by default)
- **macOS**: PCSC-Lite (install via Homebrew: `brew install pcsc-lite`)
- **Linux**: PCSC-Lite (install via package manager: `sudo apt-get install pcscd` on Ubuntu/Debian)

## Installation

### NuGet Package

```bash
dotnet add package NFCReader
```

### Source Code

```bash
git clone https://github.com/yourusername/nfc-reader.git
cd nfc-reader
dotnet restore
dotnet build
```

## Quick Start

### Basic Usage

```csharp
using NFCReader;
using Microsoft.Extensions.Logging;

// Create logger (optional)
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

// Create NFC reader instance
using var nfcReader = new NFCReader(logger);

// Get available readers
var readers = await nfcReader.GetReadersAsync();
var firstReader = readers.FirstOrDefault(r => r.IsAvailable);

if (firstReader != null)
{
    // Connect to reader
    var connected = await nfcReader.ConnectAsync(firstReader.Name);
    
    if (connected)
    {
        // Read card UID
        var uid = await nfcReader.GetCardUIDAsync();
        Console.WriteLine($"Card UID: {uid}");
    }
}
```

### Event Handling

```csharp
// Set up event handlers
nfcReader.CardInserted += (sender, card) =>
{
    Console.WriteLine($"Card inserted: {card.FormattedUID}");
};

nfcReader.CardRemoved += (sender, card) =>
{
    Console.WriteLine($"Card removed: {card.FormattedUID}");
};

// Start monitoring
await nfcReader.StartMonitoringAsync();
```

### Advanced Operations

```csharp
// Read data from a block
var blockData = await nfcReader.ReadBlockAsync(4);
if (blockData != null)
{
    Console.WriteLine($"Block data: {BitConverter.ToString(blockData)}");
}

// Write data to a block
var data = Encoding.UTF8.GetBytes("Hello NFC!");
var success = await nfcReader.WriteBlockAsync(5, data);

// Custom APDU command
var command = new APDUCommand
{
    CLA = 0xFF,
    INS = 0xCA,
    P1 = 0x00,
    P2 = 0x00,
    Le = 0x00
};

var response = await nfcReader.TransmitAsync(command);
if (response.IsSuccess)
{
    Console.WriteLine($"Response: {response.DataAsHex}");
}
```

## API Reference

### Core Classes

#### `NFCReader`

Main class for NFC operations.

**Properties:**
- `IsConnected`: Gets whether the reader is connected
- `CurrentCard`: Gets the currently connected card
- `ReaderInfo`: Gets current reader information

**Methods:**
- `ConnectAsync()`: Connect to the first available reader
- `ConnectAsync(string readerName)`: Connect to a specific reader
- `DisconnectAsync()`: Disconnect from the current reader
- `GetReadersAsync()`: Get list of available readers
- `TransmitAsync(APDUCommand)`: Send APDU command to card
- `GetCardUIDAsync()`: Get card's unique identifier
- `ReadBlockAsync(byte blockNumber)`: Read data from a block
- `WriteBlockAsync(byte blockNumber, byte[] data)`: Write data to a block
- `AuthenticateBlockAsync(byte blockNumber)`: Authenticate a block
- `StartMonitoringAsync()`: Start monitoring for card changes
- `StopMonitoringAsync()`: Stop monitoring for card changes

**Events:**
- `CardInserted`: Raised when a card is inserted
- `CardRemoved`: Raised when a card is removed
- `ReaderStateChanged`: Raised when reader state changes

#### `APDUCommand`

Represents an APDU (Application Protocol Data Unit) command.

**Properties:**
- `CLA`: Class byte
- `INS`: Instruction byte
- `P1`, `P2`: Parameter bytes
- `Data`: Command data
- `Le`: Expected response length

**Static Methods:**
- `GetUID`: Creates a GET UID command
- `ReadBlock(byte blockNumber)`: Creates a READ BLOCK command
- `WriteBlock(byte blockNumber, byte[] data)`: Creates a WRITE BLOCK command
- `AuthenticateBlock(byte blockNumber)`: Creates an AUTHENTICATE BLOCK command

#### `APDUResponse`

Represents an APDU response from the card.

**Properties:**
- `Data`: Response data
- `StatusWord`: Status word (SW1 + SW2)
- `IsSuccess`: Whether the command was successful
- `StatusDescription`: Human-readable status description
- `DataAsHex`: Data as hexadecimal string
- `DataAsString`: Data as UTF-8 string

### Utility Classes

#### `NFCHelper`

Static utility methods for common operations:

- `ToHexString()`: Convert bytes to hex string
- `FromHexString()`: Convert hex string to bytes
- `ToUtf8String()`: Convert bytes to UTF-8 string
- `ToUtf8Bytes()`: Convert string to UTF-8 bytes
- `PadToLength()`: Pad byte array to specified length
- `TrimPadding()`: Remove padding bytes
- `CalculateCRC16()`: Calculate CRC16 checksum
- `FormatBytes()`: Format bytes in various formats

## Testing

The library includes comprehensive unit tests that can run without NFC hardware:

```bash
cd Tests
dotnet test
```

Tests use mocking to simulate NFC operations, making them suitable for CI/CD pipelines.

## Examples

See the `Examples/` directory for complete working examples:

- **Basic Example**: Simple card reading and monitoring
- **Advanced Example**: Complex operations like block reading/writing

Run examples:

```bash
cd Examples
dotnet run
```

## Error Handling

The library provides comprehensive error handling:

```csharp
try
{
    var uid = await nfcReader.GetCardUIDAsync();
    if (uid != null)
    {
        Console.WriteLine($"Card UID: {uid}");
    }
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("Reader not connected");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Logging

The library supports structured logging via Microsoft.Extensions.Logging:

```csharp
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger<Program>();
var nfcReader = new NFCReader(logger);
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Migration from Old Library

If you're migrating from the old library, here are the key changes:

### Old Code:
```csharp
var reader = new NFCReader();
reader.Connect();
var uid = reader.GetCardUID();
```

### New Code:
```csharp
using var reader = new NFCReader();
await reader.ConnectAsync();
var uid = await reader.GetCardUIDAsync();
```

### Key Differences:
- All methods are now async
- Proper resource disposal with `using` statements
- Event-driven card detection instead of polling
- Better error handling and logging
- Cross-platform compatibility

## Troubleshooting

### Common Issues

1. **"No readers found"**
   - Ensure PC/SC service is running
   - Check if NFC reader is properly connected
   - Verify drivers are installed

2. **"Connection failed"**
   - Reader may be in use by another application
   - Check reader permissions
   - Restart PC/SC service

3. **"Card not responding"**
   - Ensure card is properly positioned
   - Check if card is compatible
   - Verify card is not damaged

### Platform-specific Issues

**Windows:**
- Ensure Windows Smart Card service is running
- Check Device Manager for reader status

**macOS:**
- Install PCSC-Lite: `brew install pcsc-lite`
- Start service: `brew services start pcsc-lite`

**Linux:**
- Install PCSC-Lite: `sudo apt-get install pcscd`
- Start service: `sudo systemctl start pcscd`

## Support

For issues and questions:
- Check the [Issues](https://github.com/yourusername/nfc-reader/issues) page
- Review the examples and tests
- Ensure your system meets the requirements

## Changelog

### Version 2.0.0
- Complete rewrite for modern .NET
- Cross-platform support
- Async/await support
- Event-driven architecture
- Comprehensive testing
- Better error handling and logging
