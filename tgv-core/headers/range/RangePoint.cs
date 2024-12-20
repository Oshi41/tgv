using System.IO;
using System.Net;
using tgv_core.imp;

namespace tgv_core.headers.range;

/// <summary>
/// Struct representing single point.
/// Can be relative (from end) which is undetermined.
/// That's why it needs to be actualized with knowledge of actual Max length
/// </summary>
public record RangePoint
{
    public RangePoint(SeekOrigin orientation, long x)
    {
        Orientation = orientation;
        X = x;
        
        // wrong value
        if (Orientation == SeekOrigin.Current) 
            throw new HttpException(HttpStatusCode.NotImplemented);
    }

    /// <summary>
    /// Should count offset from start?
    /// </summary>
    public SeekOrigin Orientation { get; }
    
    /// <summary>
    /// Offset
    /// </summary>
    public long X { get; }

    public override string ToString()
    {
        if (Orientation == SeekOrigin.Begin) return X.ToString();
        if (X == 0) return "End";
        return $"End-{X}";
    }
}