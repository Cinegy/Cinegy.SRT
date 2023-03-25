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
using CommandLine;
using SrtSharp;

// ReSharper disable AccessToDisposedClosure

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
        SrtSetup();

        using var updClient = UdpSetup(opts);

        var receiverThread = new Thread(() =>
        {
            try
            {
                foreach (var memory in SrtReceiveData(opts))
                {
                    updClient.Send(memory.Span);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        })
        {
            Priority = ThreadPriority.Highest,
        };
        receiverThread.Start();

        while (receiverThread.IsAlive)
        {
            Thread.Sleep(10);
        }

        SrtTearDown();

        return 0;
    }

    private static IEnumerable<Memory<byte>> SrtReceiveData(Options opts)
    {
        var tokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, args) =>
        {
            Console.WriteLine("Cancelling...");
            args.Cancel = true;
            tokenSource.Cancel();
        };

        var srtEndpoint = new IPEndPoint(IPAddress.Parse(opts.SrtAddress), opts.SrtPort);
        Console.WriteLine($"Requesting SRT Transport Stream on srt://@{srtEndpoint}");

        // Create a new SRT socket and validate its creation
        using var socket = srt.srt_create_socket().Validate();

        // Check if the non-blocking mode is enabled (based on a provided option)
        if (opts.NonBlockingMode)
        {
            // Set the SRT socket to non-blocking mode by disabling the RCVSYN (receive synchronous) flag
            // Parameters:
            // - socket: the SRT socket to configure
            // - SRT_BOOL_SOCKOPT.SRTO_BOOL_RCVSYN: the socket option to set the RCVSYN flag
            // - false: the value to disable the RCVSYN flag
            srt.srt_setsockflag_bool(socket, SRT_BOOL_SOCKOPT.SRTO_BOOL_RCVSYN, false).Validate("SRT unblock socket");
        }

        // Connect the SRT socket to the remote endpoint (specified by srtEndpoint)
        // Parameters:
        // - socket: the SRT socket to connect
        // - srtEndpoint: the remote endpoint to connect the socket to
        srt.srt_connect(socket, srtEndpoint).Validate("SRT connect");

        var stats = new SRT_TRACEBSTATS();
        var buffer = new byte[DEFAULT_CHUNK];
        var srtMsgCtrl = new SRT_MSGCTRL();

        DateTime? statisticShowTime = null;
        var receiveStartTime = DateTime.UtcNow;
        while (!tokenSource.IsCancellationRequested)
        {
            var elapsed = DateTime.UtcNow - receiveStartTime;
            // Receive data from the SRT socket into a buffer using srt_recvmsg2 function
            // Parameters:
            // - socket: the SRT socket to receive data from
            // - buffer: the byte array to store the received data
            // - buffer.Length: the maximum number of bytes to read into the buffer
            // - srtMsgCtrl: an SRT_MSGCTRL structure that can be used to control message reception
            var size = srt.srt_recvmsg2(socket, buffer, buffer.Length, srtMsgCtrl);

            // Check if there was an error while receiving data
            if (size == srt.SRT_ERROR)
            {
                // Get the error code and system error code for the last error that occurred
                var systemErrorCode = 0;
                var errorCode = srt.srt_getlasterror(ref systemErrorCode);

                // Check if the error code indicates that data is not ready or the connection is not established yet
                if (errorCode is SRT_ERRNO.SRT_EASYNCRCV or SRT_ERRNO.SRT_ENOCONN)
                {
                    // Sleep for 10 milliseconds and then try again
                    Thread.Sleep(10);
                    continue;
                }

                // If another error occurred, throw an exception with the error message
                throw new InvalidOperationException($"Receive data from the SRT stream failed: {srt.srt_getlasterror_str()}");
            }

            // Check if it's the first time displaying statistics or if it's time to display them again
            // (every 5 seconds)
            if (statisticShowTime == null || DateTime.UtcNow - statisticShowTime > TimeSpan.FromSeconds(5))
            {
                // If it's the first time, print a message indicating that the reception of SRT packets has started
                if (statisticShowTime == null)
                {
                    Console.WriteLine("Started receiving SRT packets...");
                }

                // Get Secure Reliable Transport stream statistics for the specified socket
                // Parameters:
                // - socket: the SRT socket to get the statistics for
                // - stats: an SRT_TRACEBSTATS structure to store the statistics
                // - 0: the time interval for which the statistics should be calculated (0 means since the last call)
                // - 1: clear the statistics after retrieving them
                srt.srt_bistats(socket, stats, 0, 1);

                // Display the elapsed time, bandwidth, unique packets received, and lost packets
                Console.WriteLine($"{elapsed:c} ({stats.mbpsBandwidth:F} mbps) Unique: {stats.pktRecvUniqueTotal}, Lost: {stats.pktRcvLoss}");

                // Update the time the statistics were last shown
                statisticShowTime = DateTime.UtcNow;
            }

            // Yield the received data as a memory slice of the buffer
            yield return buffer.AsMemory(0, size);
        }
    }

    private static void SrtSetup()
    {
        // Initialize the SRT library before using any SRT functions
        srt.srt_startup();

        // Set the logging flags: disable timestamps in log messages
        srt.srt_setlogflags(LogFlag.DisableTime);

        // Set the logging level to 'Debug', enabling detailed log messages
        srt.srt_setloglevel(LogLevel.Debug);

        // Add the following functional areas to the logging process:
        // Receiving API: log messages related to the receiving API calls and operations
        srt.srt_addlogfa(LogFunctionalArea.ReceivingApi);

        // Receiving Buffer: log messages related to the internal receiving buffer management
        srt.srt_addlogfa(LogFunctionalArea.ReceivingBuffer);

        // Receiving Channel: log messages related to the channel used for receiving data
        srt.srt_addlogfa(LogFunctionalArea.ReceivingChannel);

        // Receiving Group: log messages related to the group management in SRT for receiving data
        srt.srt_addlogfa(LogFunctionalArea.ReceivingGroup);

        // Receiving Queue: log messages related to the internal queue used for managing received data
        srt.srt_addlogfa(LogFunctionalArea.ReceivingQueue);

        // LogHandler: a custom log handler function to process log messages generated by the SRT library
        // Parameters:
        // - IntPtr opaque: user data passed to the log handler function (in this case, it's null as specified by IntPtr.Zero when setting the log handler)
        // - int level: the log level of the message (e.g., LogLevel.Debug, LogLevel.Error, LogLevel.Warning)
        // - string file: the source file name where the log message was generated
        // - int line: the line number in the source file where the log message was generated
        // - string area: the functional area of the log message (e.g., LogFunctionalArea.ReceivingApi, LogFunctionalArea.ReceivingBuffer)
        // - string message: the log message content
        void LogHandler(IntPtr opaque, int level, string file, int line, string area, string message)
        {
            Console.WriteLine(message);
        }

        // Set a custom log handler function for the SRT library
        // IntPtr.Zero: a null pointer, specifying the user data parameter (no user data will be passed)
        // LogHandler: a callback function to handle log messages generated by the SRT library
        srt.srt_setloghandler(IntPtr.Zero, LogHandler);
    }

    private static void SrtTearDown()
    {
        // Teardown the SRT library before application shutdown
        srt.srt_cleanup();
    }

    private static UdpClient UdpSetup(Options opts)
    {
        // Setup UDP client
        if (!IPAddress.TryParse(opts.OutputAdapterAddress, out var outputIp))
        {
            outputIp = IPAddress.Any;
        }

        var multicastAdapterEndpoint = new IPEndPoint(outputIp, opts.MulticastPort);
        var multicastEndpoint = new IPEndPoint(IPAddress.Parse(opts.MulticastAddress), opts.MulticastPort);

        Console.WriteLine($"Outputting multicast data to {opts.MulticastAddress}:{opts.MulticastPort} via adapter {outputIp}");

        var updClient = new UdpClient();
        updClient.ExclusiveAddressUse = false;
        updClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        updClient.Client.Bind(multicastAdapterEndpoint);
        updClient.Connect(multicastEndpoint);

        Console.WriteLine($"Broadcasting Transport Stream to udp://@{multicastEndpoint}");
        return updClient;
    }
}
