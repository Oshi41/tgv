using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace tgv_kestrel_server.imp;

public class HttpContextFactory : IHttpContextFactory
{
    public HttpContext Create(IFeatureCollection featureCollection)
    {
        var ctx = new DefaultHttpContext(featureCollection);
        ctx.Initialize(featureCollection);
        return ctx;
    }

    public void Dispose(HttpContext httpContext)
    {
        // ignore
    }
}