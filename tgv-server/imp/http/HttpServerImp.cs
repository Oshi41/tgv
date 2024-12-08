using System.Net;
using NetCoreServer;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_server.imp.http;

public class HttpServerImp(ServerHandler handler, Logger logger, IPEndPoint endpoint, TgvSettings settings) : HttpServer(endpoint)
{
    protected override TcpSession CreateSession() => new HttpSessionImp(this, handler, logger, settings);
}