using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Options;
using tgv_common.api;
using tgv_common.imp;
using tgv_kestrel_server.imp;
using HttpServer = Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServer;

namespace tgv_kestrel_server;

public class KestrelServer : IServer, IApplicationLifetime
{
    private readonly KestrelSettings _cgf;

    private HttpServer? _httpServer;

    public KestrelServer(ServerHandler handler, Logger? logger = null, KestrelSettings? cgf = null)
        : base(handler, logger ?? new Logger())
    {
        _cgf = cgf ?? new KestrelSettings();
    }

    public override bool IsListening { get; }
    public override bool IsHttps { get; }
    public override int Port { get; }

    public override async Task StartAsync(int port)
    {
        Stop();

        var config = _cgf.Convert();
        config.Listen(IPAddress.Any, port, options => { options.Protocols = HttpProtocols.Http1AndHttp2; });

        var factory = new LoggerFactory(Logger);

        _httpServer = new HttpServer(
            new OptionsWrapper<KestrelServerOptions>(config),
            new SocketTransportFactory(
                new OptionsWrapper<SocketTransportOptions>(new SocketTransportOptions
                {
                    IOQueueCount = 0,
                }),
                this,
                factory
            ),
            factory);

        await _httpServer.StartAsync(new HttpApp(_handler, Logger), CancellationToken.None);
    }

    public override void Stop()
    {
        if (_httpServer == null) return;

        Logger.Info("Stopping KestrelServer");
        _httpServer.StopAsync(CancellationToken.None).Wait();
    }

    public void StopApplication()
    {
        Stop();
    }

    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;
}