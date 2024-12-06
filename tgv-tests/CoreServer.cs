using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace tgv_tests;

class Session : HttpSession
{
    public Session(HttpServer server) : base(server)
    {
    }

    protected override void OnReceivedRequest(HttpRequest request)
    {
        Response.SetBegin(200);
        Response.SetBody("Hello world!");
        SendResponseAsync();
    }
    
    protected override void OnReceivedRequestError(HttpRequest request, string error)
    {
        Console.WriteLine($"Request error: {error}");
    }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"HTTP session caught an error: {error}");
    }
}

public class CoreServer : HttpServer
{
    public CoreServer(IPEndPoint endpoint) : base(endpoint)
    {
    }

    protected override TcpSession CreateSession()
    {
        return new Session(this);
    }
    
    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"HTTP session caught an error: {error}");
    }
}