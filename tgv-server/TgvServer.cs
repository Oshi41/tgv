using System;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using NetCoreServer;
using tgv_core.api;
using tgv_core.imp;
using tgv_server.imp.http;
using tgv_server.imp.https;
using IServer = tgv_core.api.IServer;
using ITransportServer = NetCoreServer.IServer;

namespace tgv_server;

public class TgvServer : IServer
{
    private readonly TgvSettings _tgvSettings;
    private ITransportServer? _server;
    
    public TgvServer(TgvSettings tgvSettings, ServerHandler handler, Logger logger)
        : base(handler, logger)
    {
        _tgvSettings = tgvSettings;
    }

    public override bool IsListening => _server?.IsStarted == true;
    public override bool IsHttps => _tgvSettings.Certificate != null;

    public override int Port
    {
        get
        {
            if (_server == null) return -1;

            var endpoint = _server.Endpoint;
            return endpoint switch
            {
                IPEndPoint ip => ip.Port,
                
                _ => -1,
            };
        }
    }
    public override Task StartAsync(int port)
    {
        Stop();

        var endpoint = new IPEndPoint(IPAddress.Any, port);
        if (_tgvSettings.Certificate != null)
        {
            _server = new HttpsServerImp(_handler, Logger,
                new SslContext(SslProtocols.Tls12, _tgvSettings.Certificate, _tgvSettings.CertificateValidation),
                endpoint, _tgvSettings);
        }
        else
        {
            _server = new HttpServerImp(_handler, Logger, endpoint, _tgvSettings);
        }

        if (!_server.Start())
        {
            throw new Exception("Server cannot be started");
        }
        
        return Task.CompletedTask;
    }

    public override void Stop()
    {
        _server?.Dispose();
        _server = null;
    }
}