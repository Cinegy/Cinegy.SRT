using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SrtSharp;

namespace Cinegy.Srt.Wrapper;

class SecureReliableTransportBroadcaster : ISecureReliableTransportBroadcaster
{
    private readonly ConcurrentDictionary<SRTSOCKET, IPEndPoint> _acceptedClients;
    private readonly BroadcasterSettings _broadcasterSettings;
    private readonly Task _clientsAcceptThread;
    private readonly CancellationTokenSource _internalTokenSource;
    private readonly SRTSOCKET _listeningSocket;
    private readonly LogMessageHandlerDelegate _logger;
    private readonly Meter _metricsMeter = new("Cinegy.SrtWrapper");
    private readonly ObservableGauge<int> _srtBroadcastConnectionGauge;
    private readonly KeyValuePair<string, object> _metricsEndpointKey;

    public SecureReliableTransportBroadcaster(IPEndPoint endpoint, BroadcasterSettings broadcasterSettings, LogMessageHandlerDelegate logger)
    {
        _broadcasterSettings = broadcasterSettings;
        _logger = logger;
        _internalTokenSource = new CancellationTokenSource();
        _acceptedClients = new ConcurrentDictionary<SRTSOCKET, IPEndPoint>();

        _listeningSocket = srt.srt_create_socket().Validate();
        srt.srt_setsockflag_bool(_listeningSocket, SRT_BOOL_SOCKOPT.SRTO_BOOL_RCVSYN, false).Validate("SRT unblock socket");
        srt.srt_listen_callback(_listeningSocket, ListenCallback, IntPtr.Zero).Validate("Setting listen callback");
        srt.srt_bind(_listeningSocket, endpoint).Validate("Binding listen socket");
        srt.srt_listen(_listeningSocket, 5).Validate("Setting listen backlog");

        Address = endpoint;

        _metricsEndpointKey = new KeyValuePair<string, object>("endpoint", endpoint.Address.ToString());
        _srtBroadcastConnectionGauge = _metricsMeter.CreateObservableGauge("srtBroadcastConnections", () => new Measurement<int>(_acceptedClients.Count,_metricsEndpointKey));

        _clientsAcceptThread = new Task(ConnectionsAcceptThread, TaskCreationOptions.LongRunning);
        _clientsAcceptThread.Start();
    }

    public void Dispose()
    {
        _internalTokenSource.Cancel();
        _clientsAcceptThread.Wait();

        _listeningSocket.Dispose();
    }

    public void Broadcast(byte[] data)
    {
        void SendToSockets(IEnumerable<SRTSOCKET> sockets)
        {
            foreach (var socket in sockets)
            {
                var sndResult = srt.srt_send(socket, data, data.Length);
                if (sndResult != srt.SRT_ERROR) continue;

                if (_acceptedClients.TryGetValue(socket, out var address))
                {
                    _logger?.Invoke(LogLevel.Warning, $"Error while sending data to {address}. Details: {srt.srt_getlasterror_str()}");
                }
            }
        }

        var sockets = _acceptedClients.Keys;
        if (sockets.Count < _broadcasterSettings.ClientsPerThread)
        {
            SendToSockets(sockets);
        }
        else
        {
            // Balance sockets for parallel execution
            var socketsPerChunk = sockets.Count / (sockets.Count / _broadcasterSettings.ClientsPerThread + 1);

            var actions = sockets
                .Chunk(socketsPerChunk)
                .Select(chunk => new Action(() => SendToSockets(chunk)))
                .ToArray();

            Parallel.Invoke(actions);
        }
    }

    public IPEndPoint Address { get; }

    IReadOnlyList<IPEndPoint> ISecureReliableTransportBroadcaster.ConnectedClients
    {
        get { return _acceptedClients.Values.ToList(); }
    }

    public void ConnectionsAcceptThread()
    {
        var token = _internalTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            var retVal = srt.srt_accept(_listeningSocket, out _);
            
            Thread.Sleep(500);
        }
    }

    private void ConnectionClosedCallback(IntPtr opaque, int ns, SRT_ERRNO code, IPEndPoint address, int i)
    {
        _acceptedClients.TryRemove(ns, out _);
        _logger?.Invoke(LogLevel.Notice, $"CALLBACK: Client from {address} closed with error: {code}");
    }

    private void ListenCallback(IntPtr opaque, int ns, int version, IPEndPoint address, string id)
    {
        SRTSOCKET clientSocket = ns;
        _logger?.Invoke(LogLevel.Notice, $"CALLBACK: Client attempting connection from {address} with '{id}'");

        var result = srt.srt_connect_callback(ns, ConnectionClosedCallback, IntPtr.Zero);
        if (result == srt.SRT_ERROR)
        {
            _logger?.Invoke(LogLevel.Notice, $"CALLBACK: Client from {address} was abandoned because of internal error");
            clientSocket.Dispose();
            return;
        }

        _acceptedClients.TryAdd(ns, address);
    }
}
