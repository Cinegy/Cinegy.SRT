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

using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using CommandLine;
using SrtSharp;

namespace Cinegy.Srt.Send;

internal class Program
{
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
        var listenAddress = string.IsNullOrEmpty(opts.InputAdapterAddress)
            ? IPAddress.Any
            : IPAddress.Parse(opts.InputAdapterAddress);

        Console.WriteLine($"Listening for multicast on udp://@{opts.MulticastAddress}:{opts.MulticastPort}");

        using var updClient = new UdpClient();
        updClient.ExclusiveAddressUse = false;
        updClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        updClient.Client.ReceiveBufferSize = 1500 * 3000;
        updClient.Client.Bind(new IPEndPoint(IPAddress.Any, opts.MulticastPort));
        //updClient.JoinMulticastGroup(IPAddress.Parse(opts.MulticastAddress), listenAddress);

        // Initialize srt
        srt.srt_startup();

        var srtHandle = srt.srt_create_socket();
        var socketAddress = SocketHelper.CreateSocketAddress("0.0.0.0", opts.SrtPort);
        srt.srt_bind(srtHandle, socketAddress, Marshal.SizeOf(typeof(sockaddr_in)));
        srt.srt_listen(srtHandle, 5);

        Console.WriteLine($"SRT client connected {socketAddress}");
        Console.WriteLine($"Waiting for SRT connection on {opts.SrtPort}");

        var lenHnd = GCHandle.Alloc(Marshal.SizeOf(typeof(sockaddr_in)), GCHandleType.Pinned);
        var addressLenPtr = new SWIGTYPE_p_int(lenHnd.AddrOfPinnedObject(), false);
        lenHnd.Free();

        var srtSocketHandle = srt.srt_accept(srtHandle, socketAddress, addressLenPtr);
        Console.WriteLine("SRT client connected to a socket");

        // Start network receiving thread
        // ReSharper disable once AccessToDisposedClosure
        var ts = new ThreadStart(() => NetworkReceiverThread(srtSocketHandle, updClient));
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

    private static void NetworkReceiverThread(int newSocket, UdpClient client)
    {
        var tokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, args) =>
        {
            Console.WriteLine("Cancelling...");
            args.Cancel = true;
            tokenSource.Cancel();
        };

        IPEndPoint receivedFromEndPoint = null;

        while (!tokenSource.IsCancellationRequested)
        {
            var data = client.Receive(ref receivedFromEndPoint);
            try
            {
                var sndResult = srt.srt_send(newSocket, data, data.Length);
                if (sndResult == srt.SRT_ERROR)
                {
                    Console.WriteLine($"Error in reading loop: {srt.srt_getlasterror_str()}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception within network receiver: {ex.Message}");
                return;
            }
        }
    }
}
