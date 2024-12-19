using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using tgv_core.api;
using tgv_core.extensions;

namespace tgv_core.imp;

/// <summary>
/// Represents a routing mechanism for handling HTTP requests in a web application.
/// <inheritdoc cref="IRouter"/>
/// </summary>
public class Router : IRouter
{
    internal readonly List<IMatch> _routes = new();
    private readonly RouterConfig _routerConfig;

    public Router(string path, RouterConfig? routerConfig = null)
    {
        _routerConfig = routerConfig ?? new RouterConfig();
        Route = new RoutePath(HttpMethodExtensions.Before, path, null, _routerConfig, true);
        Handler = HandleInner;
    }

    public RoutePath Route { get; }
    public IRouter Use(params HttpHandler[] handlers) => Use("*", handlers);

    public IRouter Use<T, T1>(params ExtensionFactory<T, T1>[] extensions)
        where T : class
        where T1 : IEquatable<T1>
    {
        return Use("*", extensions);
    }

    public IRouter After(params HttpHandler[] handlers) => After("*", handlers);

    public IRouter Use(string path, params HttpHandler[] handlers)
    {
        return AddRoutes(HttpMethodExtensions.Before, path, handlers);
    }
    
    public IRouter Use<T, T1>(string path, params ExtensionFactory<T, T1>[] extensions)
        where T : class
        where T1 : IEquatable<T1>
    {
        foreach (var ext in extensions)
        {
            _routes.Add(new MiddlewareExtension(
                HttpMethodExtensions.Before,
                path,
                ext,
                _routerConfig
                ));
        }

        return this;
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

    /// <summary>
    /// Determines if the provided context path matches the route's path pattern.
    /// </summary>
    /// <param name="ctx">The context containing the path to be matched against the route.</param>
    /// <returns>True if the context path matches the route's path pattern; otherwise, false.</returns>
    public bool Match(Context ctx) => Route.Match(ctx);

    public HttpHandler Handler { get; }

    /// <summary>
    /// Handles the inner execution of route matching and processing within the router.
    /// </summary>
    /// <param name="ctx">The context encapsulating the current HTTP request and response.</param>
    /// <param name="next">The function to invoke the next middleware in the pipeline if the current route handler does not terminate execution.</param>
    /// <param name="exception">An optional exception that may have occurred during the request handling.</param>
    /// <returns>A task representing the asynchronous operation of processing the route match.</returns>
    private async Task HandleInner(Context ctx, Action next, Exception? exception = null)
    {
        // pushing current router and eject after processing finished
        ctx.CurrentPath.Push(this);
        using var __ = new Disposable(() => ctx.CurrentPath.Pop());

        foreach (var match in _routes.Where(x => x.Route.Match(ctx)))
        {
            // recorded visited routes
            ctx.Visited.Enqueue(match);
            if (match is RoutePath routePath)
            {
                // parse parameters for exact route
                ctx.Parameters = routePath.Parameters(ctx);
            }

            // clear parameters after handling
            using var _ = new Disposable(() => ctx.Parameters = null);
            var navigateNext = false;
            await match.Handler(ctx, () => navigateNext = true);
            if (!navigateNext)
                return;
        }

        next();
    }

    /// <summary>
    /// Adds routes to the router with the specified HTTP method, path, and handlers.
    /// </summary>
    /// <param name="method">The HTTP method to associate with the specified path and handlers.</param>
    /// <param name="path">The path for which the handlers should be registered.</param>
    /// <param name="handlers">An array of functions to handle requests that match the specified path and method.</param>
    /// <returns>The current instance of the router to allow for method chaining.</returns>
    private Router AddRoutes(HttpMethod method, string path, params HttpHandler[] handlers)
    {
        foreach (var handler in handlers)
        {
            _routes.Add(new RoutePath(method, path, handler, _routerConfig));
        }

        return this;
    }

    public IEnumerator<IMatch> GetEnumerator()
    {
        return _routes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}