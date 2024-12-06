using System.Net;
using tgv_core.api;
using tgv_core.extensions;
using tgv_core.imp;
using IRouter = tgv_core.api.IRouter;

namespace tgv_server_kestrel;

public class KestrelServer : IServer
{
    private readonly KestrelSettings _settings;
    private readonly Router _root;

    private long _connectionId = 0;
    private WebApplication? _app;
    private int _port;

    public KestrelServer(KestrelSettings settings, Router root, Logger logger)
        : base(_ => Task.CompletedTask, logger)
    {
        _settings = settings;
        _root = root;
    }

    private Task Handle(HttpContext context, Func<Task> next)
    {
        var id = Interlocked.Increment(ref _connectionId).CreateId();
        var logger = Logger.WithCustomMessage((_, message, _, _, _)
            => $"[{id}][{context.Request.Method} {context.Request.Path}] {message}");
        var ctx = new KestrelContext(id, context, logger);
        return _handler(ctx);
    }

    public override bool IsListening => _app != null;
    public override bool IsHttps => _settings.Certificate != null;
    public override int Port => _port;

    public override Task StartAsync(int port)
    {
        Stop();
        _port = port;
        _connectionId = 0;

        var builder = WebApplication.CreateSlimBuilder();
        builder.Environment.ApplicationName = "TGVServerKestrel";
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Any, _port, listenOptions =>
            {
                if (IsHttps) listenOptions.UseHttps(_settings.Certificate!);

                listenOptions.Protocols = _settings.Protocols;
            });

            options.AddServerHeader = false;
        });
        _app = builder.Build();
        _app.Use((context, next) =>
        {
            context.Features.Set(context.Convert(Logger));
            return next();
        });
        _app.Apply(_root);
        return _app.StartAsync();
    }

    public override void Stop()
    {
        _app?.StopAsync().Wait();
        _app?.DisposeAsync().GetAwaiter().GetResult();
        _app = null;
    }
}