using System;

namespace tgv_core.imp;

public class Disposable(Action action) : IDisposable
{
    public static IDisposable Create(Action action) => new Disposable(action);

    public void Dispose() => action?.Invoke();
}