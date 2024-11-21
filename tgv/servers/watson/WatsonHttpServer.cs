using tgv.core;
using tgv.imp;
using WatsonWebserver.Core;
using WatsonWebserver.Lite;

namespace tgv.servers;

public class WatsonHttpServer : IServer
{
    private readonly HttpHandler _httpHandler;
    private readonly IRouter _root;
    private readonly AppConfig _cfg;
    private WebserverLite? _server;

    public WatsonHttpServer(HttpHandler httpHandler): this(httpHandler, new AppConfig()) {}
    public WatsonHttpServer(HttpHandler httpHandler, AppConfig? cfg = null)
    {
        _httpHandler = httpHandler;
        _cfg = cfg ?? new AppConfig();
        Logger = new Logger();
    }

    public bool IsListening => _server?.IsListening == true;
    public bool IsHttps => _server?.Settings?.Ssl?.Enable == true;
    public int Port => _server?.Settings?.Port ?? -1;
    public Logger Logger { get; }
    public Task StartAsync(int port)
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
        await _httpHandler(ctx);
    }

    public void Stop()
    {
        if (_server != null)
        {
            Logger.Info("Stopping WatsonHttpServer");
            _server?.Stop();
        }
    }
}