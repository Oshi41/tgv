using tgv_core.api;
using tgv_core.imp;
using tgv_server_kestrel;
using tgv_server;

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
        new("tgv--kestrel-server", x => new KestrelServer(new KestrelSettings(), x, new Logger()))
    ];
}