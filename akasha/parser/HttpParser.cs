using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices.imp;
using System.Text;
using System.Threading.Tasks;
using akasha.api;
using akasha.extensions;

namespace akasha.parser;

public static class HttpParser
{
    public static async Task<HttpRequest> ParseHttpResponse(this Stream stream, int chunkSize = 2048)
    {
        var data = new HttpRequest();
        var buffer = new byte[chunkSize];
        byte[] pending = [];
        var read = 0;
        var headersReceived = false;

        void SavePending(Span<byte> span)
        {
            Array.Clear(pending, 0, pending.Length);
            pending = span.ToArray();
        }

        while (!headersReceived && (read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            // using possible pending data
            var span = pending.Concat(buffer.AsSpan(0, read));

            // parse protocol line
            if (data.Method == null || data.Protocol == null || data.Uri == null)
            {
                var count = ParseProtocolLine(data, span);
                if (count == 0)
                {
                    SavePending(span);
                    continue;
                }

                // removing read data
                span = span.Slice(count);
            }

            // parse headers
            if (!headersReceived)
            {
                var count = ParseHeaders(data, span);
                if (count == 0)
                {
                    SavePending(span);
                    continue;
                }
                
                // removing read data
                span = span.Slice(count);
            }

            // detecting body delmitter
            if (!headersReceived)
            {
                var index = span.IndexOf("\r\n\r\n"u8);
                if (index >= 0)
                {
                    SavePending(span.Slice(0, index + 4));
                    headersReceived = true;
                }
            }
        }
        
        // starting reading body.
        // as it may block
        if (!headersReceived) throw new FormatException("No HTTP body found");

        data.Body = new BufferStream();
        
        // writing pending data
        if (pending.Length > 0)
            await data.Body.WriteAsync(pending, 0, pending.Length);
        
        // starting async copy 
        _ = stream.CopyToAsync(data.Body);

        return data;
    }
    
    private static int ParseProtocolLine(HttpRequest request, Span<byte> span)
    {
        // first linebreak
        var nlIndex = span.IndexOf("\r\n"u8);

        // doesn't receive enough data
        if (nlIndex < 0) return 0;

        var line = span.Slice(0, nlIndex);

        // GET POST PUT 
        var index = line.IndexOf(" "u8);
        if (index < 0) throw new FormatException("Invalid HTTP header line");
        request.Method = new HttpMethod(line.Slice(0, index).ToUtf8String().ToUpperInvariant());
        line = line.Slice(index + 1);


        // \index.html \path\to\url
        index = line.IndexOf(" "u8);
        if (index < 0) throw new FormatException("Invalid HTTP header line");
        if (!Uri.TryCreate(line.Slice(0, index).ToUtf8String(), UriKind.RelativeOrAbsolute, out var uri))
            throw new FormatException("Invalid HTTP url in header line");
        request.Uri = uri;
        line = line.Slice(index + 1);


        // HTTP/1.1 HTTP/0.9
        index = line.IndexOf("/"u8);
        if (index < 0) throw new FormatException("Invalid HTTP header line");
        if (!Version.TryParse(line.Slice(index + 1).ToUtf8String(), out var version))
            throw new FormatException("Invalid HTTP protocol version");
        request.Protocol = version;

        return index + 1 + 2;
    }

    private static int ParseHeaders(HttpRequest request, Span<byte> span)
    {
        var read = 0;

        while (true)
        {
            // first linebreak
            var nlIndex = span.IndexOf("\r\n"u8);
            if (nlIndex < 0) return read; // no new line provided
            if (nlIndex == 0) // empty line detected, navigating to the start of HTTP body delmitter
            {
                read -= 2;
                return read;
            }

            read += nlIndex + 2;
            var line = span.Slice(0, nlIndex);
            span = span.Slice(line.Length); // removing read occurrence
            var index = line.IndexOf(":"u8);
            if (index < 0) throw new FormatException("Invalid HTTP header");

            request.Headers ??= new();
            var header = span.Slice(0, index).ToUtf8String().Trim();
            var value = line.Slice(index + 1).ToUtf8String().Trim();
            request.Headers.Add(header, value);
            
            if (request.Uri == null) throw new FormatException("Invalid parsing sequence");

            if (string.Equals(header, "Host", StringComparison.InvariantCultureIgnoreCase))
            {
                if (Uri.TryCreate(new Uri(value), request.Uri, out var uri))
                {
                    request.Uri = uri;
                }
            }

            if (string.Equals(header, "Cookies", StringComparison.InvariantCultureIgnoreCase))
            {
                // some hack here
                var container = new CookieContainer();
                container.SetCookies(request.Uri, value);
                request.Cookies = container.GetCookies(request.Uri);
            }
        }
    }
}