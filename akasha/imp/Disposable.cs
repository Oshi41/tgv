using System;

namespace akasha.imp;

internal class Disposable(Action action) : IDisposable
{
    private Action? _action = action;

    public void Dispose()
    {
        _action?.Invoke();
        _action = null;
    }

    public static Disposable Create(Action action) => new(action);
}