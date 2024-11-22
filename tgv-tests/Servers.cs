using tgv_core.api;

namespace tgv_tests;

public static class Servers
{
    public static Func<ServerHandler, IServer>[] AllServers =
    [
        // h => new tgv_watson_server.WatsonServer(h),
        h => new tgv_kestrel_server.KestrelServer(h),
    ];
}