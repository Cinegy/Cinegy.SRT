using System.Net;
using SrtSharp;

namespace Cinegy.Srt.Wrapper.CLI;

internal class Program
{
    public static void Main(string[] args)
    {
        var bannedMessages = new HashSet<string>
        {
            ": srt_accept: no pending connection available at the moment"
        };

        var options = new LoggerOptions
        {
            LogLevel = LogLevel.Notice,
            LogFlags = LogFlag.DisableTime | LogFlag.DisableEOL | LogFlag.DisableThreadName | LogFlag.DisableSeverity,
            LogMessageAction = (level, message, area, file, line) =>
            {
                if (bannedMessages.Contains(message)) return;
                var verbalLevel = level switch
                {
                    var x when x == LogLevel.Debug => "Debug",
                    var x when x == LogLevel.Notice => "Info",
                    var x when x == LogLevel.Warning => "Warning",
                    var x when x == LogLevel.Error => "Error",
                    var x when x == LogLevel.Critical => "Critical",
                    _ => "Trace"
                };
                Console.WriteLine($"{verbalLevel}: {area}: {message.Trim(':')}");
            }
        };
        using var srt = SecureReliableTransport.Setup(options);
        using var sender = srt.CreateBroadcaster(new IPEndPoint(IPAddress.Any, 9000));

        var dummy = new byte[100];
        var tokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, args) =>
        {
            Console.WriteLine("Cancelling...");
            args.Cancel = true;
            tokenSource.Cancel();
        };

        while (!tokenSource.IsCancellationRequested)
        {
            Console.WriteLine($"Listening address: {sender.Address}");
            Console.WriteLine("Connected clients:");
            if (sender.ConnectedClients.Any())
            {
                foreach (var connectedClient in sender.ConnectedClients)
                {
                    Console.WriteLine($"- {connectedClient}");
                }
            }
            else
            {
                Console.WriteLine("- None");
            }

            Console.ReadLine();
            sender.Broadcast(dummy);
        }
    }
}
