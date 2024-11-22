using Microsoft.Extensions.Logging;
using tgv_core.imp;

namespace tgv_kestrel_server.imp;

public class LogWrapper : ILogger
{
    private readonly Logger _inner;

    public LogWrapper(Logger inner)
    {
        _inner = inner;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, 
        Func<TState, Exception, string> formatter)
    {
        var text = formatter(state, exception);

        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.None:
                _inner.Debug(text);
                return;
            
            case LogLevel.Information:
                _inner.Info(text);
                return;
            
            case LogLevel.Warning:
                _inner.Warn(text);
                return;
            
            case LogLevel.Error:
                _inner.Error(text);
                return;
            
            case LogLevel.Critical:
                _inner.Fatal(text);
                return;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state)
    {
        throw new NotImplementedException();
    }
}