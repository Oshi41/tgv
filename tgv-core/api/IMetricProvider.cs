using System.Diagnostics.Metrics;

namespace tgv_core.api;

public interface IMetricProvider
{
    Meter Metrics { get; set; }
}