namespace tgv_core.imp;

/// <summary>
/// Represents a segment of a path in a routing context.
/// </summary>
public class PathSegment
{
    /// <summary>
    /// Represents a segment of a path in a routing context.
    /// This class is used to encapsulate individual components of a route path
    /// and determine the type of matching required for each segment.
    /// </summary>
    public PathSegment(string pattern)
    {
        IsWildcard = pattern == "*";
        IsPattern = pattern.StartsWith(":");
        Regex = IsWildcard ? ".*?" // accept anything
            : IsPattern ? "[^/]+?" // Accepting everything except slash
            : pattern;

        Name = IsPattern
            ? pattern.Substring(1)
            : null;
    }

    /// <summary>
    /// Gets the regex pattern corresponding to this path segment.
    /// If the segment is a wildcard, the regex will match any sequence of characters.
    /// If the segment is a pattern, the regex will match any sequence of characters except the forward slash.
    /// Otherwise, regex string.
    /// </summary>
    public string Regex { get; }

    /// <summary>
    /// Pattern Name. Not null only for <see cref="IsPattern"/> == true
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Is this segment is pattern
    /// </summary>
    public bool IsPattern { get; }
    
    /// <summary>
    /// If Pattern == "*" 
    /// </summary>
    public bool IsWildcard { get; }
}