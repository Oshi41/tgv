using Microsoft.Extensions.Logging;
using tgv_core.imp;

namespace tgv_kestrel_server.imp;

public class LoggerFactory : ILoggerFactory
{
    private readonly Logger _source;

    public LoggerFactory(Logger source)
    {
        _source = source;
    }
    
    public void Dispose()
    {
        
    }

    public ILogger CreateLogger(string categoryName)
    {
        var src = _source.WithCustomMessage((_, message, _, _, _) => $"{categoryName} {message}");
        return new LogWrapper(src);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotImplementedException();
    }
}