using System;
using System.Net;
using System.Threading.Tasks;
using NetCoreServer;
using tgv_server.imp.http;
using tgv_server.imp.https;
using IServer = tgv_core.api.IServer;
using ITransportServer = NetCoreServer.api.IServer;

namespace tgv_server;

public class TgvServer(TgvSettings tgvSettings) : IServer
{
    private ITransportServer? _server;

    public override bool IsListening => _server?.IsStarted == true;
    public override bool IsHttps => tgvSettings.Certificate != null;

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
        if (tgvSettings.Certificate != null)
        {
            _server = new HttpsServerImp(Handler,
                new SslContext(tgvSettings.Protocols, tgvSettings.Certificate, tgvSettings.CertificateValidation),
                endpoint, tgvSettings);
        }
        else
        {
            _server = new HttpServerImp(Handler, endpoint, tgvSettings);
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