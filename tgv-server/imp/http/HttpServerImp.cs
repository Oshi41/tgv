using System.Net;
using System.Threading.Tasks;
using NetCoreServer;
using NLog;
using tgv_core.api;
using tgv_core.imp;

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