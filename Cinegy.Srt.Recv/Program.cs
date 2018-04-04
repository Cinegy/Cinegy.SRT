/*   Copyright 2017 Cinegy GmbH

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
using CommandLine;

namespace Cinegy.Srt.Recv
{
    internal unsafe class Program
    {
        private static UdpClient _udpClient;

        private static SrtReceiver _srtRecvr = new SrtReceiver();
        private static bool _running = false;

        private static int Main(string[] args)
        {
            Console.CancelKeyPress += delegate {
                _running = false;
            };

            var result = Parser.Default.ParseArguments<Options>(args);

            return result.MapResult(
                Run,
                errs => CheckArgumentErrors());

        }

        ~Program()
        {
            _srtRecvr.Stop();
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

            _srtRecvr.SetHostname(opts.InputAdapterAddress);
            _srtRecvr.SetPort(opts.SrtPort);
            _srtRecvr.OnDataReceived += SrtRecvr_OnDataReceived;

            Console.WriteLine($"About to attempt to bind for listening as SRT TARGET on {_srtRecvr.GetHostname()}:{_srtRecvr.GetPort()}");

            PrepareOutputUdpClient(opts.OutputAdapterAddress, opts.MulticastAddress, opts.MulticastPort);

            _running = true;

            while (_running)
            {
                _srtRecvr.Run();
                Console.WriteLine("\nConnection closed, resetting...");
                System.Threading.Thread.Sleep(100);
            }

            return 0;
        }

        private static void SrtRecvr_OnDataReceived(sbyte* data, ulong dataSize)
        {
            if (dataSize < 1) return;
            var ptr = new IntPtr(data);
            var dataarr = new byte[dataSize];
            Marshal.Copy(ptr, dataarr, 0, dataarr.Length);
            _udpClient.Send(dataarr, dataarr.Length);
           // Console.Write(".");
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

            var parsedMcastAddr = IPAddress.Parse(multicastAddress);
            _udpClient.Connect(parsedMcastAddr, multicastGroup);
        }
        

    }
}
