using System.Diagnostics.Metrics;
using System.Net;
using NetCoreServer;
using tgv_core.api;

namespace tgv_server.imp.https;

public class HttpsServerImp : HttpsServer
{
    private readonly ServerHandler _handler;
    private readonly TgvSettings _settings;
    private readonly Meter _metric;

    public HttpsServerImp(ServerHandler handler,
        SslContext context,
        IPEndPoint endpoint,
        TgvSettings settings,
        Meter metric) : base(context, endpoint)
    {
        _handler = handler;
        _settings = settings;
        _metric = metric;
        OptionKeepAlive = true;
    }

    protected override SslSession CreateSession() => new HttpsSessionImp(this, _handler, _settings, _metric);
}