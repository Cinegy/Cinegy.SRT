using SrtSharp;

namespace Cinegy.Srt.Wrapper;

public class LoggerOptions
{
    public LogFunctionalArea? FunctionalAreas { get; set; }

    public LogFlag? LogFlags { get; set; }

    public LogLevel? LogLevel { get; set; }

    public LogMessageHandlerDelegate LogMessageAction { get; set; }
}
