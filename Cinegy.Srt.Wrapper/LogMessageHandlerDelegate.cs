using System.Runtime.CompilerServices;

namespace Cinegy.Srt.Wrapper;

public delegate void LogMessageHandlerDelegate(int level, string message, [CallerMemberName] string area = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0);
