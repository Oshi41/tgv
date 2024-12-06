using System.Threading.Tasks;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_server;

public class TgvServer : IServer
{
    public TgvServer(ServerHandler handler, Logger logger)
        : base(handler, logger)
    {
        
    }

    public override bool IsListening { get; }
    public override bool IsHttps { get; }
    public override int Port { get; }
    public override Task StartAsync(int port)
    {
        throw new System.NotImplementedException();
    }

    public override void Stop()
    {
        throw new System.NotImplementedException();
    }
}