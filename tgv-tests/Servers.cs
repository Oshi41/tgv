using tgv_common.api;
using tgv_common.imp;
using tgv_kestrel_server;
using tgv_watson_server;

namespace tgv_tests;

public static class Servers
{
    public static Func<ServerHandler, IServer>[] AllServers =
    [
        h => new WatsonServer(h),
        // h => new KestrelServer(h),
    ];
}