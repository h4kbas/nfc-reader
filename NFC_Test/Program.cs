using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFCReader;
using System.Linq;

namespace NFC_Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NFC Reader Library Test");
            Console.WriteLine("=======================");

            try
            {
                // Create a logger factory
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                var logger = loggerFactory.CreateLogger<NFCReader.NFCReader>();

                // Create an NFC reader instance
                Console.WriteLine("Creating NFC reader instance...");
                var nfcReader = new NFCReader.NFCReader(logger);

                // Test getting available readers
                Console.WriteLine("Getting available readers...");
                var readers = await nfcReader.GetReadersAsync();
                
                if (readers != null)
                {
                    var readerList = readers.ToList();
                    if (readerList.Count > 0)
                    {
                        Console.WriteLine($"Found {readerList.Count} reader(s):");
                        foreach (var reader in readerList)
                        {
                            Console.WriteLine($"  - {reader.Name} (State: {reader.State})");
                        }

                        // Try to connect to the first available reader
                        var firstReader = readerList[0];
                        Console.WriteLine($"Attempting to connect to reader: {firstReader.Name}");
                        
                        var connected = await nfcReader.ConnectAsync(firstReader.Name);
                        if (connected)
                        {
                            Console.WriteLine("Successfully connected to reader!");
                            
                            // Test getting card UID
                            Console.WriteLine("Attempting to get card UID...");
                            var cardUID = await nfcReader.GetCardUIDAsync();
                            if (!string.IsNullOrEmpty(cardUID))
                            {
                                Console.WriteLine($"Card UID: {cardUID}");
                            }
                            else
                            {
                                Console.WriteLine("No card detected or failed to read UID");
                            }

                            // Disconnect
                            await nfcReader.DisconnectAsync();
                            Console.WriteLine("Disconnected from reader");
                        }
                        else
                        {
                            Console.WriteLine("Failed to connect to reader");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No NFC readers found");
                    }
                }
                else
                {
                    Console.WriteLine("No NFC readers found");
                }

                Console.WriteLine("All tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during testing: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
