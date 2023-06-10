using System;
using System.Net;

namespace Cinegy.Srt.Wrapper
{
    public interface ISecureReliableTransportSender : IDisposable
    {
        IPEndPoint Address { get; }

        IPEndPoint Accept();

        void Send(byte[] data);
    }
}
