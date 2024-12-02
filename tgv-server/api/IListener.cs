using System;
using System.Threading;
using System.Threading.Tasks;
using tgv_core.api;

namespace tgv_server.api;

public interface IListener<out T>
    where T : Context
{
    bool IsRunning { get; }
    void Run(ServerHandler handler, CancellationToken token);
    void Stop();
}