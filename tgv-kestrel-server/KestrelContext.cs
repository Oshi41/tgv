using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using tgv_core.api;
using tgv_core.imp;
using tgv_kestrel_server.extensions;

namespace tgv_kestrel_server;

public class KestrelContext : Context
{
    private readonly HttpContext _ctx;
    private string? _body;

    public KestrelContext(HttpContext ctx, Logger logger)
        : base(
            new HttpMethod(ctx.Request.Method.ToUpper()),
            new HttpMethod(ctx.Request.Method.ToUpper()),
            Guid.Empty, // ctx.TraceIdentifier
            new Uri(ctx.Request.Host.ToString()),
            logger,
            ctx.Request.Headers.Convert(),
            ctx.Request.Query.Convert())
    {
        _ctx = ctx;
    }

    public override string ContentType
    {
        get => _ctx.Response.ContentType;
        set => _ctx.Response.ContentType = value;
    }

    public override bool WasSent => _ctx?.Response?.HasStarted == true;

    public override async Task<string> Body()
    {
        if (string.IsNullOrEmpty(_body) && _ctx.Request.Body.CanRead)
        {
            try
            {
                using var ms = new MemoryStream();
                await _ctx.Request.Body.CopyToAsync(ms);
                var bytes = ms.ToArray();
                _body = Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error while reading body: {ex}");
            }
            finally
            {
                _body ??= string.Empty;
            }
        }

        return _body;
    }

    public override Task Redirect(string path, HttpStatusCode code = HttpStatusCode.Moved)
    {
        throw new NotImplementedException();
    }

    protected override async Task SendRaw(byte[] bytes, int code, string contentType)
    {
        ContentType = contentType;
        _ctx.Response.StatusCode = code;
        
        await BeforeSending();
        await _ctx.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        await AfterSending();
    }

    protected override async Task SendRaw(Stream stream, int code, string contentType)
    {
        ContentType = contentType;
        _ctx.Response.StatusCode = code;
        
        await BeforeSending();
        await stream.CopyToAsync(_ctx.Response.Body);
        await AfterSending();
    }
}