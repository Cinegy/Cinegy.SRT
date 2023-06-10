using System;
using System.Collections.Generic;
using System.Net;

namespace Cinegy.Srt.Wrapper;

public interface ISecureReliableTransportBroadcaster : IDisposable
{
    IPEndPoint Address { get; }

    IReadOnlyList<IPEndPoint> ConnectedClients { get; }

    void Broadcast(byte[] data);
}
