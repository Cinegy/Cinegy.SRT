﻿/*   Copyright 2019 Cinegy GmbH

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

using Cinegy.Telemetry;
using Cinegy.TsAnalysis;
using Cinegy.TsDecoder.TransportStream;
using Cinegy.TtxDecoder.Teletext;
using CommandLine;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Cinegy.Srt.StreamAnalyser
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal unsafe class Program
    {   private static Options _options;
        private static Logger _logger;
        private static Analyser _analyser;
        private static readonly DateTime StartTime = DateTime.UtcNow;
        private static bool _pendingExit;
        private static readonly StringBuilder ConsoleDisplay = new StringBuilder(1024);
        private static int _lastPrintedTsCount;
        private static SrtReceiver _srtReceiver = new SrtReceiver();
        private static Thread _receiverThread;

        static int Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return RunStreamInteractive();
            }

            var result = Parser.Default.ParseArguments<StreamOptions>(args);

            return result.MapResult(
                Run,
                errs => CheckArgumentErrors());
        }

        private static int CheckArgumentErrors()
        {
            //will print using library the appropriate help - now pause the console for the viewer
            Console.WriteLine("Hit enter to quit");
            Console.ReadLine();
            return -1;
        }

        private static int RunStreamInteractive()
        {
            Console.WriteLine("No arguments supplied - would you like to enter interactive mode? [Y/N]");
            var response = Console.ReadKey();

            if (response.Key != ConsoleKey.Y)
            {
                Console.WriteLine("\n\n");
                Parser.Default.ParseArguments<StreamOptions>(new string[] { });
                return CheckArgumentErrors();
            }

            var newOpts = new StreamOptions();
            
            //ask the user interactively for an adapter and port
           Console.Write("Please enter the SRT listening port [9000]: ");
            var port = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(port))
            {
                port = "9000";
            }

            newOpts.SrtPort = int.Parse(port);

            Console.Write("Please enter the adapter address to listen for SRT clients [0.0.0.0]: ");

            var adapter = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(adapter))
            {
                adapter = "0.0.0.0";
            }

            newOpts.AdapterAddress = adapter;

            return Run(newOpts);
        }

        private static int Run(Options opts)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            _logger = LogManager.GetCurrentClassLogger();

            var buildVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();

            LogSetup.ConfigureLogger("srtstreamanalyser", opts.OrganizationId, opts.DescriptorTags, "https://telemetry.cinegy.com", opts.TelemetryEnabled, false, "SRTStreamAnalyser", buildVersion);

            _analyser = new Analyser(_logger);

            var location = Assembly.GetEntryAssembly().Location;

            _logger.Info($"Cinegy Transport Stream Monitoring and Analysis Tool (Built: {File.GetCreationTime(location)})");

            try
            {
                Console.CursorVisible = false;
                Console.SetWindowSize(120, 50);
                Console.OutputEncoding = Encoding.Unicode;
            }
            catch
            {
                Console.WriteLine("Failed to increase console size - probably screen resolution is low");
            }
            _options = opts;

            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            WorkLoop();

            return 0;
        }

        ~Program()
        {
            Console.CursorVisible = true;
        }

        private static void WorkLoop()
        {
            Console.Clear();
            
            LogMessage($"Logging started {Assembly.GetEntryAssembly().GetName().Version}.");

            _analyser.InspectTeletext = _options.DecodeTeletext;
            _analyser.InspectTsPackets = !_options.SkipDecodeTransportStream;
            _analyser.SelectedProgramNumber = _options.ProgramNumber;
            _analyser.VerboseLogging = _options.VerboseLogging;

            if (_options is StreamOptions streamOptions)
            {
                _analyser.HasRtpHeaders = !streamOptions.NoRtpHeaders;
                _analyser.Setup(streamOptions.AdapterAddress, streamOptions.SrtPort);
                _analyser.TsDecoder.TableChangeDetected += TsDecoder_TableChangeDetected;

                if (_analyser.InspectTeletext)
                {
                    _analyser.TeletextDecoder.Service.TeletextPageReady += Service_TeletextPageReady;
                    _analyser.TeletextDecoder.Service.TeletextPageCleared += Service_TeletextPageCleared;
                }

                _srtReceiver.SetHostname(streamOptions.AdapterAddress);
                _srtReceiver.SetPort(streamOptions.SrtPort);
                _srtReceiver.OnDataReceived += SrtReceiverOnDataReceived;
                
                var ts = new ThreadStart(delegate
                {
                    SrtWorkerThread(_srtReceiver);
                });

                _receiverThread = new Thread(ts);

                _receiverThread.Start();

            }

            Console.Clear();

            while (!_pendingExit)
            {
                if (!_options.SuppressOutput)
                {
                    PrintConsoleFeedback();
                }

                Thread.Sleep(30);
            }

            LogMessage("Logging stopped.");
        }

        private static void TsDecoder_TableChangeDetected(object sender, TableChangedEventArgs args)
        {
            if ((args.TableType == TableType.Pat) || (args.TableType == TableType.Pmt) || (args.TableType == TableType.Sdt))
            {
                //abuse occasional table refresh to clear all content on screen
                Console.Clear();
            }

            //Todo: finish implementing better EIT support
            //if (args.TableType != TableType.Eit) return;
            //if (sender is TsDecoder.TransportStream.TsDecoder decoder) Debug.WriteLine("EIT Version Num: " + decoder.EventInformationTable.VersionNumber);
        }

        private static void PrintConsoleFeedback()
        {
            var runningTime = DateTime.UtcNow.Subtract(StartTime);

            Console.SetCursorPosition(0, 0);

            if ((_options as StreamOptions) != null)
            {
                PrintToConsole("URL: {0}://{1}:{2}\tRunning time: {3:hh\\:mm\\:ss}\t\t",
                    ((StreamOptions)_options).NoRtpHeaders ? "udp" : "rtp",
                    $"@{((StreamOptions)_options).AdapterAddress}",
                    ((StreamOptions)_options).SrtPort, runningTime);

                var networkMetric = _analyser.NetworkMetric;
                var rtpMetric = _analyser.RtpMetric;

                PrintToConsole(
                    "\nNetwork Details\n----------------\nTotal Packets Rcvd: {0} \tBuffer Usage: {1:0.00}%/(Peak: {2:0.00}%)\t\t\nTotal Data (MB): {3}\t\tPackets per sec:{4}\t",
                    networkMetric.TotalPackets, networkMetric.NetworkBufferUsage, networkMetric.PeriodMaxNetworkBufferUsage,
                    networkMetric.TotalData / 1048576,
                    networkMetric.PacketsPerSecond);

                PrintToConsole("Period Max Packet Jitter (ms): {0}\t\t",
                    networkMetric.PeriodLongestTimeBetweenPackets);

                PrintToConsole(
                    "Bitrates (Mbps): {0:0.00}/{1:0.00}/{2:0.00}/{3:0.00} (Current/Avg/Peak/Low)\t\t\t",
                    (networkMetric.CurrentBitrate / 1048576.0), networkMetric.AverageBitrate / 1048576.0,
                    (networkMetric.HighestBitrate / 1048576.0), (networkMetric.LowestBitrate / 1048576.0));

                if (!((StreamOptions)_options).NoRtpHeaders)
                {
                    PrintToConsole(
                        "\nRTP Details\n----------------\nSeq Num: {0}\tMin Lost Pkts: {1}\nTimestamp: {2}\tSSRC: {3}\t",
                        rtpMetric.LastSequenceNumber, rtpMetric.EstimatedLostPackets, rtpMetric.LastTimestamp,
                        rtpMetric.Ssrc);
                }
            }

            var pidMetrics = _analyser.PidMetrics;

            lock (pidMetrics)
            {

                var span = new TimeSpan((long)(_analyser.LastPcr / 2.7));
                PrintToConsole(_analyser.LastPcr > 0 ? $"\nPCR Value: {span}\n----------------" : "\n\n");

                PrintToConsole(pidMetrics.Count < 10
                    ? $"\nPID Details - Unique PIDs: {pidMetrics.Count}\n----------------"
                    : $"\nPID Details - Unique PIDs: {pidMetrics.Count}, (10 shown by packet count)\n----------------");

                foreach (var pidMetric in pidMetrics.OrderByDescending(m => m.PacketCount).Take(10))
                {
                    PrintToConsole("TS PID: {0}\tPacket Count: {1} \t\tCC Error Count: {2}\t", pidMetric.Pid,
                        pidMetric.PacketCount, pidMetric.CcErrorCount);
                }
            }

            var tsDecoder = _analyser.TsDecoder;

            if (tsDecoder != null)
            {
                lock (tsDecoder)
                {
                    var pmts = tsDecoder.ProgramMapTables.OrderBy(p => p.ProgramNumber).ToList();

                    PrintToConsole(pmts.Count < 5
                        ? $"\t\t\t\nService Information - Service Count: {pmts.Count}\n----------------\t\t\t\t"
                        : $"\t\t\t\nService Information - Service Count: {pmts.Count}, (5 shown)\n----------------\t\t\t\t");

                    foreach (var pmtable in pmts.Take(5))
                    {
                        var desc = tsDecoder.GetServiceDescriptorForProgramNumber(pmtable?.ProgramNumber);
                        if (desc != null)
                        {
                            PrintToConsole(
                                $"Service {pmtable?.ProgramNumber}: {desc.ServiceName.Value} ({desc.ServiceProviderName.Value}) - {desc.ServiceTypeDescription}\t\t\t"
                                );
                        }
                    }

                    var pmt = tsDecoder.GetSelectedPmt(_options.ProgramNumber);
                    if (pmt != null)
                    {
                        _options.ProgramNumber = pmt.ProgramNumber;
                        _analyser.SelectedPcrPid = pmt.PcrPid;
                    }

                    var serviceDesc = tsDecoder.GetServiceDescriptorForProgramNumber(pmt?.ProgramNumber);

                    PrintToConsole(serviceDesc != null
                        ? $"\t\t\t\nElements - Selected Program: {serviceDesc.ServiceName} (ID:{pmt?.ProgramNumber}) (first 5 shown)\n----------------\t\t\t\t"
                        : $"\t\t\t\nElements - Selected Program Service ID {pmt?.ProgramNumber} (first 5 shown)\n----------------\t\t\t\t");

                    if (pmt?.EsStreams != null)
                    {
                        foreach (var stream in pmt.EsStreams.Take(5))
                        {
                            if (stream == null) continue;
                            if (stream.StreamType != 6)
                            {
                                PrintToConsole(
                                    "PID: {0} ({1})", stream.ElementaryPid,
                                    DescriptorDictionaries.ShortElementaryStreamTypeDescriptions[
                                        stream.StreamType]);
                            }
                            else
                            {
                                if (stream.Descriptors.OfType<Ac3Descriptor>().Any())
                                {
                                    PrintToConsole("PID: {0} ({1})", stream.ElementaryPid, "AC-3 / Dolby Digital");
                                    continue;
                                }
                                if (stream.Descriptors.OfType<Eac3Descriptor>().Any())
                                {
                                    PrintToConsole("PID: {0} ({1})", stream.ElementaryPid, "EAC-3 / Dolby Digital Plus");
                                    continue;
                                }
                                if (stream.Descriptors.OfType<SubtitlingDescriptor>().Any())
                                {
                                    PrintToConsole("PID: {0} ({1})", stream.ElementaryPid, "DVB Subtitles");
                                    continue;
                                }
                                if (stream.Descriptors.OfType<TeletextDescriptor>().Any())
                                {
                                    PrintToConsole("PID: {0} ({1})", stream.ElementaryPid, "Teletext");
                                    continue;
                                }
                                if (stream.Descriptors.OfType<RegistrationDescriptor>().Any())
                                {
                                    if (stream.Descriptors.OfType<RegistrationDescriptor>().First().Organization == "2LND")
                                    {
                                        PrintToConsole("PID: {0} ({1})", stream.ElementaryPid, "Cinegy DANIEL2");
                                        continue;
                                    }
                                }

                                PrintToConsole(
                                    "PID: {0} ({1})", stream.ElementaryPid,
                                    DescriptorDictionaries.ShortElementaryStreamTypeDescriptions[
                                        stream.StreamType]);

                            }

                        }
                    }
                }

                if (_options.DecodeTeletext)
                {
                    PrintTeletext();
                }
            }

            if (_lastPrintedTsCount != pidMetrics.Count)
            {
                _lastPrintedTsCount = pidMetrics.Count;
                Console.Clear();
            }

            var result = ConsoleDisplay.ToString();

            Console.WriteLine(result);
            ConsoleDisplay.Clear();

        }

        private static void PrintTeletext()
        {
            //some strangeness here to get around the fact we just append to console, to clear out
            //a fixed 4 lines of space for TTX render
            const string clearLine = "\t\t\t\t\t\t\t\t\t";
            var ttxRender = new[] { clearLine, clearLine, clearLine, clearLine };

            if (_decodedSubtitlePage != null)
            {
                lock (_decodedSubtitlePage)
                {
                    var defaultLang = _decodedSubtitlePage.ParentMagazine.ParentService.AssociatedDescriptor.Languages
                        .FirstOrDefault();

                    if (defaultLang != null)
                        PrintToConsole(
                            $"\nTeletext Subtitles ({defaultLang.Iso639LanguageCode}) - decoding from Service ID {_decodedSubtitlePage.ParentMagazine.ParentService.ProgramNumber}, PID: {_decodedSubtitlePage.ParentMagazine.ParentService.TeletextPid}\n----------------");

                    PrintToConsole($"Live Decoding Page {_decodedSubtitlePage.ParentMagazine.MagazineNum}{_decodedSubtitlePage.PageNum:x00}");

                    PrintToConsole(
                        $"Packets (Period/Total): {_analyser.TeletextMetric.PeriodTtxPacketCount}/{_analyser.TeletextMetric.TtxPacketCount}, Total Pages: {_analyser.TeletextMetric.TtxPageReadyCount}, Total Clears: {_analyser.TeletextMetric.TtxPageClearCount}\n");


                    var i = 0;

                    foreach (var row in _decodedSubtitlePage.Rows)
                    {
                        if (!row.IsChanged() || string.IsNullOrWhiteSpace(row.GetPlainRow())) continue;
                        ttxRender[i] = $"{row.GetPlainRow()}\t\t\t";
                        i++;
                    }
                }
            }

            foreach (var val in ttxRender)
            {
                PrintToConsole(val);
            }
        }

        private static void SrtWorkerThread(SrtReceiver client)
        {
            while (!_pendingExit)
            {
                client.Run();
                Console.WriteLine("\nConnection closed, resetting...");
                Thread.Sleep(100);
            }

            Environment.Exit(0);
        }

        private static void SrtReceiverOnDataReceived(sbyte* data, ulong dataSize)
        {
            if (dataSize < 1) return;
            var ptr = new IntPtr(data);
            var dataArr = new byte[dataSize];
            Marshal.Copy(ptr, dataArr, 0, dataArr.Length);
            _analyser.RingBuffer.Add(ref dataArr);
        }

        private static TeletextPage _decodedSubtitlePage;

        private static void Service_TeletextPageReady(object sender, EventArgs e)
        {
            var ttxE = (TeletextPageReadyEventArgs)e;

            if (ttxE == null) return;

            _decodedSubtitlePage = ttxE.Page;
        }

        private static void Service_TeletextPageCleared(object sender, EventArgs e)
        {
            var ttxE = (TeletextPageClearedEventArgs)e;

            if (_decodedSubtitlePage?.PageNum == ttxE.PageNumber)
                _decodedSubtitlePage = null;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.CursorVisible = true;
            if (_pendingExit) return; //already trying to exit - allow normal behaviour on subsequent presses
            _pendingExit = true;
            _srtReceiver.Stop();
            _analyser.Cancel();
            _analyser = null;
            //_receiverThread.Abort(); 
            e.Cancel = true;
        }

        private static void PrintToConsole(string message, params object[] arguments)
        {
            if (_options.SuppressOutput) return;

            ConsoleDisplay.AppendLine(string.Format(message, arguments));
        }

        private static void LogMessage(string message)
        {
            var lei = new TelemetryLogEventInfo
            {
                Level = LogLevel.Info,
                Key = "GenericEvent",
                Message = message
            };

            _logger.Log(lei);
        }

    }
}

