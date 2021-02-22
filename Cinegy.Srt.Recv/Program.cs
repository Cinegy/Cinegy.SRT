/*   Copyright 2019 Cinegy GmbH

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

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using SrtSharp;

namespace Cinegy.Srt.Recv
{
    internal unsafe class Program
    {   
        // SRT related constant values - probably should end up wrapped within the SrtSharp library itself
        private const int AF_INET = 2;
        private const int SOCK_DGRAM = 2; /* datagram socket */
        private const int DEFAULT_CHUNK = 1328;

        private static UdpClient _udpClient;
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
            if (_srtHandle <= 0) return;
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
            PrepareOutputUdpClient(opts.OutputAdapterAddress, opts.MulticastAddress, opts.MulticastPort);            
            srt.srt_startup();
            var destination = IPAddress.Parse(opts.SrtAddress);
            _srtHandle = srt.srt_socket(AF_INET, SOCK_DGRAM, 0);

            var sin = new sockaddr_in()
            {
                sin_family = AF_INET,
                sin_port = (ushort)IPAddress.HostToNetworkOrder((short)opts.SrtPort),
#pragma warning disable 618
                sin_addr = (uint)destination.Address,
#pragma warning restore 618
                sin_zero = 0
            };

            var hnd = GCHandle.Alloc(sin, GCHandleType.Pinned);
            var socketAddress = new SWIGTYPE_p_sockaddr(hnd.AddrOfPinnedObject(), false);
            srt.srt_connect(_srtHandle, socketAddress, sizeof(SrtSharp.sockaddr_in));
            Console.WriteLine($"Requesting SRT Transport Stream on srt://@{opts.SrtAddress}:{opts.SrtPort}");
            var ts = new ThreadStart(ReceivingNetworkWorkerThread);
            var receiverThread = new Thread(ts) { Priority = ThreadPriority.Highest };
            receiverThread.Start();

            while (!_pendingExit)
            {
                Thread.Sleep(10);
            }
                      
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();

            return 0;
        }
        
        private static void ReceivingNetworkWorkerThread()
        {
            var buf = new byte[DEFAULT_CHUNK * 2];
            while (!_pendingExit)
            {
                var stat = srt.srt_recvmsg(_srtHandle, buf, DEFAULT_CHUNK);

                if (stat == srt.SRT_ERROR)
                {
                    _pendingExit = true;
                    Console.WriteLine($"Error in reading loop.");
                }
                else
                {
                    if (!_packetsStarted)
                    {
                        Console.WriteLine("Started receiving SRT packets...");
                        _packetsStarted = true;
                    }
                    try
                    {
                        _udpClient.Send(buf, stat);
                        Console.Write(".");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"Unhandled exception within network receiver: {ex.Message}");
                        return;
                    }
                }
            }

            Console.WriteLine("Closing SRT Receiver");
            srt.srt_close(_srtHandle);
            srt.srt_cleanup();
            _srtHandle = -1;
            Environment.Exit(0);
        }
        
        private static void PrepareOutputUdpClient(string adapterAddress, string multicastAddress, int multicastGroup)
        {
            var outputIp = adapterAddress != null ? IPAddress.Parse(adapterAddress) : IPAddress.Any;
            Console.WriteLine($"Outputting multicast data to {multicastAddress}:{multicastGroup} via adapter {outputIp}");

            _udpClient = new UdpClient { ExclusiveAddressUse = false };
            var localEp = new IPEndPoint(outputIp, multicastGroup);

            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.ExclusiveAddressUse = false;
            _udpClient.Client.Bind(localEp);

            var parsedMulticastAddress = IPAddress.Parse(multicastAddress);
            _udpClient.Connect(parsedMulticastAddress, multicastGroup);
        }      
    }
}