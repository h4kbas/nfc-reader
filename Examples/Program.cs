using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFCReader;
using NFCReader.Interfaces;
using NFCReader.Models;
using NFCReader.Utils;

namespace NFCReader.Examples;

/// <summary>
/// Example console application demonstrating NFC library usage
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("NFC Reader Library Example");
        Console.WriteLine("==========================");
        Console.WriteLine();

        // Create logger
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<NFCReader>();

        try
        {
            // Create NFC reader instance
            using var nfcReader = new NFCReader(logger);

            // Get available readers
            Console.WriteLine("Scanning for available readers...");
            var readers = await nfcReader.GetReadersAsync();
            
            if (!readers.Any())
            {
                Console.WriteLine("No NFC readers found. Please ensure you have an NFC reader connected.");
                return;
            }

            Console.WriteLine($"Found {readers.Count()} reader(s):");
            foreach (var reader in readers)
            {
                Console.WriteLine($"  - {reader.DisplayName} (State: {reader.State})");
            }
            Console.WriteLine();

            // Connect to the first available reader
            var firstReader = readers.FirstOrDefault(r => r.IsAvailable);
            if (firstReader == null)
            {
                Console.WriteLine("No available readers found.");
                return;
            }

            Console.WriteLine($"Connecting to {firstReader.DisplayName}...");
            var connected = await nfcReader.ConnectAsync(firstReader.Name);
            
            if (!connected)
            {
                Console.WriteLine("Failed to connect to reader.");
                return;
            }

            Console.WriteLine("Successfully connected to reader!");
            Console.WriteLine();

            // Set up event handlers
            nfcReader.CardInserted += (sender, card) =>
            {
                Console.WriteLine($"Card inserted: {card.FormattedUID}");
                Console.WriteLine($"ATR: {card.FormattedATR}");
            };

            nfcReader.CardRemoved += (sender, card) =>
            {
                Console.WriteLine($"Card removed: {card.FormattedUID}");
            };

            nfcReader.ReaderStateChanged += (sender, readerInfo) =>
            {
                Console.WriteLine($"Reader state changed: {readerInfo.State}");
            };

            // Start monitoring for card changes
            Console.WriteLine("Starting card monitoring...");
            await nfcReader.StartMonitoringAsync();

            Console.WriteLine("Place an NFC card on the reader to test...");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            // Stop monitoring
            await nfcReader.StopMonitoringAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running the example");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

/// <summary>
/// Example of advanced NFC operations
/// </summary>
public class AdvancedExample
{
    public static async Task RunExample(INFCReader nfcReader)
    {
        Console.WriteLine("Advanced NFC Operations Example");
        Console.WriteLine("===============================");
        Console.WriteLine();

        try
        {
            // Example: Read card UID
            var uid = await nfcReader.GetCardUIDAsync();
            if (!string.IsNullOrEmpty(uid))
            {
                Console.WriteLine($"Card UID: {uid}");
                Console.WriteLine($"Formatted UID: {uid.Chunk(2).Select(chunk => new string(chunk)).Aggregate((a, b) => $"{a}:{b}")}");
            }

            // Example: Read a block
            var blockData = await nfcReader.ReadBlockAsync(4);
            if (blockData != null)
            {
                Console.WriteLine($"Block 4 data: {blockData.ToHexString()}");
                Console.WriteLine($"As string: {blockData.ToUtf8String()}");
            }

            // Example: Write data to a block
            var testData = "Hello NFC!".ToUtf8Bytes();
            var paddedData = testData.PadToLength(16);
            
            var writeSuccess = await nfcReader.WriteBlockAsync(5, paddedData);
            if (writeSuccess)
            {
                Console.WriteLine("Successfully wrote data to block 5");
                
                // Read it back to verify
                var readBackData = await nfcReader.ReadBlockAsync(5);
                if (readBackData != null)
                {
                    var trimmedData = readBackData.TrimPadding();
                    Console.WriteLine($"Read back: {trimmedData.ToUtf8String()}");
                }
            }

            // Example: Custom APDU command
            var customCommand = new APDUCommand
            {
                CLA = 0xFF,
                INS = 0xCA,
                P1 = 0x00,
                P2 = 0x00,
                Le = 0x00
            };

            var response = await nfcReader.TransmitAsync(customCommand);
            Console.WriteLine($"Custom command response: {response.StatusDescription}");
            if (response.IsSuccess)
            {
                Console.WriteLine($"Response data: {response.DataAsHex}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in advanced example: {ex.Message}");
        }
    }
}
