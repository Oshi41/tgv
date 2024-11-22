using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using tgv_core.api;
using tgv_core.extensions;

namespace tgv_core.imp;

public class Router : IRouter
{
    private readonly List<IMatch> _routes = new();
    private readonly RouterConfig _routerConfig;

    public Router(string path, RouterConfig? routerConfig = null)
    {
        _routerConfig = routerConfig ?? new RouterConfig();
        Route = new RoutePath(HttpMethodExtensions.Before, path, null, _routerConfig, true);
        Handler = HandleInner;
    }

    public RoutePath Route { get; }
    public IRouter Use(params HttpHandler[] handlers) => Use("*", handlers);

    public IRouter After(params HttpHandler[] handlers) => After("*", handlers);

    public IRouter Use(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethodExtensions.Before, path, handlers);
    }

    public IRouter After(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethodExtensions.After, path, handlers);
    }

    public IRouter Use(IRouter router)
    {
        _routes.Add(router);
        return this;
    }

    public IRouter Get(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethod.Get, path, handlers);
    }

    public IRouter Post(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethod.Post, path, handlers);
    }

    public IRouter Delete(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethod.Delete, path, handlers);
    }

    public IRouter Patch(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethodExtensions.Patch, path, handlers);
    }

    public IRouter Put(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethod.Put, path, handlers);
    }

    public IRouter Head(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethod.Head, path, handlers);
    }

    public IRouter Error(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethodExtensions.Error, path, handlers);
    }

    public IRouter Options(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethod.Options, path, handlers);
    }

    public IRouter Connect(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethodExtensions.Connect, path, handlers);
    }

    public IRouter Trace(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethod.Trace, path, handlers);
    }

    public bool Match(Context ctx) => Route.Match(ctx);
    public HttpHandler Handler { get; }

    private async Task HandleInner(Context ctx, Action next, Exception? exception = null)
    {
        ctx.CurrentPath.Push(this);
        using var __ = new Disposable(() => ctx.CurrentPath.Pop());

        foreach (var match in _routes.Where(x => x.Route.Match(ctx)))
        {
            ctx.Visited.Enqueue(match);
            if (ctx.Parameters == null && match is RoutePath routePath)
            {
                ctx.Parameters = routePath.Parameters(ctx);
            }

            using var _ = new Disposable(() => ctx.Parameters = null);
            var navigateNext = false;
            await match.Handler(ctx, () => navigateNext = true);
            if (!navigateNext)
                return;
        }

        next();
    }

    private Router AddRoutes(HttpMethod method, string path, params HttpHandler[] handlers)
    {
        foreach (var handler in handlers)
        {
            _routes.Add(new RoutePath(method, path, handler, _routerConfig));
        }

        return this;
    }
}