using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;

namespace akasha.api;

public class HttpRequest
{
    public HttpMethod? Method { get; internal set; }
    public Uri? Uri { get; internal set; }
    public Version? Protocol { get; internal set; }
    public NameValueCollection? Headers { get; internal set; }
    public CookieCollection? Cookies { get; internal set; }
    public Stream? Body { get; internal set; }
}