using System.Diagnostics;
using System.Net;
using sharp_express.core;

namespace sharp_express.imp;

public class HttpServer
{
    private readonly IRouter _router;
    private readonly AppConfig _config;

    private HttpListener _listener;
    private string _host = "localhost";
    private int _port;

    public HttpServer(IRouter router, AppConfig config)
    {
        _router = router;
        _config = config;
    }

    public string Url => _listener?.Prefixes?.First();

    public void Start(int port = 7000)
    {
        _port = port;

        Stop();
        Debug.WriteLine("Starting HTTP server...");
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://{_host}:{_port}/");
        _listener.Start();
        Debug.WriteLine("HTTP server started on {0} port", _port);
        _ = Listen();
    }

    public bool Stop()
    {
        if (_listener == null) return false;
        
        Debug.WriteLine("Stopping the server");
        _listener.Stop();
        return true;
    }

    private async Task Listen()
    {
        Debug.WriteLine($"Listening on {Url}");

        while (_listener.IsListening)
        {
            _ = Handle(await _listener.GetContextAsync());
        }
    }

    private async Task Handle(HttpListenerContext context)
    {
        Debug.WriteLine($"HTTP[{context.Request.HttpMethod}] {context.Request.Url.AbsolutePath}]");
        context.Response.ContentType = _config.DefaultContentType;
        using var _ = context.Response;

        IContext ctx = new Context(context);
        context.Response.Cookies = context.Request.Cookies;

        foreach (var stage in new[] { HandleStages.Before, HandleStages.Handle, HandleStages.After })
        {
            if (!await HandleStage(ctx, stage, context.Response)) break;
        }

        if (ctx.Stage is HandleStages.SendingError or HandleStages.SendingOk)
        {
            if (ctx.Result == null)
            {
                ctx.Send(ctx.Stage == HandleStages.SendingError
                    ? HttpStatusCode.InternalServerError
                    : HttpStatusCode.OK);
            }

            if (ctx.Result != null)
                await context.Response.OutputStream.WriteAsync(ctx.Result, 0, ctx.Result.Length);

            context.Response.Close();
            ctx.Stage = HandleStages.Sent;
        }
    }

    private async Task<bool> HandleStage(IContext ctx, HandleStages stage, HttpListenerResponse response,
        Exception ex = null)
    {
        try
        {
            ctx.Stage = stage;
            var next = false;
            await _router.Handler(ctx, () => { next = true; }, ex);

            // finish handling regular flow
            if (!next || ctx.Stage != HandleStages.Error)
            {
                ctx.Stage = HandleStages.SendingOk;
            }

            // finished handling error
            if (ctx.Stage == HandleStages.Error)
            {
                ctx.Stage = HandleStages.SendingError;
            }

            return next;
        }
        catch (Exception e)
        {
            if (ctx.Stage == HandleStages.Error)
            {
                // fatal error, cannot handle by ourself
                Console.Error.WriteLine("Fatal Error during HTTP handling: {0}", e);
                ctx.Send(HttpStatusCode.InternalServerError);
                ctx.Stage = HandleStages.SendingError;
                return false;
            }

            // handling errors
            return await HandleStage(ctx, HandleStages.Error, response, e);
        }
    }
}