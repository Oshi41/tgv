using System.Net;
using NetCoreServer;
using NLog;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_server.imp.https;

public class HttpsServerImp(
    ServerHandler handler,
    SslContext context,
    IPEndPoint endpoint,
    TgvSettings settings) : HttpsServer(context, endpoint)
{
    protected override SslSession CreateSession() => new HttpsSessionImp(this, handler, settings);
}