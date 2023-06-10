using System;
using SrtSharp;

namespace Cinegy.Srt.Wrapper
{
    public interface ISecureReliableTransportReceiver : IDisposable
    {
        ISecureReliableTransportChunk GetChunk();

        SRT_TRACEBSTATS GetStats();
    }
}