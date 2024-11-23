using tgv_core.api;

namespace tgv_tests;

public static class Servers
{
    public class ServerCreationCase(string name, Func<ServerHandler, IServer> fn)
    {
        private readonly Func<ServerHandler, IServer> _fn = fn;
        
        public static implicit operator Func<ServerHandler, IServer>(ServerCreationCase s) => s._fn;
        public override string ToString() => name;
    }
    
    public static ServerCreationCase[] AllServers =
    [
        // new("watson", x => new tgv_watson_server.WatsonServer(x)),
        new("kestrel", x => new tgv_kestrel_server.KestrelServer(x)),
    ];
}