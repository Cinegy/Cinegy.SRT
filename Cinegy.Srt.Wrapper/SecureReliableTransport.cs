using System;
using System.Net;
using SrtSharp;

namespace Cinegy.Srt.Wrapper;

public class SecureReliableTransport : ISecureReliableTransport
{
    private readonly LogMessageHandlerDelegate _logMessageAction;

    private SecureReliableTransport(LoggerOptions loggerOptions)
    {
        srt.srt_startup();

        if (loggerOptions?.FunctionalAreas is LogFunctionalArea logFunctionalArea) srt.srt_addlogfa(logFunctionalArea);
        if (loggerOptions?.LogFlags is LogFlag logFlags) srt.srt_setlogflags(logFlags);
        if (loggerOptions?.LogLevel is LogLevel logLevel) srt.srt_setloglevel((int) logLevel);

        _logMessageAction = loggerOptions?.LogMessageAction;
        if (_logMessageAction != null)
        {
            void Handler(IntPtr opaque, int level, string file, int line, string area, string message)
            {
                _logMessageAction(level, message, area, file, line);
            }

            srt.srt_setloghandler(IntPtr.Zero, Handler);
        }
    }

    public void Dispose()
    {
        srt.srt_cleanup();
    }

    public ISecureReliableTransportReceiver CreateReceiver(IPEndPoint endpoint, int bufferSize)
    {
        return new SecureReliableTransportReceiver(endpoint, bufferSize);
    }

    public ISecureReliableTransportSender CreateSender(IPEndPoint endpoint)
    {
        return new SecureReliableTransportSender(endpoint);
    }

    public ISecureReliableTransportBroadcaster CreateBroadcaster(IPEndPoint endpoint, BroadcasterSettings broadcasterSettings)
    {
        return new SecureReliableTransportBroadcaster(endpoint, broadcasterSettings ?? new BroadcasterSettings(), _logMessageAction);
    }

    public static ISecureReliableTransport Setup(LoggerOptions options = null)
    {
        return new SecureReliableTransport(options);
    }
}
