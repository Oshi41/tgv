using sharp_express.core;

namespace sharp_express.imp;

public class Router : IRouter
{
    private readonly List<IMatch> _routes = new();
    private readonly RouterConfig _routerConfig;

    public Router(string path, RouterConfig? routerConfig = null)
    {
        _routerConfig = routerConfig ?? new RouterConfig();
        Route = new RoutePath("BEFORE", path, null, _routerConfig, true);
        Handler = HandleInner;
    }

    public RoutePath Route { get; }

    private Router AddRoutes(string method, string path, params Handle[] handlers)
    {
        foreach (var handler in handlers)
        {
            _routes.Add(new RoutePath(method, path, handler, _routerConfig));
        }

        return this;
    }

    private async Task HandleInner(IContext ctx, Action next, Exception? exception = null)
    {
        ctx.CurrentPath.Push(this);
        using var __ = new Disposable(() => ctx.CurrentPath.Pop());

        foreach (var match in _routes.Where(x => x.Route.Match(ctx)))
        {
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

    public IRouter Use(params Handle[] handlers) => Use("*", handlers);

    public IRouter After(params Handle[] handlers)
    {
        return After("*", handlers);
    }

    public IRouter Use(string path, params Handle[] handlers)
    {
        return AddRoutes("BEFORE", path, handlers);
    }

    public IRouter After(string path, params Handle[] handlers)
    {
        return AddRoutes("AFTER", path, handlers);
    }

    public IRouter Use(IRouter router)
    {
        _routes.Add(router);
        return this;
    }

    public IRouter Get(string path, params Handle[] handlers)
    {
        return AddRoutes("GET", path, handlers);
    }

    public IRouter Post(string path, params Handle[] handlers)
    {
        return AddRoutes("POST", path, handlers);
    }

    public IRouter Delete(string path, params Handle[] handlers)
    {
        return AddRoutes("DELETE", path, handlers);
    }

    public IRouter Patch(string path, params Handle[] handlers)
    {
        return AddRoutes("PATCH", path, handlers);
    }

    public IRouter Put(string path, params Handle[] handlers)
    {
        return AddRoutes("PUT", path, handlers);
    }

    public IRouter Head(string path, params Handle[] handlers)
    {
        return AddRoutes("HEAD", path, handlers);
    }

    public IRouter Error(string path, params Handle[] handlers)
    {
        return AddRoutes("ERROR", path, handlers);
    }

    public bool Match(IContext ctx) => Route.Match(ctx);
    public Handle Handler { get; }
}