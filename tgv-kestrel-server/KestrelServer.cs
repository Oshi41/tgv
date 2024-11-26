using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Options;
using tgv_core.api;
using tgv_core.imp;
using tgv_kestrel_server.imp;
using HttpServer = Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServer;

namespace tgv_kestrel_server;

public class KestrelServer : IServer, IApplicationLifetime
{
    private int _port = -1;
    private readonly KestrelSettings _cgf;
    private HttpServer? _httpServer;
    private bool _started;

    public KestrelServer(ServerHandler handler, Logger? logger = null, KestrelSettings? cgf = null)
        : base(handler, logger ?? new Logger())
    {
        _cgf = cgf ?? new KestrelSettings();

    }

    public override bool IsListening => _httpServer != null && _started;

    public override bool IsHttps => _cgf.Https.ServerCertificateSelector != null
                                    || _cgf.Https.ServerCertificate != null;

    public override int Port => _httpServer != null ? _port : -1;

    public override async Task StartAsync(int port)
    {
        _port = -1;
        Stop();
        

        var config = _cgf.Convert();
        config.ListenAnyIP(port, options =>
        {
            options.Protocols = HttpProtocols.Http1AndHttp2;
        });

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
        
        var addresses = _httpServer.Features.Get<IServerAddressesFeature>().Addresses;
        addresses.Add($"http://0.0.0.0:{port}");
        addresses.Add($"https://0.0.0.0:{port}");

        await _httpServer.StartAsync(new HttpApp(_handler, Logger), CancellationToken.None);
        _port = port;
        _started = true;
    }

    public override void Stop()
    {
        _started = false;
        if (_httpServer == null) return;

        Logger.Info("Stopping KestrelServer");
        _httpServer.StopAsync(CancellationToken.None).Wait();
        _httpServer.Dispose();
        _httpServer = null;
    }

    public void StopApplication()
    {
        Stop();
    }

    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;
}