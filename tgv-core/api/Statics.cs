using System.Diagnostics.Metrics;
using NLog;

namespace tgv_core.api;

public static class Statics
{
    private static Meter _meter = new("tgv-app");
    public static Meter GetMetric() => _meter;
    public static void SetupMetrics(Meter m) => _meter = m;
}