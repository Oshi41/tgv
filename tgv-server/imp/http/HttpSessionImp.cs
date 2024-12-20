using System;
using NetCoreServer;
using NLog;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_server.imp.http;

public class HttpSessionImp : HttpSession
{
    private readonly ServerHandler _handler;
    private readonly Logger _logger;
    private readonly TgvSettings _settings;
    private Context _ctx;

    public HttpSessionImp(HttpServer server, ServerHandler handler, TgvSettings settings)
        : base(server)
    {
        _handler = handler;
        _logger = LogManager.LogFactory.GetLogger("HttpSessionImp");
        _settings = settings;
    }

    public event EventHandler OnWasSent;

    private Context CreateContext()
    {
        _ctx?.Dispose();
        return _ctx = new TgvContext(this, _settings, ref OnWasSent);
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
        if (pending == 0) OnWasSent?.Invoke(this, EventArgs.Empty);
    }
}