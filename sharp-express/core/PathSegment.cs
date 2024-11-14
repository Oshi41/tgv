namespace sharp_express.core;

public class PathSegment
{
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