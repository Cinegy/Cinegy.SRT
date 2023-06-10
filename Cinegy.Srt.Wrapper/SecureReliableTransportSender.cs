using System;
using System.IO;
using System.Net;
using SrtSharp;

namespace Cinegy.Srt.Wrapper
{
    class SecureReliableTransportSender : ISecureReliableTransportSender
    {
        private readonly int _socket;
        private int _acceptedSocket;

        public SecureReliableTransportSender(IPEndPoint endpoint)
        {
            _socket = srt.srt_create_socket();

            if (srt.srt_bind(_socket, endpoint) < 0)
            {
                throw new IOException($"Error binding listen socket: {srt.srt_getlasterror_str()}");
            }

            if (srt.srt_listen(_socket, 5) < 0)
            {
                throw new IOException($"Error setting listen backlog: {srt.srt_getlasterror_str()}");
            }

            Address = endpoint;
        }

        public void Dispose()
        {
            if (_socket != 0)
            {
                srt.srt_close(_socket);
            }

            if (_acceptedSocket != 0)
            {
                _acceptedSocket = 0;
                srt.srt_close(_acceptedSocket);
            }
        }

        public IPEndPoint Accept()
        {
            if (_acceptedSocket != 0)
            {
                srt.srt_close(_acceptedSocket);
                _acceptedSocket = 0;
            }

            _acceptedSocket = srt.srt_accept(_socket, out var endPoint);
            return endPoint;
        }

        public void Send(byte[] data)
        {
            if (_acceptedSocket == 0)
            {
                throw new InvalidOperationException("Pending connection is not accepted");
            }

            var sndResult = srt.srt_send(_acceptedSocket, data, data.Length);
            if (sndResult == srt.SRT_ERROR) throw new IOException(srt.srt_getlasterror_str());
        }

        public IPEndPoint Address { get; }
    }
}
