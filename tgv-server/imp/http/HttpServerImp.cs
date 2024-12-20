using System.Net;
using NetCoreServer;
using NLog;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_server.imp.http;

public class HttpServerImp(ServerHandler handler, IPEndPoint endpoint, TgvSettings settings) : HttpServer(endpoint)
{
    protected override TcpSession CreateSession() => new HttpSessionImp(this, handler, settings);
}