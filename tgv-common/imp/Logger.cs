using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace tgv_common.imp;

public delegate string LogAction(EventLevel level, string message, string? caller, string? member, int? line);

public class Logger
{
    private static string? AssemblyRoot => Assembly.GetExecutingAssembly()?.FullName?.Split(',')?.ElementAtOrDefault(0);
    
    private LogAction _messageMutator = (_, message, _, _, _) => message;
    public Action<string>? WriteLog { get; set; } = Console.WriteLine;

    public Logger WithCustomMessage(LogAction msgMutator)
    {
        var result = new Logger
        {
            WriteLog = WriteLog,
            _messageMutator = (level, message, caller, member, line)
                => msgMutator(level, this._messageMutator(level, message, caller, member, line), caller, member, line)
        };

        return result;
    }

    private void LogInner(EventLevel level, string message, string? caller, string? member, int? line)
    {
        var lvl = level switch
        {
            EventLevel.Error => level.ToString().Substring(3),
            EventLevel.Verbose or EventLevel.LogAlways => "debug",
            EventLevel.Informational or EventLevel.Warning or EventLevel.Critical => level.ToString().Substring(4),
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };

        if (!string.IsNullOrEmpty(AssemblyRoot) && !string.IsNullOrEmpty(caller))
        {
            var i = caller.IndexOf(AssemblyRoot);
            if (i >= 0)
                caller = caller.Substring(i + AssemblyRoot.Length + 1);
        }

        WriteLog?.Invoke($"[{lvl.ToUpper()}] [{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] "
                         + $"[{caller}:{line}] {_messageMutator?.Invoke(level, message, caller, member, line) ?? message}");
    }

    public void Log(EventLevel level, string message, [CallerFilePath] string? caller = null,
        [CallerMemberName] string? member = null,
        [CallerLineNumber] int? line = null) => LogInner(level, message, caller, member, line);

    public void Debug(string message, [CallerFilePath] string? caller = null,
        [CallerMemberName] string? member = null,
        [CallerLineNumber] int? line = null) => LogInner(EventLevel.Verbose, message, caller, member, line);

    public void Info(string message, [CallerFilePath] string? caller = null,
        [CallerMemberName] string? member = null,
        [CallerLineNumber] int? line = null) => LogInner(EventLevel.Informational, message, caller, member, line);

    public void Warn(string message, [CallerFilePath] string? caller = null,
        [CallerMemberName] string? member = null,
        [CallerLineNumber] int? line = null) => LogInner(EventLevel.Warning, message, caller, member, line);

    public void Error(string message, [CallerFilePath] string? caller = null,
        [CallerMemberName] string? member = null,
        [CallerLineNumber] int? line = null) => LogInner(EventLevel.Error, message, caller, member, line);

    public void Fatal(string message, [CallerFilePath] string? caller = null,
        [CallerMemberName] string? member = null,
        [CallerLineNumber] int? line = null) => LogInner(EventLevel.Critical, message, caller, member, line);
}