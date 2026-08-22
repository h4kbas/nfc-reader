using System.Threading;
using Microsoft.Extensions.Logging;
using NFCReader.Interfaces;
using NFCReader.Models;
using NFCReader.Utils;
using NfcReader = NFCReader.NFCReader;

namespace NFCReader.Examples;

/// <summary>
/// Example console application demonstrating NFC library usage
/// </summary>
public class Program
{
    private static readonly object ConsoleLock = new();

    public static async Task Main(string[] args)
    {
        Out("NFC Reader Library Demo");
        Out("=======================");
        Out();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        var logger = loggerFactory.CreateLogger<NfcReader>();

        using var exitCts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            exitCts.Cancel();
        };

        var cardReady = new ManualResetEventSlim(false);
        var waitingMessageShown = false;

        try
        {
            using var nfcReader = new NfcReader(logger);

            var readers = (await nfcReader.GetReadersAsync(exitCts.Token)).ToList();
            if (!readers.Any())
            {
                Out("No NFC reader found.");
                return;
            }

            Out("GetReadersAsync():");
            foreach (var reader in readers)
                Out($"  {reader.DisplayName}  state={reader.State}  available={reader.IsAvailable}");

            nfcReader.CardInserted += (_, card) =>
            {
                Out();
                Out("CardInserted event");
                Out($"  NFCCard.FormattedUID: {card.FormattedUID}");
                Out($"  NFCCard.FormattedATR: {card.FormattedATR}");
                Out();
                waitingMessageShown = false;
                cardReady.Set();
            };

            nfcReader.CardRemoved += (_, card) =>
            {
                Out();
                Out("CardRemoved event");
                Out($"  NFCCard.FormattedUID: {card.FormattedUID}");
                Out();
                cardReady.Reset();
            };

            nfcReader.ReaderStateChanged += (_, info) =>
            {
                Out($"ReaderStateChanged: {info.DisplayName} -> {info.State}");
            };

            await nfcReader.StartMonitoringAsync(exitCts.Token);
            Out("StartMonitoringAsync() active. Tap card. Ctrl+C to exit.");
            Out();

            while (!exitCts.Token.IsCancellationRequested)
            {
                if (!nfcReader.IsConnected)
                {
                    if (!waitingMessageShown)
                    {
                        Out("Waiting for card...");
                        waitingMessageShown = true;
                    }

                    cardReady.Wait(500);
                    continue;
                }

                CardDemoMenu.Print();
                Prompt("> ");
                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    exitCts.Cancel();
                    break;
                }

                if (!nfcReader.IsConnected)
                {
                    Out("Not connected. Tap card again.");
                    continue;
                }

                await CardDemoMenu.RunAsync(nfcReader, input);
                Out();
            }

            await nfcReader.StopMonitoringAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo error");
            Out($"Error: {ex.Message}");
        }
    }

    internal static void Out(string text = "")
    {
        lock (ConsoleLock)
        {
            Console.WriteLine(text);
        }
    }

    internal static void Prompt(string text)
    {
        lock (ConsoleLock)
        {
            Console.Write(text);
        }
    }
}

internal static class CardDemoMenu
{
    public static void Print()
    {
        Program.Out("--- Library API demo ---");
        Program.Out("1  GetCardUIDAsync()");
        Program.Out("2  ReadBlockAsync(block)");
        Program.Out("3  WriteBlockAsync(block, data)");
        Program.Out("4  AuthenticateBlockAsync(block)");
        Program.Out("5  TransmitAsync(APDUCommand.GetUID)");
        Program.Out("6  CurrentCard + ReaderInfo");
        Program.Out("7  Full walkthrough (all high-level APIs)");
        Program.Out("q  Quit");
    }

    public static async Task RunAsync(INFCReader nfcReader, string choice)
    {
        switch (choice)
        {
            case "1":
                await DemoGetCardUidAsync(nfcReader);
                break;
            case "2":
                await DemoReadBlockAsync(nfcReader);
                break;
            case "3":
                await DemoWriteBlockAsync(nfcReader);
                break;
            case "4":
                await DemoAuthenticateBlockAsync(nfcReader);
                break;
            case "5":
                await DemoTransmitGetUidAsync(nfcReader);
                break;
            case "6":
                DemoCardAndReaderInfo(nfcReader);
                break;
            case "7":
                await DemoFullWalkthroughAsync(nfcReader);
                break;
            default:
                Program.Out("Unknown option.");
                break;
        }
    }

    private static async Task DemoGetCardUidAsync(INFCReader nfcReader)
    {
        var uid = await nfcReader.GetCardUIDAsync();
        Program.Out(string.IsNullOrEmpty(uid)
            ? "GetCardUIDAsync() returned null."
            : $"GetCardUIDAsync() -> {uid}");
    }

