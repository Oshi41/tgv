using System;
using System.Threading.Tasks;
using tgv_core.imp;

namespace tgv_core.api;

/// <summary>
/// Base delegate for handling endpoints \ middleware
/// </summary>
public delegate Task HttpHandler(Context ctx, Action next, Exception? e = null);

/// <summary>
/// Delegate handling HTTP call
/// </summary>
public delegate Task ServerHandler(Context ctx);

/// <summary>
/// Server interface
/// </summary>
public abstract class IServer
{
    protected readonly ServerHandler _handler;

    protected IServer(ServerHandler handler, Logger logger)
    {
        Logger = logger;
        _handler = handler;
    }
    
    public abstract bool IsListening { get; }
    public abstract bool IsHttps { get; }
    public abstract int Port { get; }
    public Logger Logger { get; }
    public abstract Task StartAsync(int port);
    public abstract void Stop();
}