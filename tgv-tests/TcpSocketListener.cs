using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace tgv_tests;

public record ParsedHttp(HttpMethod Method, Uri Uri, Version Protocol, NameValueCollection Headers, byte[] Body);
public record HttpResponse(HttpMethod Method, HttpStatusCode Code, Uri Uri, Version Protocol, NameValueCollection Headers, CookieContainer Cookies, byte[]? Body);

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

    public static ParsedHttp? Parse(byte[]? bytes)
    {
        HttpMethod method;
        Uri url = null;
        Version protocol = default;
        NameValueCollection headers = new();
        byte[] body;
        int i = 0;
        int index = -1;
        string line;

        if (bytes == null) return default;
        var str = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        var lines = str.Split("\n");

        line = lines[0];
        var arr = line.Split(' ');
        Debug.Assert(arr.Length == 3);

        method = new HttpMethod(arr[0]);
        Debug.Assert(ValidMethods.Contains(arr[0]));
        Debug.Assert(Uri.TryCreate(arr[1], UriKind.RelativeOrAbsolute, out url));
        Debug.Assert(HttpVersions.TryGetValue(arr[2].Trim(), out protocol));

        for (i = 1; i < lines.Length; i++)
        {
            line = lines[i].Trim();
            // found new line
            if (string.IsNullOrEmpty(line))
            {
                i++;
                break;
            }

            index = line.IndexOf(':');
            Debug.Assert(index != -1);
            headers.Add(line.Substring(0, index).Trim(), line.Substring(index + 1).Trim());
        }
        
        body = Encoding.UTF8.GetBytes(string.Join("\n", lines.Skip(i)));
        return new ParsedHttp(method, url, protocol, headers, body);
    }

    public static string CreateHttpMessage(HttpResponse resp)
    {
        if (resp.Cookies?.Count > 0)
            resp.Headers.Add("Cookie", resp.Cookies.GetCookieHeader(resp.Uri));
        
        var sb = new StringBuilder();
        sb.AppendLine($"HTTP/{resp.Protocol} {(int)resp.Code} {resp.Code.ToString()}");
        
        if (resp.Headers.Count > 0)
        {
            foreach (var key in resp.Headers.AllKeys)
            {
                sb.AppendLine($"{key}: {resp.Headers[key]}");
            }
            sb.AppendLine();
        }
        
        if (resp.Body != null)
        {
            sb.Append(Encoding.UTF8.GetString(resp.Body));
            sb.AppendLine();
        }

        sb.AppendLine();

        return sb.ToString();
    }
}

public class TcpSocketListener
{
    private int _port;
    private Socket? _listener;

    public void Start(int port)
    {
        Stop();

        var endPoint = new IPEndPoint(IPAddress.Any, port);
        _listener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(endPoint);
        _listener.NoDelay = true;
        _listener.Listen(512);
        Task.Run(() => Run(_listener));
    }

    private void Run(Socket socket)
    {
        while (true)
        {
            var args = new SocketAsyncEventArgs();
            var arr = new byte[4096];
            args.SetBuffer(new Memory<byte>(arr, 0, arr.Length));
            args.Completed += HandleConnection;
            socket.AcceptAsync(args);
        }
    }

    private void HandleConnection(object? sender, SocketAsyncEventArgs e)
    {
        var socket = e.AcceptSocket;
        Debug.Assert(socket != null);
        var result = HttpParser.Parse(e.MemoryBuffer.Slice(0, e.BytesTransferred).ToArray());
        Debug.Assert(result != null);

        var headers = new NameValueCollection(result.Headers)
        {
            { result.Method.ToString(), result.Protocol.ToString() },
            { "Content-Type", "text/plain" },
        };

        var response = new HttpResponse(result.Method, HttpStatusCode.OK, result.Uri, result.Protocol,
            headers, new(), "Hello World!"u8.ToArray());
        var httpMessage = HttpParser.CreateHttpMessage(response);
        httpMessage = @"HTTP/1.1 200 OK
Content-Type: text/plain
Content-Length: 12

Hello world!" ;
        var sent = Encoding.UTF8.GetBytes(httpMessage);
        socket.SendAsync(sent);
    }

    public void Stop()
    {
        Console.WriteLine("Server stopping...");
        _listener?.Close();
        _listener?.Dispose();
    }
}