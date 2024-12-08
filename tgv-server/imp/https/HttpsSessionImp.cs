using System;
using NetCoreServer;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_server.imp.https;

public class HttpsSessionImp: HttpsSession
{
    private readonly ServerHandler _handler;
    private readonly Logger _logger;
    private readonly TgvSettings _settings;
    private Context _ctx;

    public HttpsSessionImp(HttpsServer server, ServerHandler handler, Logger logger, TgvSettings settings)
        : base(server)
    {
        _handler = handler;
        _logger = logger;
        _settings = settings;
    }
    
    public event EventHandler OnWasSent;

    private Context CreateContext()
    {
        _ctx?.Dispose();
        return _ctx = new TgvContext(this, _logger, _settings, ref OnWasSent);
    }

    protected override async void OnReceivedRequest(HttpRequest request)
    {
        try
        {
            await _handler(CreateContext());
        }
        catch (Exception e)
        {
            _logger.Error($"Error during request receive: {e}");
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
            _logger.Error($"Error during request error handling: {e}");
        }
    }

    protected override void OnSent(long sent, long pending)
    {
        if (pending == 0) OnWasSent?.Invoke(this, EventArgs.Empty);
    }
}