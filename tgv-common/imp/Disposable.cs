﻿using System;

namespace tgv_common.imp;

public class Disposable : IDisposable
{
    private readonly Action _action;

    public Disposable(Action action)
    {
        _action = action;
    }

    public static IDisposable Create(Action action) => new Disposable(action);

    public void Dispose() => _action?.Invoke();
}