using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using MimeTypes;
using NetCoreServer;
using tgv_core.api;
using tgv_core.extensions;
using tgv_server.api;
using HttpClient = System.Net.Http.HttpClient;

namespace tgv_server;

public class TgvContext : Context
{
    private static readonly byte[] _newLine = "\r\n"u8.ToArray(); 
    private readonly IHttpSession _session;
    private readonly TgvSettings _settings;
    private bool _wasSent;

    public TgvContext(IHttpSession session, TgvSettings settings, ref EventHandler afterSent)
        : base(new HttpMethod(session.Request.Method.ToUpperInvariant()),
            session.Id,
            GetUri(session.Request),
            new(session.Request.Headers),
            null,
            session.Request.Cookies)
    {
        _session = session;
        _settings = settings;
        afterSent += (_, _) => AfterSending();
        
        var meta = new List<string>();
        if (string.IsNullOrEmpty(_session.Request.Body))
            meta.Add("no-body");
        if (Cookies.Count > 0)
            meta.Add($"cookies={Cookies.Count}");
        if (ClientHeaders.Count > 0)
            meta.Add($"headers={ClientHeaders.Count}");

        if (meta.Any())
        {
            Logger.WithProperty("_03meta", string.Join(", ", meta));
        }

        if (ClientHeaders["Content-Type"] == "application/x-www-form-urlencoded")
        {
            Form = new NameValueCollection();
            foreach (var strings in HttpUtility.UrlDecode(_session.Request.Body ?? "")
                         .Split(['&'], StringSplitOptions.RemoveEmptyEntries)
                         .Select(x => x.Split('='))
                         .Where(x => x.Length == 2 && x.All(x => !string.IsNullOrEmpty(x))))
            {
                Form.Add(strings[0], strings[1]);
            }
        }
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

    public override async Task SendRaw(byte[]? bytes, HttpStatusCode code, string? contentType)
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

    public override async Task SendFile(Stream stream, string filename)
    {
        if (_session is not IStreamProvider provider)
            throw new NotSupportedException($"Only {nameof(IStreamProvider)} session is supported");
        
        var socket = provider.GetStream();
        if (socket is null)
            throw new ArgumentException("Stream cannot be null", nameof(socket));
        
        // dispose after usage
        using var _ = stream;
        
        ResponseHeaders["Content-Length"] = stream.Length.ToString();
        ResponseHeaders["Content-Type"] = MimeTypeMap.GetMimeType(filename);
        ResponseHeaders["Content-Disposition"] = $"attachment; filename=\"{filename}\"";
        
        var buff = (await CreateResponse(HttpStatusCode.OK)).Cache!;
        buff.Append(_newLine);
        
        await socket.WriteAsync(buff.Data, (int)buff.Offset, (int)buff.Size);
        await stream.CopyToAsync(socket, 4096);

        // End Of Message
        for (int i = 0; i < 2; i++) await socket.WriteAsync(_newLine, 0, _newLine.Length);
        
        await socket.FlushAsync();
        await AfterSending();
    }

    protected override async Task AfterSending()
    {
        await base.AfterSending();

        Statics.GetMetric().CreateCounter<long>("response_sent", description: "HTTP response sent")
            .Add(1, this.ToTagsFull()
                .With("elapsed", _watch.Elapsed)
                .With("code", _session.Response.Status)
                .With("status", _session.Response.StatusPhrase)
                .With("sent_headers", _session.Response.Headers)
                .With("request_body", _session.Request.Body)
                .With("response_body", _session.Response.Body)
                .With("content_type", _session.Response.Headers["Content-Type"])
                .With("bytes_received", _session.BytesReceived)
                .With("bytes_sent", _session.BytesSent)
            );
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