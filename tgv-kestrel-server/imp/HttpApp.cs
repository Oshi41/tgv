using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using tgv_common.api;
using tgv_common.imp;

namespace tgv_kestrel_server.imp;

public class HttpApp : IHttpApplication<KestrelContext>
{
    private readonly ServerHandler _handler;
    private readonly Logger _logger;
    private readonly IHttpContextFactory _factory;

    public HttpApp(ServerHandler handler, Logger logger)
    {
        _factory = new HttpContextFactory();
        _handler = handler;
        _logger = logger;
    }

    public KestrelContext CreateContext(IFeatureCollection contextFeatures)
    {
        var ctx = _factory.Create(contextFeatures);
        return new KestrelContext(ctx, _logger);
    }

    public Task ProcessRequestAsync(KestrelContext context) => _handler(context);

    public void DisposeContext(KestrelContext context, Exception exception)
    {
        context?.Dispose();
    }
}