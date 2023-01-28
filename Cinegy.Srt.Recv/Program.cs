//  Copyright 2019-2023 Cinegy GmbH
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using CommandLine;
using SrtSharp;

namespace Cinegy.Srt.Recv;

internal class Program
{
    private const int DEFAULT_CHUNK = 1328; //sized to be able to cope with RTP headers if present

    public static int Main(string[] args)
    {
        var arguments = Parser.Default.ParseArguments<Options>(args);
        var result = arguments.MapResult(Run, _ => -1);

        Console.WriteLine();
        Console.WriteLine("Press enter to exit");
        Console.ReadLine();

        return result;
    }

    internal static int Run(Options opts)
    {
        // Initialize UDP client
        var outputIp = opts.OutputAdapterAddress != null
            ? IPAddress.Parse(opts.OutputAdapterAddress)
            : IPAddress.Any;

        Console.WriteLine($"Outputting multicast data to {opts.MulticastAddress}:{opts.MulticastPort} via adapter {outputIp}");

        using var updClient = new UdpClient();
        updClient.ExclusiveAddressUse = false;
        updClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        updClient.Client.Bind(new IPEndPoint(outputIp, opts.MulticastPort));
        updClient.Connect(IPAddress.Parse(opts.MulticastAddress), opts.MulticastPort);

        // Initialize srt
        srt.srt_startup();

        var srtHandle = srt.srt_create_socket();
        var socketAddress = SocketHelper.CreateSocketAddress(opts.SrtAddress, opts.SrtPort);
        srt.srt_connect(srtHandle, socketAddress, Marshal.SizeOf(typeof(sockaddr_in)));

        Console.WriteLine($"Requesting SRT Transport Stream on srt://@{opts.SrtAddress}:{opts.SrtPort}");

        // Start network receiving thread
        // ReSharper disable once AccessToDisposedClosure
        var ts = new ThreadStart(() => NetworkReceiverThread(srtHandle, updClient));
        var receiverThread = new Thread(ts)
        {
            Priority = ThreadPriority.Highest
        };
        receiverThread.Start();

        while (receiverThread.IsAlive)
        {
            Thread.Sleep(10);
        }

        Console.WriteLine("Closing SRT Receiver...");

        srt.srt_close(srtHandle);
        srt.srt_cleanup();

        return 0;
    }

    private static void NetworkReceiverThread(int srtHandle, UdpClient udpClient)
    {
        var tokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, args) =>
        {
            Console.WriteLine("Cancelling...");
            args.Cancel = true;
            tokenSource.Cancel();
        };

        DateTime? statisticShowTime = null;

        var buffer = ArrayPool<byte>.Shared.Rent(DEFAULT_CHUNK * 2);
        while (!tokenSource.IsCancellationRequested)
        {
            var stat = srt.srt_recvmsg(srtHandle, buffer, DEFAULT_CHUNK);
            if (stat == srt.SRT_ERROR)
            {
                Console.WriteLine($"Error in reading loop: {srt.srt_getlasterror_str()}");
                return;
            }

            if (statisticShowTime == null)
            {
                Console.WriteLine("Started receiving SRT packets...");
            }

            try
            {
                if (statisticShowTime == null || DateTime.UtcNow - statisticShowTime > TimeSpan.FromSeconds(1))
                {
                    using var perf = new CBytePerfMon();
                    srt.srt_bistats(srtHandle, perf, 0, 1);
                    Console.Clear();

                    var jsonStats = JsonSerializer.Serialize(perf);
                    Console.WriteLine(jsonStats);

                    statisticShowTime = DateTime.UtcNow;
                }

                udpClient.Send(buffer, stat);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Unhandled exception within network receiver: {ex.Message}");
                return;
            }
        }
    }
}
