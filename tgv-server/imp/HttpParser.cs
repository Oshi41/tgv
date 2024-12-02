using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO.Pipelines;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using tgv_core.imp;
using tgv_server.api;

namespace tgv_server.imp;

public class HttpParser : IHttpParser<TgvContext>
{
    private static readonly Encoding _encoding = Encoding.UTF8;
    
    private static readonly HashSet<string> ValidMethods =
        ["GET", "HEAD", "OPTIONS", "TRACE", "PUT", "DELETE", "POST", "PATCH", "CONNECT"];
    
    private static readonly Dictionary<byte[], Version> HttpVersions = new()
    {
        [_encoding.GetBytes("HTTP/1.0")] = new Version(1, 0),
        [_encoding.GetBytes("HTTP/1.1")] = new Version(1, 1),
        [_encoding.GetBytes("HTTP/2.0")] = new Version(2, 0),
        [_encoding.GetBytes("HTTP/3.0")] = new Version(3, 0),
    };

    private const byte Cr = (byte)'\r';
    private const byte Lf = (byte)'\n';
    private const byte Colon = (byte)':';
    private const byte Space = (byte)' ';
    private const byte Question = (byte)'?';
    private const byte And = (byte)'&';
    private const byte Equals = (byte)'=';

    public async Task<TgvContext> Parse(Pipe socket)
    {
        var read = await socket.Reader.ReadAsync();
        var parsed = Parse(read.Buffer);
        return new TgvContext(parsed.Method, new Guid(), parsed.Url, new Logger(), parsed.Headers, parsed.Url.Query());
    }

    private HttpParsed Parse(ReadOnlySequence<byte> buffer)
    {
        HttpMethod method;
        Version version;
        Uri uri;
        NameValueCollection headers = new();
        byte[] body = [];

        SequencePosition? pos;

        // parsing method
        pos = buffer.PositionOf(Space);
        if (pos == null) throw new FormatException("Wrong HTTP format");
        method = new HttpMethod(Encoding.UTF8.GetString(buffer.Slice(0, pos.Value).ToArray()).ToUpper());
        if (!ValidMethods.Contains(method.Method)) throw new FormatException($"Unknown HTTP method: {method.Method}");

        // parsing URL
        buffer = buffer.Slice(pos.Value.GetInteger() + 1);
        pos = buffer.PositionOf(Space);
        if (pos == null) throw new FormatException("Wrong HTTP format");
        if (!Uri.TryCreate(Encoding.UTF8.GetString(buffer.Slice(0, pos.Value).ToArray()), UriKind.RelativeOrAbsolute,
                out uri)) throw new FormatException("Wrong HTTP format");
        
        // parsing version
        buffer = buffer.Slice(pos.Value.GetInteger() + 1);
        pos = buffer.PositionOf(Cr) ?? buffer.PositionOf(Lf);
        if (pos == null) throw new FormatException("Wrong HTTP format");
        if (!HttpVersions.TryGetValue(buffer.Slice(pos.Value).ToArray(), out version)) 
            throw new FormatException("Wrong HTTP format");
        
        return new HttpParsed(method, uri, version, headers, body);
        
    }
}