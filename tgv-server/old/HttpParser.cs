using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using tgv_core.imp;

namespace tgv_server;

public record Parsed(HttpMethod Method, Uri Url, Version Protocol, NameValueCollection Headers, string Body);

public static class HttpParser
{
    private static readonly HashSet<string> ValidMethods =
        ["GET", "HEAD", "OPTIONS", "TRACE", "PUT", "DELETE", "POST", "PATCH", "CONNECT"];

    private static readonly Dictionary<string, Version> HttpVersions = new()
    {
        ["HTTP/1.0"] = HttpVersion.Version10,
        ["HTTP/1.1"] = HttpVersion.Version11,
        ["HTTP/2.0"] = new Version(2, 0),
        ["HTTP/3.0"] = new Version(3, 0),
    };

    public static async Task<Parsed> Parse(NetworkStream stream, int chunkSize)
    {
        using var ms = new MemoryStream();
        var chunk = ArrayPool<byte>.Shared.Rent(chunkSize);
        using var _ = Disposable.Create(() => ArrayPool<byte>.Shared.Return(chunk, true));
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(chunk, 0, chunk.Length)) > 0)
        {
            await ms.WriteAsync(chunk, 0, bytesRead);
            var search = new ReadOnlySpan<byte>(chunk, 0, bytesRead);
            if (search.IndexOf("\r\n\r\n"u8) >= 0 || search.IndexOf("\n\n"u8) >= 0) break;
        }
        
        return await Parse(Encoding.UTF8.GetString(ms.ToArray()));
    }

    private static async Task<Parsed> Parse(string raw)
    {
        string line = null;
        string[] arr = null;
        int i = 0;
        int index = 0;
        byte[] buffer;

        var lines = raw.Trim().Split('\n').Select(l => l.Trim()).ToArray();

        arr = lines[0].Split(' ');
        if (arr.Length != 3) throw new FormatException("HTTP header is not valid: " + lines[0]);
        arr[0] = arr[0].ToUpperInvariant();
        if (!ValidMethods.Contains(arr[0])) throw new FormatException("HTTP method is not valid: " + lines[0]);
        if (!Uri.TryCreate(arr[1], UriKind.RelativeOrAbsolute, out var uri))
            throw new FormatException("URI is not valid: " + arr[1]);
        if (!HttpVersions.TryGetValue(arr[2].Trim(), out var protocol))
            throw new FormatException("HTTP version is not valid: " + arr[2]);

        var headers = new WebHeaderCollection();
        var method = arr[0];

        for (i = 1; i < lines.Length; i++)
        {
            line = lines[i];
            if (string.IsNullOrEmpty(line))
            {
                i++;
                break; // end of the headers
            }

            index = line.IndexOf(':');
            if (index < 0) throw new FormatException("HTTP header is not valid: " + line);
            arr = new[]
            {
                line.Substring(0, index).Trim(),
                line.Substring(index + 1).Trim()
            };
            headers.Add(arr[0], arr[1]);
        }

        var body = string.Join("\n", lines.Skip(i));

        if (!uri.IsAbsoluteUri && headers[HttpRequestHeader.Host] != null)
        {
            if (Uri.TryCreate(new Uri(headers[HttpRequestHeader.Host]), uri.OriginalString, out var _uri))
            {
                uri = _uri;
            }
        }

        return new Parsed(new HttpMethod(method), uri, protocol, headers, body);
    }
}