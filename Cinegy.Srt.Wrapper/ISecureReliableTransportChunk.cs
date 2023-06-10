using System;
using System.Net;
using SrtSharp;

namespace Cinegy.Srt.Wrapper
{
    public interface ISecureReliableTransportChunk
    {
        byte[] Data { get; }

        int DataLen { get; }

        IPEndPoint EndPoint { get; }

        SRT_MSGCTRL MessageControl { get; }
    }
}