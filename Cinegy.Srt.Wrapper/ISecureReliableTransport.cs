using System;
using System.Net;

namespace Cinegy.Srt.Wrapper;

public interface ISecureReliableTransport : IDisposable
{
    ISecureReliableTransportBroadcaster CreateBroadcaster(IPEndPoint endpoint, BroadcasterSettings broadcasterSettings = null);

    ISecureReliableTransportReceiver CreateReceiver(IPEndPoint endpoint, int bufferSize);

    ISecureReliableTransportSender CreateSender(IPEndPoint endpoint);
}
