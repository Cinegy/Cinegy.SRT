/*   Copyright 2019-2022 Cinegy GmbH

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using CommandLine;
using SrtSharp;

namespace Cinegy.Srt.Send
{
    internal unsafe class Program
    {   
        private static readonly UdpClient UdpClient = new() { ExclusiveAddressUse = false };
        private static int _srtHandle;
        private static bool _pendingExit;
        private static bool _packetsStarted;
        
        private static int Main(string[] args)
        {
            Console.CancelKeyPress += delegate {
                _pendingExit = true;
            };

            var result = Parser.Default.ParseArguments<Options>(args);

            return result.MapResult(
                Run,
                errs => CheckArgumentErrors());
        }

        ~Program()
        {
            if (_srtHandle == 0) return;
            srt.srt_close(_srtHandle);
            srt.srt_cleanup();
        }

        private static int CheckArgumentErrors()
        {
            //will print using library the appropriate help - now pause the console for the viewer
            Console.WriteLine("Hit enter to quit");
            Console.ReadLine();
            return -1;
        }

        private static int Run(Options opts)
        {   
            srt.srt_startup();

            _srtHandle = srt.srt_create_socket();

            var socketAddress = SocketHelper.CreateSocketAddress("0.0.0.0", opts.SrtPort);

            Console.WriteLine($"SRT client connected {socketAddress}");
            Console.WriteLine($"Waiting for SRT connection on {opts.SrtPort}");
            srt.srt_bind(_srtHandle, socketAddress, sizeof(sockaddr_in));
            srt.srt_listen(_srtHandle, 5);


            var lenHnd = GCHandle.Alloc(sizeof(sockaddr_in), GCHandleType.Pinned);
            var addressLenPtr = new SWIGTYPE_p_int(lenHnd.AddrOfPinnedObject(), false);
            lenHnd.Free();
            
            var newsocket = srt.srt_accept(_srtHandle, socketAddress, addressLenPtr);
            //srt.
            Console.WriteLine($"SRT client connected");

            StartListeningToNetwork(opts.MulticastAddress, opts.MulticastPort, opts.InputAdapterAddress, newsocket);

            while (!_pendingExit)
            {
                Thread.Sleep(10);
            }

            if (!opts.SuppressOutput)
            {
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }

            return 0;
        }
        
        private static void StartListeningToNetwork(string multicastAddress, int networkPort,
            string listenAdapter = "", int newSocket = 0)
        {
            var listenAddress = string.IsNullOrEmpty(listenAdapter) ? IPAddress.Any : IPAddress.Parse(listenAdapter);

            var localEp = new IPEndPoint(IPAddress.Any, networkPort);

            Console.WriteLine($"Listening for multicast on udp://@{multicastAddress}:{networkPort}");

            UdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            UdpClient.Client.ReceiveBufferSize = 1500 * 3000;
            UdpClient.Client.Bind(localEp);
            
            if (!string.IsNullOrWhiteSpace(multicastAddress))
            {
                var parsedMcastAddr = IPAddress.Parse(multicastAddress);
                UdpClient.JoinMulticastGroup(parsedMcastAddr, listenAddress);
            }

            var ts = new ThreadStart(delegate
            {
                ReceivingNetworkWorkerThread(UdpClient, newSocket);
            });

            var receiverThread = new Thread(ts) { Priority = ThreadPriority.Highest };

            receiverThread.Start();
        }


        private static void ReceivingNetworkWorkerThread(UdpClient client, int newSocket)
        {
            IPEndPoint? receivedFromEndPoint = null;

            while (!_pendingExit)
            {
                var data = client.Receive(ref receivedFromEndPoint);
                try
                {
                    var sndResult = srt.srt_send(newSocket, data, data.Length);
                    if (sndResult == srt.SRT_ERROR)
                    {
                        Console.WriteLine("Client ended connection, or connection broken.");
                        _pendingExit = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            srt.srt_close(_srtHandle);
            srt.srt_cleanup();
        }
        
    }
}