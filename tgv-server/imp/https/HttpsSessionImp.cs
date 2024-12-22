using System;
using System.Diagnostics.Metrics;
using System.IO;
using NetCoreServer;
using NLog;
using tgv_core.api;
using tgv_core.extensions;
using tgv_server.api;

namespace tgv_server.imp.https;

public class HttpsSessionImp: HttpsSession, IStreamProvider
{
    private readonly Logger _logger;
    
    private readonly ServerHandler _handler;
    private readonly TgvSettings _settings;
    private readonly Meter _metric;
    private Context _ctx;

    public HttpsSessionImp(HttpsServer server, ServerHandler handler, TgvSettings settings, Meter metric)
        : base(server)
    {
        _handler = handler;
        _settings = settings;
        _metric = metric;
        _logger = LogManager.LogFactory.GetLogger("HttpsSessionImp");
    }
    
    public event EventHandler OnWasSent;

    private Context CreateContext()
    {
        _ctx?.Dispose();
        return _ctx = new TgvContext(this, _settings, ref OnWasSent, _metric);
    }

    protected override async void OnReceivedRequest(HttpRequest request)
    {
        try
        {
            await _handler(CreateContext());
        }
        catch (Exception e)
        {
            _logger.Error("Error during request receive: {e}", e);
        }
    }

    protected override async void OnReceivedRequestError(HttpRequest request, string error)
    {
        try
        {
            await _handler(CreateContext(), new Exception(error));
        }
        catch (Exception e)
        {
            _logger.Error("Error during request error handling: {e}", e);
        }
    }

    protected override void OnSent(long sent, long pending)
    {
        if (sent > 0)
        {
            _metric.CreateCounter<long>("server_bytes_sent", description: "How much bytes session sent")
                .Add(sent, _ctx.ToTagsFull());
            _metric.CreateCounter<long>("http_bytes_sent", description: "How much bytes HTTP session sent")
                .Add(sent, _ctx.ToTagsFull());
        }
        if (pending == 0) OnWasSent?.Invoke(this, EventArgs.Empty);
    }

    public Stream? GetStream() => _sslStream;
}