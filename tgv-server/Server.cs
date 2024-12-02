using System.IO.Pipelines;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using tgv_core.api;
using tgv_core.imp;
using tgv_server.api;
using tgv_server.imp;

namespace tgv_server;

public class Server : IServer
{
    private readonly Settings _settings;
    private readonly PipeScheduler _scheduler;
    private readonly IHttpParser<TgvContext> _parser;
    
    private IListener<TgvContext>? _listener;
    private int _port;

    public Server(Settings settings, ServerHandler handler, Logger logger) : base(handler, logger)
    {
        _settings = settings;
        _scheduler = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? PipeScheduler.ThreadPool
            : PipeScheduler.Inline;
        _parser = new imp.HttpParser();
    }

    public override bool IsListening => _listener?.IsRunning == true;
    public override bool IsHttps => _settings.Certificate != null;
    public override int Port => _port;

    public override Task StartAsync(int port)
    {
        Stop();
        
        _port = port;
        _listener = new imp.SocketListener(new IPEndPoint(IPAddress.Any, port), _settings, _scheduler, _parser);
        _listener.Run(_handler, CancellationToken.None);
        return Task.CompletedTask;
    }

    public override void Stop() => _listener?.Stop();
}