using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using tgv_core.api;

namespace tgv_core.extensions;

public static class MetricExtensions
{
    public static TagList ToTagsLight(this Context ctx, Exception? e = null)
    {
        var list = new List<KeyValuePair<string, object?>>
        {
            new("traceID", ctx.TraceId),
        };

        if (e != null)
            list.Add(new KeyValuePair<string, object?>("error", e));
        
        return new TagList(list.ToArray());
    }
    
    public static TagList ToTagsFull(this Context ctx, Exception? e = null)
    {
        var tag = ctx.ToTagsLight(e);
        
        tag.Add(new("url", ctx.Url));
        tag.Add(new("method", ctx.Method));
        tag.Add(new("headers", ctx.ClientHeaders));
        
        return tag;
    }

    public static TagList With(this TagList tag, string key, object? value)
    {
        tag.Add(new(key, value));
        return tag;
    }
    
    public static string GetMetricName(this Context ctx) => $"{ctx.Method}.{ctx.Url.AbsolutePath}";
}