    private static async Task DemoReadBlockAsync(INFCReader nfcReader)
    {
        if (!TryReadBlockNumber(out var block))
            return;

        var data = await nfcReader.ReadBlockAsync(block);
        if (data == null)
        {
            Program.Out("ReadBlockAsync() failed.");
            return;
        }

        Program.Out($"ReadBlockAsync({block}) hex: {data.ToHexString()}");
        Program.Out($"ReadBlockAsync({block}) utf8: {data.ToUtf8String()}");
    }

    private static async Task DemoWriteBlockAsync(INFCReader nfcReader)
    {
        if (!TryReadBlockNumber(out var block))
            return;

        Program.Prompt("Text (16 bytes max): ");
        var text = Console.ReadLine() ?? string.Empty;
        if (text.Length > 16)
            text = text[..16];

        var payload = text.ToUtf8Bytes().PadToLength(16);
        var ok = await nfcReader.WriteBlockAsync(block, payload);
        Program.Out(ok ? $"WriteBlockAsync({block}) OK." : $"WriteBlockAsync({block}) failed.");

        if (ok)
        {
            var readBack = await nfcReader.ReadBlockAsync(block);
            if (readBack != null)
                Program.Out($"ReadBlockAsync verify: {readBack.TrimPadding().ToUtf8String()}");
        }
    }

    private static async Task DemoAuthenticateBlockAsync(INFCReader nfcReader)
    {
        if (!TryReadBlockNumber(out var block))
            return;

        var ok = await nfcReader.AuthenticateBlockAsync(block);
        Program.Out(ok
            ? $"AuthenticateBlockAsync({block}) OK (Key A 0x60, default MIFARE key)."
            : $"AuthenticateBlockAsync({block}) failed.");
    }

    private static async Task DemoTransmitGetUidAsync(INFCReader nfcReader)
    {
        var response = await nfcReader.TransmitAsync(APDUCommand.GetUID);
        Program.Out($"TransmitAsync(APDUCommand.GetUID)");
        Program.Out($"  APDUResponse.IsSuccess: {response.IsSuccess}");
        Program.Out($"  APDUResponse.StatusDescription: {response.StatusDescription}");
        if (response.IsSuccess)
            Program.Out($"  APDUResponse.DataAsHex: {response.DataAsHex}");
    }

    private static void DemoCardAndReaderInfo(INFCReader nfcReader)
    {
        var card = nfcReader.CurrentCard;
        var reader = nfcReader.ReaderInfo;

        Program.Out($"IsConnected: {nfcReader.IsConnected}");
        Program.Out($"ReaderInfo.DisplayName: {reader.DisplayName}");
        Program.Out($"ReaderInfo.State: {reader.State}");

        if (card == null)
        {
            Program.Out("CurrentCard: null");
            return;
        }

        Program.Out($"CurrentCard.FormattedUID: {card.FormattedUID}");
        Program.Out($"CurrentCard.FormattedATR: {card.FormattedATR}");
        Program.Out($"CurrentCard.State: {card.State}");
    }

    private static async Task DemoFullWalkthroughAsync(INFCReader nfcReader)
    {
        Program.Out("-- Full walkthrough --");

        var uid = await nfcReader.GetCardUIDAsync();
        Program.Out($"GetCardUIDAsync() -> {uid ?? "(null)"}");

        const byte readBlock = 4;
        var blockData = await nfcReader.ReadBlockAsync(readBlock);
        if (blockData != null)
            Program.Out($"ReadBlockAsync({readBlock}) -> {blockData.ToHexString()}");
        else
            Program.Out($"ReadBlockAsync({readBlock}) failed.");

        const byte writeBlock = 5;
        var authed = await nfcReader.AuthenticateBlockAsync(writeBlock);
        Program.Out($"AuthenticateBlockAsync({writeBlock}) -> {authed}");

        var demoText = "Hello NFC!".ToUtf8Bytes().PadToLength(16);
        var written = await nfcReader.WriteBlockAsync(writeBlock, demoText);
        Program.Out($"WriteBlockAsync({writeBlock}, \"Hello NFC!\") -> {written}");

        if (written)
        {
            var readBack = await nfcReader.ReadBlockAsync(writeBlock);
            if (readBack != null)
                Program.Out($"ReadBlockAsync({writeBlock}) verify -> {readBack.TrimPadding().ToUtf8String()}");
        }

        var apduResponse = await nfcReader.TransmitAsync(APDUCommand.GetUID);
        Program.Out($"TransmitAsync(APDUCommand.GetUID) -> {apduResponse.StatusDescription}  data={apduResponse.DataAsHex}");

        DemoCardAndReaderInfo(nfcReader);
    }

    private static bool TryReadBlockNumber(out byte block)
    {
        block = 0;
        Program.Prompt("Block number (0-255): ");
        var input = Console.ReadLine()?.Trim();
        if (!byte.TryParse(input, out block))
        {
            Program.Out("Invalid block.");
            return false;
        }

        return true;
    }
}
