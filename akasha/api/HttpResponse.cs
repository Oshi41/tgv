using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace akasha.api;

public class HttpResponse
{
    static Uri DefaultUrl = new Uri("https://www.google.com/");
    
    public Version? Protocol { get; internal set; }
    public HttpStatusCode? Code { get; internal set; }
    public string? StatusMessage { get; internal set; }
    public NameValueCollection? Headers { get; internal set; }
    public CookieCollection? Cookies { get; internal set; }
    public Stream? Body { get; internal set; }

    public byte[] GetHttpWithoutBody()
    {
        if (Protocol == null) throw new ArgumentException(nameof(Protocol));
        if (Code == null) throw new ArgumentException(nameof(Code));
        if (Headers == null || !Headers.HasKeys()) throw new ArgumentException(nameof(Headers));
        if (Code == null) throw new ArgumentException(nameof(Code));
        
        if (StatusMessage == null) StatusMessage = Code.ToString();

        if (Cookies != null)
        {
            // some hack here
            var container = new CookieContainer();
            container.Add(DefaultUrl, Cookies);
            Headers["Set-Cookie"] = container.GetCookieHeader(DefaultUrl);
        }
        
        var sb = new StringBuilder();
        sb.AppendLine($"HTTP/{Protocol} {(int)Code} {StatusMessage}");
        foreach (string key in Headers)
        {
            sb.AppendLine($"{key}: {Headers[key]}");
        }

        // body delmitter
        sb.AppendLine();
        
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}