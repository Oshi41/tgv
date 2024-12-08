using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NetCoreServer;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_server;

public class TgvContext : Context
{
    private readonly IHttpSession _session;
    private readonly TgvSettings _settings;
    private bool _wasSent;

    public TgvContext(IHttpSession session, Logger logger, TgvSettings settings, ref EventHandler afterSent) 
        : base(new HttpMethod(session.Request.Method.ToUpperInvariant()),
            session.Id, 
            GetUri(session.Request),
            logger,
            new(session.Request.Headers),
            null,
            session.Request.Cookies)
    {
        _session = session;
        _settings = settings;
        afterSent += (_, _) => AfterSending();
    }


    public override bool WasSent => _wasSent;
    public override Task<string> Body() => Task.FromResult(_session.Request.Body);

    private async Task<HttpResponse> CreateResponse(HttpStatusCode code)
    {
        if (_settings.AddServerHeader)
            ResponseHeaders["Server"] = "netcoreserver-tgv";
        
        await BeforeSending();
        var resp = _session.Response;
        
        resp.Clear();
        resp.SetBegin((int)code);
        foreach (var key in ResponseHeaders.AllKeys)
            resp.SetHeader(key, ResponseHeaders[key]);
        
        // rising a flag that response was sent so no more data can be sent to client
        _wasSent = true;
        return resp;
    }

    public override async Task Redirect(string path, HttpStatusCode code = HttpStatusCode.Moved)
    {
        ResponseHeaders["Location"] = path;
        var resp = await CreateResponse(code);
        resp.SetBody();
        if (!_session.SendResponseAsync(resp))
            throw new Exception("Response cannot be sent");
    }

    protected override async Task SendRaw(byte[]? bytes, HttpStatusCode code, string? contentType)
    {
        if (contentType != null)
            ResponseHeaders["Content-Type"] = contentType;
        
        var resp = await CreateResponse(code);
        
        if (bytes != null)
            resp.SetBody(bytes);
        else
            resp.SetBody();

        if (!_session.SendResponseAsync(resp))
            throw new Exception("Response cannot be sent");

    }

    protected override async Task SendRaw(Stream stream, HttpStatusCode code, string contentType)
    {
        if (contentType != null)
            ResponseHeaders["Content-Type"] = contentType;
        
        var resp = await CreateResponse(code);

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        resp.SetBody(ms.ToArray());
        
        if (!_session.SendResponseAsync(resp))
            throw new Exception("Response cannot be sent");
    }

    private static Uri GetUri(HttpRequest request)
    {
        if (!Uri.TryCreate(request.Url, UriKind.RelativeOrAbsolute, out var uri))
        {
            throw new Exception("Invalid URI: " + request.Url);
        }

        if (!uri.IsAbsoluteUri)
        {
            var host = request.Headers["Host"];
            if (Uri.TryCreate(host, UriKind.Absolute, out var url2))
            {
                if (Uri.TryCreate(url2, request.Url, out url2))
                    return url2;
            }
        }

        return uri;
    }
}
