using System;
using System.IO;
using System.Net;
using SrtSharp;

namespace Cinegy.Srt.Wrapper
{
    class SecureReliableTransportReceiver : ISecureReliableTransportReceiver,
                                            ISecureReliableTransportChunk
    {
        private readonly byte[] _buffer;
        private readonly int _socket;
        private readonly SRT_TRACEBSTATS _stats;
        private int _size;

        public SecureReliableTransportReceiver(IPEndPoint endpoint, int bufferSize)
        {
            _stats = new SRT_TRACEBSTATS();

            EndPoint = endpoint;
            _buffer = new byte[bufferSize];

            MessageControl = new SRT_MSGCTRL();

            _socket = srt.srt_create_socket();

            srt.srt_connect(_socket, EndPoint);
        }

        public SRT_MSGCTRL MessageControl { get; }

        public byte[] Data => _buffer;

        public int DataLen => _size;

        public IPEndPoint EndPoint { get; }

        public ISecureReliableTransportChunk GetChunk()
        {
            var stat = srt.srt_recvmsg2(_socket, _buffer, _buffer.Length, MessageControl);
            if (stat == srt.SRT_ERROR) throw new IOException(srt.srt_getlasterror_str());
            _size = stat;
            return this;
        }

        public SRT_TRACEBSTATS GetStats()
        {
            srt.srt_bistats(_socket, _stats, 0, 1);
            return _stats;
        }

        public void Dispose()
        {
            if (_socket != 0)
            {
                srt.srt_close(_socket);
            }

            MessageControl?.Dispose();
            _stats?.Dispose();
        }
    }
}