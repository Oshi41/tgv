using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using akasha.extensions;

namespace akasha.api;

public class HttpResponse
{
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
            foreach (Cookie cookie in Cookies)
            {
                Headers.Add("Set-Cookie", cookie.ToHttpHeader());
            }
        }
        
        var sb = new StringBuilder();
        sb.AppendLine($"HTTP/{Protocol} {(int)Code} {StatusMessage}");
        foreach (string key in Headers)
        {
            foreach (var value in Headers.GetValues(key))
            {
                sb.AppendLine($"{key}: {value}");
            }
        }

        // body delmitter
        sb.AppendLine();
        
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}