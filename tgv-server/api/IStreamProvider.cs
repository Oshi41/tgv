using System.IO;

namespace tgv_server.api;

public interface IStreamProvider
{
    /// <summary>
    /// Returns current socket stream
    /// </summary>
    /// <returns></returns>
    Stream? GetStream();
}