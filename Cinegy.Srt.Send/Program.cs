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

    internal static void BackgroundThread(Options opts, UdpClient updClient)
    {
        try
        {
            var tokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (_, args) =>
            {
                Console.WriteLine("Cancelling...");
                args.Cancel = true;
                tokenSource.Cancel();
            };

            // This method is expected to return new accepted SRT socket on each iteration
            foreach (var socket in SrtAcquireAcceptedSocket(opts, tokenSource.Token))
            {
                // Ensure the accepted socket is properly disposed of after use
                using var acceptedSocket = socket;

                IPEndPoint receivedFromEndPoint = null;

                while (!tokenSource.IsCancellationRequested)
                {
                    // Receive data from the UDP client
                    var data = updClient.Receive(ref receivedFromEndPoint);

                    // Send the received data to the accepted SRT socket
                    var sndResult = srt.srt_send(acceptedSocket, data, data.Length);

                    // Check if there was an error sending the data
                    if (sndResult == srt.SRT_ERROR)
                    {
                        // If so, print the error message to the console and break out of the loop
                        Console.WriteLine($"Failed to send data to receiver: {srt.srt_getlasterror_str()}");
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    internal static int Run(Options opts)
    {
        SrtSetup();

        using var updClient = UpdSetup(opts);

        var receiverThread = new Thread(() => BackgroundThread(opts, updClient))
        {
            Priority = ThreadPriority.Highest
        };
        receiverThread.Start();

        while (receiverThread.IsAlive)
        {
            Thread.Sleep(10);
        }

        SrtTearDown();

        return 0;
    }

    private static SRTSOCKET SrtAcceptSocketInNonBlockingMode(CancellationToken token, SRTSOCKET socket)
    {
        while (!token.IsCancellationRequested)
        {
            // Accept an incoming connection on the SRT socket
            // Parameters:
            // - socket: the SRT socket to accept the connection on
            // - out var acceptedEndPoint: the remote endpoint of the accepted connection
            var acceptedSocket = srt.srt_accept(socket, out var acceptedEndPoint);

            // Check if the accepted socket is valid
            if (acceptedSocket.IsValid)
            {
                Console.WriteLine($"SRT client connected to a {acceptedEndPoint}");

                // Return the accepted socket
                return acceptedSocket;
            }

            // If the accepted socket is not valid, get the error code and system error code for the last error that occurred
            var systemErrorCode = 0;
            var errorCode = srt.srt_getlasterror(ref systemErrorCode);

            // Check if the error code is not EASYNCRCV (data not ready)
            if (errorCode is not SRT_ERRNO.SRT_EASYNCRCV)
            {
                // If another error occurred, throw an exception with the error message
                throw new InvalidOperationException($"Receive data from the SRT stream failed: {srt.srt_getlasterror_str()}");
            }

            // Sleep for 10 milliseconds to give time for the next connection attempt
            Thread.Sleep(10);
        }

        return SRTSOCKET.NotInitialized;
    }

    private static IEnumerable<SRTSOCKET> SrtAcquireAcceptedSocket(Options options, CancellationToken token)
    {
        // Create a new SRT socket and validate its creation
        using var socket = srt.srt_create_socket().Validate();

        // Define a binding endpoint using the provided SRT port and bind the socket to it
        var bindingEndPoint = new IPEndPoint(IPAddress.Any, options.SrtPort);
        srt.srt_bind(socket, bindingEndPoint).Validate("Binding listen socket");

        // Define a callback function that will be called when a new connection is accepted
        void ListenCallback(IntPtr opaque, int ns, int version, IPEndPoint address, string id)
        {
            Console.WriteLine($"CALLBACK: SOCKET for stream {address} with '{id}' was accepted");

            // Optionally set the passphrase for the accepted socket
            // srt.srt_setsockflag_string(ns, SRT_STRING_SOCKOPT.SRTO_STRING_PASSPHRASE, "some password");
        }

        // Set the listening callback function for the SRT socket
        // Parameters:
        // - socket: the SRT socket to set the callback for
        // - ListenCallback: the callback function to be called when a new connection is accepted
        // - IntPtr.Zero: a null pointer, specifying the user data parameter (no user data will be passed)
        srt.srt_listen_callback(socket, ListenCallback, IntPtr.Zero).Validate("Setting listen callback");

        // Set the socket to listen for incoming connections with a backlog of 5
        // Parameters:
        // - socket: the SRT socket to start listening on
        // - 5: the maximum number of pending connections in the backlog
        srt.srt_listen(socket, 5).Validate("Setting listen backlog");

        Console.WriteLine($"SRT client connected {bindingEndPoint}");
        Console.WriteLine($"Waiting for SRT connection on {options.SrtPort}");

        while (!token.IsCancellationRequested)
        {
            SRTSOCKET acceptedSocket;
            // Check if non-blocking mode is enabled
            if (options.NonBlockingMode)
            {
                // If so, accept the socket asynchronously using a custom method
                acceptedSocket = SrtAcceptSocketInNonBlockingMode(token, socket);
            }
            else
            {
                // Otherwise, accept the socket in blocking mode
                acceptedSocket = srt.srt_accept(socket, out var acceptedEndPoint).Validate();

                Console.WriteLine($"SRT client connected to a {acceptedEndPoint}");
            }

            // Validate the accepted socket
            acceptedSocket.Validate();

            // Define a callback function that will be called when the connection is closed
            void ConnectCallback(IntPtr opaque, int ns, SRT_ERRNO code, IPEndPoint address, int i)
            {
                Console.WriteLine($"CALLBACK: SOCKET for {address} closed with error: {code}");
            }

            // Set the connect callback function for the accepted SRT socket
            // Parameters:
            // - acceptedSocket: the SRT socket to set the callback for
            // - ConnectCallback: the callback function to be called when the connection is closed
            // - IntPtr.Zero: a null pointer, specifying the user data parameter (no user data will be passed)
            srt.srt_connect_callback(acceptedSocket, ConnectCallback, IntPtr.Zero);

            // Return the accepted SRT socket
            yield return acceptedSocket;
        }
    }

    private static void SrtSetup()
    {
        // Initialize the SRT library before using any SRT functions
        srt.srt_startup();

        // Set the logging flags: disable timestamps and thread names in log messages
        srt.srt_setlogflags(LogFlag.DisableTime | LogFlag.DisableThreadName);

        // Set the logging level to 'Notice', enabling log messages of Notice level and above
        srt.srt_setloglevel(LogLevel.Notice);

        // Add the following functional areas to the logging process:
        // Sending API: log messages related to the sending API calls and operations
        srt.srt_addlogfa(LogFunctionalArea.SendingApi);

        // Sending Buffer: log messages related to the internal sending buffer management
        srt.srt_addlogfa(LogFunctionalArea.SendingBuffer);

        // Sending Channel: log messages related to the channel used for sending data
        srt.srt_addlogfa(LogFunctionalArea.SendingChannel);

        // Sending Group: log messages related to the group management in SRT for sending data
        srt.srt_addlogfa(LogFunctionalArea.SendingGroup);

        // Sending Queue: log messages related to the internal queue used for managing sent data
        srt.srt_addlogfa(LogFunctionalArea.SendingQueue);

        // Define a custom log handler function to process log messages generated by the SRT library
        void LogHandler(IntPtr opaque, int level, string file, int line, string area, string message)
        {
            // Write the log message content to the console
            Console.WriteLine(message);
        }

        // Set the custom log handler function for the SRT library
        // IntPtr.Zero: a null pointer, specifying the user data parameter (no user data will be passed)
        // LogHandler: the custom log handler function
        srt.srt_setloghandler(IntPtr.Zero, LogHandler);
    }

    private static void SrtTearDown()
    {
        // Teardown the SRT library before application shutdown
        srt.srt_cleanup();
    }

    private static UdpClient UpdSetup(Options opts)
    {
        var updClient = new UdpClient();

        if (!IPAddress.TryParse(opts.InputAdapterAddress, out var inputAdapterAddress))
        {
            inputAdapterAddress = IPAddress.Any;
        }

        Console.WriteLine($"Listening for multicast on udp://@{opts.MulticastAddress}:{opts.MulticastPort}");
        updClient.ExclusiveAddressUse = false;
        updClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        updClient.Client.ReceiveBufferSize = 1500 * 3000;
        updClient.Client.Bind(new IPEndPoint(IPAddress.Any, opts.MulticastPort));
        updClient.JoinMulticastGroup(IPAddress.Parse(opts.MulticastAddress), inputAdapterAddress);
        return updClient;
    }
}
