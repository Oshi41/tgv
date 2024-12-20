using System.Collections.Generic;
using System.Linq;
using System.Net;
using tgv_core.imp;

namespace tgv_core.headers.range;

/// <summary>
/// Parsed value of 'Range' header
/// </summary>
class RangeHeaderValue
{
    /// <summary>
    /// Range unit
    /// </summary>
    public string Unit { get; }

    /// <summary>
    /// Ranges
    /// </summary>
    public IReadOnlyList<RangeSegment> Ranges { get; }

    /// <summary>
    /// Total ranges bytes
    /// </summary>
    public long Count { get; }

    public RangeHeaderValue(string rangeHeader, long maxIndex)
    {
        rangeHeader = rangeHeader
            .Replace(" ", string.Empty)
            .ToLowerInvariant();

        var index = rangeHeader.IndexOf('=');
        if (index < 0)
            throw new HttpException(HttpStatusCode.BadRequest, $"Range header does not contains '='");

        Unit = rangeHeader.Substring(0, index);

        var list = new List<RangeSegment>();
        foreach (var rangeRaw in rangeHeader.Substring(index + 1).Split(','))
        {
            list.Add(new RangeSegment(rangeRaw, maxIndex));
        }

        // preserve the order
        Ranges = list.AsReadOnly();

        if (!Ranges.Any())
            throw new HttpException(HttpStatusCode.BadRequest, $"Range header is missing ranges");

        Count = Ranges.Sum(x => x.Count);
    }

    public override string ToString()
    {
        return $"{Unit}={string.Join(", ", Ranges)}";
    }
}