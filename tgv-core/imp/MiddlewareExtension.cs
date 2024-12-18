using System.Net.Http;
using tgv_core.api;

namespace tgv_core.imp;

internal class MiddlewareExtension : IMatch
{
    internal MiddlewareExtension(HttpMethod method, string path, IExtensionFactoryInternal factory, RouterConfig config)
    {
        Handler = async (ctx, next, _) =>
        {
            await factory.FillContext(ctx);
            next();
        };
        Route = new RoutePath(method, path, Handler, config, true);
    }

    public RoutePath Route { get; }
    public HttpHandler Handler { get; }
}