using System.Net;
using Hme = tgv_core.extensions.HttpMethodExtensions;
using System.Runtime.CompilerServices;
using tgv_core.api;
using tgv_core.imp;

[assembly: InternalsVisibleTo("tgv-tests")]

namespace tgv;

public class App : IRouter
{
    private IServer _server;
    internal Router _root;

    public App(IServer server, RouterConfig? cfg = null)
    {
        Logger = new Logger();
        _root = new Router("*", cfg ?? new RouterConfig());
        _server = server;
        
        // some internals
        _server.Handler = Handle;
        _server.Logger = Logger;
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

    private Task Handle(Context ctx, Exception? error = null)
    {
        this.Associate(ctx);
        return error != null ? HandleError(ctx, error) : HandleCommon(ctx);
    }

    private async Task HandleCommon(Context ctx)
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
            await HandleError(ctx, e);
        }
    }

    private async Task HandleError(Context ctx, Exception error)
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

    public IRouter Use(params HttpHandler[] handlers)
    {
        _root.Use(handlers);
        return this;
    }

    public IRouter Use<T>(params ExtensionFactory<T>[] extensions) where T : class
    {
        return _root.Use(extensions);
    }

    public IRouter After(params HttpHandler[] handlers)
    {
        _root.After(handlers);
        return this;
    }

    public IRouter Use(string path, params HttpHandler[] handlers)
    {
        _root.Use(path, handlers);
        return this;
    }

    public IRouter Use<T>(string path, params ExtensionFactory<T>[] extensions) where T : class
    {
        return _root.Use(path, extensions);
    }

    public IRouter After(string path, params HttpHandler[] handlers)
    {
        _root.After(path, handlers);
        return this;
    }

    public IRouter Use(IRouter router)
    {
        _root.Use(router);
        return this;
    }

    public IRouter Get(string path, params HttpHandler[] handlers)
    {
        _root.Get(path, handlers);
        return this;
    }

    public IRouter Post(string path, params HttpHandler[] handlers)
    {
        _root.Post(path, handlers);
        return this;
    }

    public IRouter Delete(string path, params HttpHandler[] handlers)
    {
        _root.Delete(path, handlers);
        return this;
    }

    public IRouter Patch(string path, params HttpHandler[] handlers)
    {
        _root.Patch(path, handlers);
        return this;
    }

    public IRouter Put(string path, params HttpHandler[] handlers)
    {
        _root.Put(path, handlers);
        return this;
    }

    public IRouter Head(string path, params HttpHandler[] handlers)
    {
        _root.Head(path, handlers);
        return this;
    }

    public IRouter Error(string path, params HttpHandler[] handlers)
    {
        _root.Error(path, handlers);
        return this;
    }

    public IRouter Options(string path, params HttpHandler[] handlers)
    {
        _root.Options(path, handlers);
        return this;
    }

    public IRouter Connect(string path, params HttpHandler[] handlers)
    {
        _root.Connect(path, handlers);
        return this;
    }

    public IRouter Trace(string path, params HttpHandler[] handlers)
    {
        _root.Trace(path, handlers);
        return this;
    }

    public HttpHandler Handler => _root.Handler;

    #endregion
}