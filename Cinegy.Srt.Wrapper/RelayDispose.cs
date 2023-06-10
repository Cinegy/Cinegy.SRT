using System;

namespace Cinegy.Srt.Wrapper;

/// <summary>
/// Class wrapper for Dispose operation
/// </summary>
internal class RelayDispose : IDisposable
{
    private readonly Action _disposeAction;

    public RelayDispose(Action disposeAction)
    {
        _disposeAction = disposeAction;
    }

    public void Dispose()
    {
        _disposeAction?.Invoke();
    }
}
