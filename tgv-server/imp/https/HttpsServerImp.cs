using System.Net;
using NetCoreServer;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_server.imp.https;

public class HttpsServerImp(
    ServerHandler handler,
    Logger logger,
    SslContext context,
    IPEndPoint endpoint,
    TgvSettings settings) : HttpsServer(context, endpoint)
{
    protected override SslSession CreateSession() => new HttpsSessionImp(this, handler, logger, settings);
}