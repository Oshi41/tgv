using System.Net.Http;
using tgv_core.api;

namespace tgv_core.imp;

public class MiddlewareExtension : IMatch
{
   

    public MiddlewareExtension(HttpMethod method, string path, IExtensionFactoryInternal factory, RouterConfig config)
    {
        Factory = factory;
        Handler = async (ctx, next, _) =>
        {
            await Factory.FillContext(ctx);
            next();
        };
        Route = new RoutePath(method, path, Handler, config, true);
    }

    public RoutePath Route { get; }
    public HttpHandler Handler { get; }
    public IExtensionFactoryInternal Factory { get; }
}