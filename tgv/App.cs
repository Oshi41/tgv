using System.Net;
using tgv.core;
using tgv.extensions;
using tgv.imp;
using Handle = tgv.core.Handle;
using Hme = tgv.extensions.HttpMethodExtensions;
using System.Runtime.CompilerServices;
using tgv.servers;
using WatsonWebserver.Core;

[assembly: InternalsVisibleTo("tgv-tests")]

namespace tgv;

public delegate Task HttpHandler(Context ctx);

public class App : IRouter
{
    private IServer _server;
    private IRouter _root;

    public App(Func<HttpHandler, IServer> server, RouterConfig? cfg = null)
    {
        Logger = new Logger();
        _root = new Router("*", cfg ?? new RouterConfig());
        _server = server(Handle);
        _server.Logger.WriteLog = Logger.WriteLog;
    }

    public string? RunningUrl
    {
        get
        {
            if (_server?.IsListening != true) return null;

            var prefix = _server.IsHttps ? "https" : "http";
            return $"{prefix}://localhost:{_server.Port}/";
        }
    }

    public Logger Logger { get; }

    public async Task Start(int port = 7000)
    {
        Stop();
        await _server.StartAsync(port);

        while (string.IsNullOrEmpty(RunningUrl))
        {
            await Task.Delay(100);
        }

        Started?.Invoke(this, _server);
        Logger.Debug($"Server started on port {_server.Port}");
    }

    public bool Stop()
    {
        if (_server?.IsListening != true) return false;

        _server.Stop();
        Closed?.Invoke(this, _server);
        Logger.Debug($"Server stopped");
        return true;
    }

    public event EventHandler<IServer> Started;
    public event EventHandler<IServer> Closed;

    private async Task Handle(Context ctx)
    {
        try
        {
            foreach (var method in new[] { Hme.Before, ctx.Method, Hme.After })
            {
                ctx.Stage = method;
                var next = false;
                await _root.Handler(ctx, () => next = true);

                // do not allow to call further 
                if (!next) break;
            }

            // sending default response if didn't do so
            if (!ctx.WasSent)
            {
                ctx.Logger.Info("Sending default OK response");
                await ctx.Send(HttpStatusCode.OK);
            }
        }
        catch (Exception e)
        {
            // handling error routes
            ctx.Logger.Warn($"Exception during route handling: {e.Message}");
            await Handle(ctx, e);
        }
    }

    private async Task Handle(Context ctx, Exception error)
    {
        try
        {
            if (ctx.Stage == Hme.Error)
            {
                throw new Exception($"Fatal error occured");
            }

            ctx.Stage = Hme.Error;
            await _root.Handler(ctx, () => { }, error);
        }
        catch (Exception ex)
        {
            error = ex;
            ctx.Logger.Fatal($"Exception: {ex.Message}");
        }
        finally
        {
            // sending default error response
            if (!ctx.WasSent)
            {
                ctx.Logger.Info("Sending default InternalServerError response");
                if (error is HttpException httpException)
                {
                    await ctx.Send(httpException.Code, httpException.Message);
                }
                else
                {
                    await ctx.Send(HttpStatusCode.InternalServerError);
                }
            }
        }
    }

    #region IRouter

    public RoutePath Route => _root.Route;

    public IRouter Use(params Handle[] handlers)
    {
        _root.Use(handlers);
        return this;
    }

    public IRouter After(params Handle[] handlers)
    {
        _root.After(handlers);
        return this;
    }

    public IRouter Use(string path, params Handle[] handlers)
    {
        _root.Use(path, handlers);
        return this;
    }

    public IRouter After(string path, params Handle[] handlers)
    {
        _root.After(path, handlers);
        return this;
    }

    public IRouter Use(IRouter router)
    {
        _root.Use(router);
        return this;
    }

    public IRouter Get(string path, params Handle[] handlers)
    {
        _root.Get(path, handlers);
        return this;
    }

    public IRouter Post(string path, params Handle[] handlers)
    {
        _root.Post(path, handlers);
        return this;
    }

    public IRouter Delete(string path, params Handle[] handlers)
    {
        _root.Delete(path, handlers);
        return this;
    }

    public IRouter Patch(string path, params Handle[] handlers)
    {
        _root.Patch(path, handlers);
        return this;
    }

    public IRouter Put(string path, params Handle[] handlers)
    {
        _root.Put(path, handlers);
        return this;
    }

    public IRouter Head(string path, params Handle[] handlers)
    {
        _root.Head(path, handlers);
        return this;
    }

    public IRouter Error(string path, params Handle[] handlers)
    {
        _root.Error(path, handlers);
        return this;
    }

    public IRouter Options(string path, params Handle[] handlers)
    {
        _root.Options(path, handlers);
        return this;
    }

    public IRouter Connect(string path, params Handle[] handlers)
    {
        _root.Connect(path, handlers);
        return this;
    }

    public IRouter Trace(string path, params Handle[] handlers)
    {
        _root.Trace(path, handlers);
        return this;
    }

    public Handle Handler => _root.Handler;

    #endregion
}