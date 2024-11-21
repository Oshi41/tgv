using tgv.imp;

namespace tgv.servers;

public interface IServer
{
    bool IsListening { get; }
    bool IsHttps { get; }
    int Port { get; }
    Logger Logger { get; }
    Task StartAsync(int port);
    void Stop();
}