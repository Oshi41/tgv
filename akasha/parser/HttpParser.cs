using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using akasha.api;
using akasha.extensions;
using FormatException = System.FormatException;

namespace akasha.parser;

public static class HttpParser
{
    private static Uri DefaultUri = new("http://localhost:5000");

    public static async Task<HttpRequest> ParseHttpRequest(this Stream stream, int chunkSize = 2048)
    {
        var data = new HttpRequest();
        var headersReceived = false;

        await foreach (var e in stream.ByLineAsync(chunkSize))
        {
            e.GetChunk(out var span);
            if (data.Method == null || data.Protocol == null || data.Uri == null)
            {
                // GET POST PUT 
                var index = span.IndexOf(" "u8);
                if (index < 0) throw new FormatException("Invalid protocol HTTP line");
                data.Method = new HttpMethod(Encoding.UTF8.GetString(span.Slice(0, index).ToArray()));
                span = span.Slice(index + 1);


                // \index.html \path\to\url
                index = span.IndexOf(" "u8);
                if (index < 0) throw new FormatException("Invalid protocol HTTP line");
                data.Uri = span.Slice(0, index).ToUtf8String();
                span = span.Slice(index + 1);


                // HTTP/1.1 HTTP/0.9
                index = span.LastIndexOf("/"u8);
                if (index < 0) throw new FormatException("Invalid protocol HTTP line");
                if (!Version.TryParse(Encoding.UTF8.GetString(span.Slice(index + 1).ToArray()), out var version))
                    throw new FormatException("Invalid protocol HTTP line");
                data.Protocol = version;

                continue;
            }

            if (!headersReceived && span.IsEmpty)
            {
                var cookies = data.Headers?.GetValues("Cookie");
                if (cookies?.Any() == true)
                {
                    var container = new CookieContainer();
                    data.Cookies ??= new CookieCollection();

                    foreach (var value in cookies)
                        container.SetCookies(DefaultUri, value);

                    data.Cookies.Add(container.GetCookies(DefaultUri));
                }

                // flush all the data
                data.Body ??= new BufferStream();
                var (buffer, offset, count) = e.StopAndFlush();
                _ = WriteToBody(data, buffer, offset, count);
                continue;
            }

            if (!headersReceived)
            {
                var index = span.IndexOf(":"u8);
                if (index < 0) throw new FormatException("Invalid HTTP header line");

                var key = Encoding.UTF8.GetString(span.Slice(0, index).ToArray()).Trim();
                var value = Encoding.UTF8.GetString(span.Slice(index + 1).ToArray()).Trim();

                data.Headers ??= new();
                data.Headers.Add(key, value);
                continue;
            }
        }

        // starting async copy
        _ = ReadBodyAsync(stream, data, chunkSize);

        return data;
    }

    private static async Task ReadBodyAsync(Stream stream, HttpRequest data, int chunkSize)
    {
        data.Body ??= new BufferStream();
        var buffer = new byte[chunkSize];
        var read = 0;

        while ((read = await stream.ReadAsync(buffer, 0, chunkSize)) > 0)
        {
            await WriteToBody(data, buffer, 0, read);
        }
    }

    private static async Task WriteToBody(HttpRequest data, byte[] buffer, int offset, int count)
    {
        // remove body separator occurance
        if (buffer.AsSpan(offset, count).EndsWith("\r\n\r\n"u8))
        {
            count -= 4;
        }

        await data.Body!.WriteAsync(buffer, offset, count);
    }
}