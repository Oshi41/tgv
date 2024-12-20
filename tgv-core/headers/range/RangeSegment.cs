using System;
using System.IO;
using System.Net;
using tgv_core.imp;

namespace tgv_core.headers.range;

/// <summary>
/// Array range. Represents array slice pointers.
/// Can be relative (from end) which is undetermined.
/// That's why it needs to be actualized with knowledge of actual Max length
/// </summary>
public record RangeSegment
{
    public RangePoint Start { get; }
    public RangePoint End { get; }
    public long Count { get; }

    /// <summary>
    /// Parse range from string
    /// </summary>
    /// <param name="raw">range string</param>
    /// <param name="maxIndex">Known maximum</param>
    /// <exception cref="HttpException"></exception>
    public RangeSegment(string raw, long maxIndex)
    {
        var arr = raw.Split(['-']);
        var startParsed = long.TryParse(arr[0], out var x);
        var endParsed = long.TryParse(arr[1], out var y);

        x = Math.Min(maxIndex, x);
        y = Math.Min(maxIndex, y);

        // normal range
        if (startParsed && endParsed)
        {
            Start = new RangePoint(SeekOrigin.Begin, x);
            End = new RangePoint(SeekOrigin.Begin, y);
            // adding 1 as indexes starting from zero
            Count = y - x + 1;
        }
        // from the end and backwards
        else if (!startParsed && endParsed)
        {
            Start = new RangePoint(SeekOrigin.End, y);
            End = new RangePoint(SeekOrigin.End, 0);
            Count = y;
        }
        else
        {
            throw new HttpException(HttpStatusCode.BadRequest, "Range header is not valid");
        }

        if (Count <= 0)
            throw new HttpException(HttpStatusCode.BadRequest, "Range header is not valid");
    }

    public override string ToString()
    {
        return $"{Start}-{End}";
    }
}