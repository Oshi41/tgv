using System.Threading.Tasks;
using tgv_common.api;
using tgv_common.imp;
using WatsonWebserver.Core;
using WatsonWebserver.Lite;

namespace tgv_watson_server;

public class WatsonServer : IServer
{
    private readonly WatsonConfig _cfg;
    private WebserverLite? _server;

    public WatsonServer(ServerHandler httpHandler): this(httpHandler, new WatsonConfig()) {}
    public WatsonServer(ServerHandler httpHandler, WatsonConfig? cfg = null)
        : base(httpHandler, new Logger())
    {
        _cfg = cfg ?? new WatsonConfig();
    }

    public override bool IsListening => _server?.IsListening == true;
    public override bool IsHttps => _server?.Settings?.Ssl?.Enable == true;
    public override int Port => _server?.Settings?.Port ?? -1;
    public override Task StartAsync(int port)
    {
        Stop();
        
        var config = _cfg.Convert();
        config.Port = port;
        _server = new WebserverLite(config, HttpHandle);
        _server.Start();
        return Task.CompletedTask;
    }

    private async Task HttpHandle(HttpContextBase context)
    {
        using var ctx = new WatsonContext(context, Logger);
        await _handler(ctx);
    }

    public override void Stop()
    {
        if (_server != null)
        {
            Logger.Info("Stopping WatsonHttpServer");
            _server?.Stop();
        }
    }
}