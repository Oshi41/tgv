using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using tgv_core.api;
using tgv_core.extensions;

namespace tgv_core.imp;

public class RoutePath : IMatch
{
    public RoutePath(HttpMethod method, string path, HttpHandler handler, RouterConfig config, bool isRouterPath = false)
    {
        Method = method;
        Path = path;
        Handler = handler;
        Config = config;
        IsRouterPath = isRouterPath;

        Segments = path.Split('/', '\\')
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Select(x => new PathSegment(x))
            .ToList();
    }

    public RoutePath Route => this;
    public HttpMethod Method { get; }
    public string Path { get; }
    public bool IsRouterPath { get; }

    public HttpHandler Handler { get; }
    public RouterConfig Config { get; }
    public List<PathSegment> Segments { get; }

    private string ConstructRegex(List<PathSegment> segments, Context ctx)
    {
        var allSegments = new List<PathSegment>();

        allSegments.AddRange(ctx.CurrentPath
            .Reverse()
            .Select(x => x.Route)
            .SkipWhile(x => x.Segments.All(s => s.IsWildcard))
            .SelectMany(x => x.Segments));
        allSegments.AddRange(segments);

        if (allSegments.All(x => x.IsWildcard))
            return ".+";

        var regex = $"^/{string.Join("/", allSegments.Select(x => x.Regex))}";
        // router must match onle the start
        if (!IsRouterPath)
            regex += "$";

        // should ignore trailing slash
        if (!Config.IgnoreTrailingSlashes && regex.EndsWith("/"))
            regex = regex.Substring(0, regex.Length - 1);

        // set trailing slash explicitly if needed
        if (Config.IgnoreTrailingSlashes && ctx.Url.OriginalString.EndsWith("/") && !regex.EndsWith("/"))
            regex += '/';

        return regex;
    }

    public bool Match(Context ctx)
    {
        // router should work anyway
        if (!IsRouterPath && ctx.Stage != Method) return false;

        // full match
        if (new Regex(ConstructRegex(Segments, ctx)).IsMatch(ctx.Url.AbsolutePath)) return true;

        // last path segment can be skipped
        if (Segments.Any() && Segments.Last() is { IsWildcard: true } or { IsPattern: true })
        {
            var regex = ConstructRegex(Segments.SkipLast(1).ToList(), ctx);
            if (new Regex(regex, RegexOptions.IgnoreCase).IsMatch(ctx.Url.AbsolutePath))
            {
                var parameterName = Segments.Last().Name!;
                var parameters = Parameters(ctx, true);
                if (parameters.ContainsKey(parameterName))
                {
                    // Parse parameters only once
                    ctx.Parameters = parameters;
                    return true;
                }
            }
        }

        return false;
    }

    public IDictionary<string, string> Parameters(Context ctx, bool ignoreLastSegment = false)
    {
        var regex = ConstructRegex(Segments.SkipLast(ignoreLastSegment ? 1 : 0).ToList(), ctx)
            .Replace("[^/]+?", "([^/]+?)");

        var parameters = Segments.Where(x => x.IsPattern).ToList();
        var groups = Regex.Match(ctx.Url.AbsolutePath, regex)
            .Groups
            .OfType<Group>()
            .Where(x => x.GetType() == typeof(Group))
            .ToList();
        var dict = new Dictionary<string, string>();
        for (var i = 0; i < parameters.Count; i++)
        {
            dict[parameters[i].Name!] = groups.ElementAtOrDefault(i)?.Value
                                        ?? ctx.Query[parameters[i].Name!]
                                        ?? string.Empty;
        }

        return dict;
    }
}