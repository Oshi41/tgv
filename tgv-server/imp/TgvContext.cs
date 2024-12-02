using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_server.imp;

public class TgvContext : Context
{
    public TgvContext(HttpMethod method, Guid traceId, Uri url, Logger logger, NameValueCollection? headers, NameValueCollection? query) 
        : base(method, traceId, url, logger, headers, query)
    {
    }

    public override bool WasSent => throw new NotImplementedException();
    public override Task<string> Body()
    {
        throw new NotImplementedException();
    }

    public override Task Redirect(string path, HttpStatusCode code = HttpStatusCode.Moved)
    {
        throw new NotImplementedException();
    }

    protected override Task SendRaw(byte[]? bytes, HttpStatusCode code, string? contentType)
    {
        throw new NotImplementedException();
    }

    protected override Task SendRaw(Stream stream, HttpStatusCode code, string contentType)
    {
        throw new NotImplementedException();
    }
}