using System.Diagnostics.Metrics;
using NLog;

namespace tgv_core.api;

public static class Statics
{
    /// <summary>
    /// Default Statics.Metrics
    /// </summary>
    public static readonly Meter Metrics = new("metrics");
    
    /// <summary>
    /// Default logger
    /// </summary>
    public static readonly Logger Logger = LogManager.GetLogger("main");
}