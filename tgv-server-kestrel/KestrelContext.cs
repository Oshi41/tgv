using System.Net;
using System.Text;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_server_kestrel;

public class KestrelContext : Context
{
    private readonly HttpContext _context;
    private string? _body;

    public KestrelContext(Guid traceId, HttpContext context, Logger logger)
        : base(
            new HttpMethod(context.Request.Method),
            traceId,
            new Uri(
                $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}"),
            logger,
            context.Request.Headers.Convert(),
            context.Request.Query.Convert())
    {
        _context = context;
    }

    public override bool WasSent => _context.Response.HasStarted;

    protected override async Task BeforeSending()
    {
        await base.BeforeSending();

        foreach (var key in ResponseHeaders.AllKeys) 
            _context.Response.Headers[key] = ResponseHeaders[key];
    }

    public override async Task<string> Body()
    {
        if (_body == null && _context.Request.Body.CanRead)
        {
            var task = await _context.Request.BodyReader.ReadAsync();
            _body = Encoding.UTF8.GetString(task.Buffer);
        }

        return _body ?? string.Empty;
    }

    public override async Task Redirect(string path, HttpStatusCode code = HttpStatusCode.Moved)
    {
        _context.Response.StatusCode = (int)code;
        ResponseHeaders["Location"] = path;
        await BeforeSending();
        await _context.Response.CompleteAsync();
        await AfterSending();
    }

    protected override async Task SendRaw(byte[]? bytes, HttpStatusCode code, string? contentType)
    {
        _context.Response.StatusCode = (int)code;
        if (!string.IsNullOrEmpty(contentType)) _context.Response.ContentType = contentType;
        
        
        await BeforeSending();
        if (bytes != null) await _context.Response.Body.WriteAsync(bytes);

        await _context.Response.CompleteAsync();
        await AfterSending();
    }

    protected override async Task SendRaw(Stream stream, HttpStatusCode code, string contentType)
    {
        _context.Response.StatusCode = (int)code;
        if (!string.IsNullOrEmpty(contentType)) _context.Response.ContentType = contentType;
        
        await BeforeSending();
        await stream.CopyToAsync(_context.Response.Body);
        await _context.Response.CompleteAsync();
        await AfterSending();
    }
}