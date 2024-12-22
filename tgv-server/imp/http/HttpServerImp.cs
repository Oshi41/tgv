using System.Diagnostics.Metrics;
using System.Net;
using NetCoreServer;
using tgv_core.api;

namespace tgv_server.imp.http;

public class HttpServerImp : HttpServer
{
    private readonly ServerHandler _handler;
    private readonly TgvSettings _settings;

    public HttpServerImp(ServerHandler handler, IPEndPoint endpoint, TgvSettings settings) : base(endpoint)
    {
        _handler = handler;
        _settings = settings;
        OptionKeepAlive = true;
    }

    protected override TcpSession CreateSession() => new HttpSessionImp(this, _handler, _settings);
}