using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using tgv_core.api;
using tgv_core.extensions;

namespace tgv_core.imp;

/// <summary>
/// Represents a path configuration for HTTP routing, encapsulating the details of the HTTP method, path segments,
/// handler, and any routing configurations. It is capable of matching a given context against its path and extracting
/// parameter values from the path.
/// <inheritdoc cref="IMatch"/>
/// </summary>
public class RoutePath : IMatch
{
    /// <summary>
    /// Represents a route path used to match and handle HTTP requests within a routing framework.
    /// </summary>
    /// <param name="method">The HTTP method for the route.</param>
    /// <param name="path">The path pattern to match requests against.</param>
    /// <param name="handler">The handler to process matched requests.</param>
    /// <param name="config">Configuration settings for the router.</param>
    /// <param name="isRouterPath">Indicates whether the path is a router path.</param>
    public RoutePath(HttpMethod method, string path, HttpHandler handler, RouterConfig config,
        bool isRouterPath = false)
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

    /// <summary>
    /// Gets the HTTP method associated with this route path configuration. The HTTP method indicates the
    /// type of operation (e.g., GET, POST, PUT) or special stage (BEFORE, AFTER, ERROR) that this route is configured to handle.
    /// </summary>
    public HttpMethod Method { get; }

    /// <summary>
    /// Gets the path pattern that is used to match HTTP requests in the routing framework. This property holds
    /// the string representation of the path as specified when the route was configured.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Indicates whether the current route path is utilized as a router path. This property affects the matching
    /// behavior of the route path against a given context, in particular determining whether the regex pattern for
    /// the route path should match only the start of a path or if it requires a complete match.
    /// </summary>
    public bool IsRouterPath { get; }

    public HttpHandler Handler { get; }
    public RouterConfig Config { get; }

    /// <summary>
    /// Gets a list of path segments parsed from the route's raw path string.
    /// These segments are derived by splitting the path string on slash and backslash characters,
    /// trimming whitespace, and filtering out any empty segments. Each non-empty segment
    /// is encapsulated within a <see cref="PathSegment"/> object to facilitate further processing
    /// within the routing logic, such as pattern matching and parameter extraction.
    /// </summary>
    public List<PathSegment> Segments { get; }

    /// <summary>
    /// Constructs a regular expression string to match against HTTP request paths
    /// based on the provided list of path segments and routing context.
    /// </summary>
    /// <param name="segments">The list of path segments that form part of the route path.</param>
    /// <param name="ctx">The context of the current HTTP request, including its path and configuration.</param>
    /// <returns>A string representing a regular expression to match the route path against HTTP request paths.</returns>
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

        // should ignore trailing slash
        if (!Config.IgnoreTrailingSlashes && regex.EndsWith("/"))
            regex = regex.Substring(0, regex.Length - 1);

        // set trailing slash explicitly if needed
        if (Config.IgnoreTrailingSlashes && ctx.Url.OriginalString.EndsWith("/") && !regex.EndsWith("/"))
            regex += '/';

         // router must match onle the start
        if (!IsRouterPath)
            regex += "$";

        return regex;
    }

    /// <summary>
    /// Determines if the current route path matches the provided context's URL and stage.
    /// </summary>
    /// <param name="ctx">The context containing the URL and additional request information to match against.</param>
    /// <returns>True if the context matches the route path; otherwise, false.</returns>
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

    /// <summary>
    /// Extracts and returns a dictionary of parameters from the current HTTP request context, based on the defined route path.
    /// </summary>
    /// <param name="ctx">The current context of the HTTP request containing URL and query information.</param>
    /// <param name="ignoreLastSegment">A flag indicating whether to ignore the last segment of the path when extracting parameters.</param>
    /// <returns>A dictionary containing parameter names and their corresponding values extracted from the URL and query.</returns>
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