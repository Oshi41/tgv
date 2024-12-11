using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MimeTypes;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_large_files;

/// <summary>
/// Struct representing single point.
/// Can be relative (from end) which is undetermined.
/// That's why it needs to be actualized with knowledge of actual Max length
/// </summary>
record Point
{
    public Point(SeekOrigin orientation, long x)
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

/// <summary>
/// Array range. Represents array slice pointers.
/// Can be relative (from end) which is undetermined.
/// That's why it needs to be actualized with knowledge of actual Max length
/// </summary>
record Range
{
    public Point Start { get; }
    public Point End { get; }
    public long Count { get; }

    /// <summary>
    /// Parse range from string
    /// </summary>
    /// <param name="raw">range string</param>
    /// <param name="maxIndex">Known maximum</param>
    /// <exception cref="HttpException"></exception>
    public Range(string raw, long maxIndex)
    {
        var arr = raw.Split(['-']);
        var startParsed = long.TryParse(arr[0], out var x);
        var endParsed = long.TryParse(arr[1], out var y);

        x = Math.Min(maxIndex, x);
        y = Math.Min(maxIndex, y);

        // normal range
        if (startParsed && endParsed)
        {
            Start = new Point(SeekOrigin.Begin, x);
            End = new Point(SeekOrigin.Begin, y);
            // adding 1 as indexes starting from zero
            Count = y - x + 1;
        }
        // from the end and backwards
        else if (!startParsed && endParsed)
        {
            Start = new Point(SeekOrigin.End, y);
            End = new Point(SeekOrigin.End, 0);
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
    public IReadOnlyList<Range> Ranges { get; }

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
            throw new HttpException( HttpStatusCode.BadRequest, $"Range header does not contains '='");

        Unit = rangeHeader.Substring(0, index);

        var list = new List<Range>();
        foreach (var rangeRaw in rangeHeader.Substring(index + 1).Split(','))
        {
            list.Add(new Range(rangeRaw, maxIndex));
        }

        // preserve the order
        Ranges = list.AsReadOnly();

        if (!Ranges.Any())
            throw new HttpException( HttpStatusCode.BadRequest, $"Range header is missing ranges");

        Count = Ranges.Sum(x => x.Count);
    }

    public override string ToString()
    {
        return $"{Unit}={string.Join(", ", Ranges)}";
    }
}

public static class TgvLargeFile
{
    private const string _rangeUnit = "bytes";

    /// <summary>
    /// Middleware for serving large files via partial HTTP requests
    /// </summary>
    /// <param name="router">Router/application</param>
    /// <param name="path">Server path pattern</param>
    /// <param name="filepath">path to hosting file</param>
    /// <param name="chunkSize">max chunk size - how much can we send per single request</param>
    public static IRouter ServeFile(this IRouter router, string path, string filepath, int chunkSize = 32768)
    {
        var contentType = MimeTypeMap.GetMimeType(Path.GetExtension(filepath));

        router.Head(path, (ctx, next, exception) =>
        {
            var info = new FileInfo(filepath);

            ctx.ResponseHeaders.Add("Last-Modified", info.LastWriteTime.ToString("R"));
            ctx.ResponseHeaders.Add("Content-Type", contentType);
            ctx.ResponseHeaders.Add("Accept-Ranges", _rangeUnit);
            ctx.ResponseHeaders.Add("Content-Length", info.Length.ToString());

            next();
            return Task.CompletedTask;
        });

        router.Get(path, async (ctx, next, exception) =>
        {
            var header = ctx.ClientHeaders["Range"]?.Trim();
            if (string.IsNullOrEmpty(header))
                throw ctx.Throw(HttpStatusCode.BadRequest, "Range header is missing.");

            var info = new FileInfo(filepath);
            var range = new RangeHeaderValue(header, info.Length);
            if (range.Unit != _rangeUnit)
            {
                ctx.Logger.Warn($"Invalid Range header: '{header}'");
                throw ctx.Throw(HttpStatusCode.BadRequest,
                    $"Range header is invalid, only {_rangeUnit} are supported.");
            }

            if (range.Count > chunkSize)
            {
                ctx.Logger.Warn($"Too big range: '{header}'");
                throw ctx.Throw(HttpStatusCode.BadRequest, $"Requested range bigger than {chunkSize} {_rangeUnit}.");
            }

            using var ms = new MemoryStream();
            using var msw = new StreamWriter(ms);
            using var fs = File.OpenRead(info.FullName);
            
            ctx.ResponseHeaders.Add("Date", DateTime.Now.ToString("R"));
            ctx.ResponseHeaders.Add("Last-Modified", info.LastWriteTime.ToString("R"));
            ctx.ResponseHeaders.Add("Content-Length", range.Count.ToString());

            // flushing provided range to the body
            async Task WriteBody(Range r)
            {
                // setting the position
                fs.Seek(r.Start.X, r.Start.Orientation);
                // fixing max bytes count to read
                var max = Math.Min(fs.Length - 1 - fs.Position, r.Count);
                // allocating array of needed size
                var buff = new byte[max];
                var read = await fs.ReadAsync(buff, 0, (int)max);
                if (read == 0)
                    ctx.Logger.Warn($"File was no read at this range: '{header}'");
                else
                    await ms.WriteAsync(buff, 0, read);
            }

            if (range.Ranges.Count == 1)
            {
                var r = range.Ranges[0];
                ctx.ResponseHeaders.Add("Content-Range", $"{r.Start}-{r.End}/{info.Length}");
                ctx.ResponseHeaders.Add("Content-Type", contentType);
                await WriteBody(r);
            }
            else
            {
                var separator = "SEPARATOR";
                ctx.ResponseHeaders.Add("Content-Type", $"multipart/byteranges; boundary={separator}");
                
                foreach (var r in range.Ranges)
                {
                    await msw.WriteAsync($"--{separator}\r\n" +
                                         $"Content-Type: {contentType}\r\n" +
                                         $"Content-Range: {r.Start}-{r.End}/{info.Length}\r\n");
                    await WriteBody(r);
                }
            }

            await ctx.SendRaw(ms.ToArray(), HttpStatusCode.PartialContent, null);
        });

        return router;
    }
}