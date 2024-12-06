using System.Collections.Specialized;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Routing.Tree;
using tgv_core.api;
using tgv_core.extensions;
using tgv_core.imp;
using TgvRouter = tgv_core.imp.Router;
using AspRouter = Microsoft.AspNetCore.Components.Routing.Router;

namespace tgv_server_kestrel;

public static class Extensions
{
    public static NameValueCollection Convert(this IHeaderDictionary headers)
    {
        var collection = new NameValueCollection();
        foreach (var header in headers)
            collection.Add(header.Key, header.Value);
        return collection;
    }

    public static NameValueCollection Convert(this IQueryCollection query)
    {
        var collection = new NameValueCollection();
        foreach (var queryKey in query)
            collection.Add(queryKey.Key, queryKey.Value);
        return collection;
    }

    public static KestrelContext Convert(this HttpContext context, Logger logger)
    {
        return new KestrelContext(Guid.NewGuid(), context, logger);
    }

    private static void Apply<T>(this T app, TgvRouter router, Stack<TgvRouter>? queue)
        where T : IEndpointRouteBuilder, IApplicationBuilder
    {
        bool pushed = queue != null;
        if (pushed)
        {
            queue.Push(router);
        }
        else
        {
            queue ??= new Stack<TgvRouter>();
        }

        string CreatePattern(IMatch match)
        {
            var paths = queue.Reverse().SelectMany(x => x.Route.Segments)
                .Union(match.Route.Segments)
                .SkipWhile(x => x.IsWildcard)
                .Where(x => !string.IsNullOrEmpty(x.Regex))
                .ToList();
            var result = string.Join('/', paths.Select(x =>
            {
                if (x.IsPattern) return $"{{{x.Name}}}";

                if (x.IsWildcard) return "*";

                return x.Regex;
            })).TrimEnd('/');
            
            if (!result.StartsWith('/') && !string.IsNullOrEmpty(result))
                result = $"/{result}";
            
            return result;
        }

        var routes = router._routes.ToList();
        var current = routes.Where(x => !x.Route.IsRouterPath && x.Route.Method == HttpMethodExtensions.Before)
            .Union(routes.Where(x => !x.Route.IsRouterPath && x.Route.Method.IsStandardMethod()))
            .Union(routes.Where(x => !x.Route.IsRouterPath && x.Route.Method == HttpMethodExtensions.After))
            .ToList();

        foreach (var match in current)
        {
            routes.Remove(match);

            var methods = match.Route.Method.IsStandardMethod()
                ? [match.Route.Method.Method]
                : HttpMethodExtensions.GetStandardMethods();

            var path = CreatePattern(match);
            app.MapMethods(path, methods, new Func<HttpContext, Func<Task>, Task>(async (context, next) =>
            {
                var ctx = context.Features.Get<KestrelContext>();
                ctx!.Stage = match.Route.Method;
                var shouldContinue = false;
                await match.Handler(ctx, () => { shouldContinue = true; });
                if (shouldContinue) await next();
            }));
        }

        current = routes.Where(x => x.Route.IsRouterPath && x is TgvRouter).ToList();
        foreach (var match in current.OfType<TgvRouter>())
        {
            routes.Remove(match);
            app.Apply(match, queue);
        }

        if (pushed)
            queue.Pop();
    }

    public static void Apply<T>(this T app, TgvRouter router) where T : IEndpointRouteBuilder, IApplicationBuilder
    {
        app.UseRouting();
        app.Apply(router, null);
    }
}