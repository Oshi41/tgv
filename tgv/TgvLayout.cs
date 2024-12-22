using System.Text;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;

namespace tgv;

[LayoutRenderer("tgv-context")]
public class TgvLayout : LayoutRenderer
{
    protected override void Append(StringBuilder builder, LogEventInfo e)
    {
        // selecting only context props started with '_'
        var ctxProps = e.Properties
            .Where(x => (x.Key as string)?.StartsWith("_") == true)
            .OrderBy(x => x.Key)
            .Select(x => string.Format(e.FormatProvider, "{0}={1}", ((string)x.Key).Substring(1), x.Value))
            .ToList();
        
        if (ctxProps.Any())
            builder.Append($" ({string.Join(", ", ctxProps)}) ");
    }
}