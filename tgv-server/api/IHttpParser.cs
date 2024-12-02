using System;
using System.Collections.Specialized;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using tgv_core.api;

namespace tgv_server.api;

public record HttpParsed(HttpMethod Method, Uri Url, Version Protocol, NameValueCollection Headers, byte[] Body);

public interface IHttpParser<T>
    where T : Context
{
    Task<T> Parse(Pipe socket);
}