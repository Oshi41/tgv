using System.Net;
using tgv.core;
using tgv.extensions;
using tgv.imp;
using WatsonWebserver.Lite;
using Handle = tgv.core.Handle;
using Hme = tgv.extensions.HttpMethodExtensions;
using System.Runtime.CompilerServices;
using WatsonWebserver.Core;

[assembly: InternalsVisibleTo("tgv-tests")]

namespace tgv;

public class App : IRouter
{
    private readonly AppConfig _appConfig;
    private WebserverLite _server;
    private IRouter _root;

    public App(AppConfig? config = null, RouterConfig? routerConfig = null)
    {
        _appConfig = config ?? new AppConfig();
        _root = new Router("*", routerConfig ?? new RouterConfig());
        Logger = new Logger();
    }

    public string? RunningUrl
    {
        get
        {
            if (_server?.IsListening != true) return null;

            var prefix = _server.Settings.Ssl.Enable ? "https" : "http";
            return $"{prefix}://localhost:{_server.Settings.Port}/";
        }
    }

    public Logger Logger { get; }

    public async Task Start(int port = 7000)
    {
        Stop();
        var settings = _appConfig.Convert();
        settings.Port = port;
        _server = new WebserverLite(settings, ServerHandler);
        _server.Events.Logger = Logger.WriteLog;
        _server.Start();

        while (string.IsNullOrEmpty(RunningUrl))
        {
            await Task.Delay(100);
        }
        
        Started?.Invoke(this, _server);
        Logger.Debug($"Server started on port {_server.Settings.Port}");
    }

    public bool Stop()
    {
        if (_server?.IsListening != true) return false;

        _server.Stop();
        Closed?.Invoke(this, _server);
        Logger.Debug($"Server stopped");
        return true;
    }

    public event EventHandler<WebserverLite> Started;
    public event EventHandler<WebserverLite> Closed;

    private async Task ServerHandler(HttpContextBase ctx)
    {
        using var context = new Context(ctx, Logger);
        await Handle(context);
        
        // checking if response was sent
        if (context.WasSent) return;

        // no route was visited
        if (!context.Visited.Any())
        {
            await context.Send(HttpStatusCode.NotFound);
            return;
        }
        
        // checking if visited any error
        var wasError = context.Visited
            .Select(x => x.Route)
            .Any(x => x.Method == Hme.Error);
        
        await context.Send(wasError
            ? HttpStatusCode.InternalServerError
            : HttpStatusCode.OK);
    }

    private async Task Handle(Context ctx)
    {
        try
        {
            foreach (var method in new [] { Hme.Before, ctx.Ctx.Request.Method.Convert(), Hme.After })
            {
                ctx.Stage = method;
                var next = false;
                await _root.Handler(ctx, () => next = true);

                // do not allow to call further 
                if (!next) break;
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
            ctx.Logger.Fatal($"Exception: {ex.Message}");
            if (!ctx.WasSent)
                await ctx.Send(HttpStatusCode.InternalServerError);
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