using tgv_core.imp;

namespace tgv_core.api;

/// <summary>
/// Represents a contract for matching route paths within an application routing system.
/// </summary>
public interface IMatch
{
    /// <summary>
    /// Gets the route path associated with this entity, providing access to routing path information and evaluation.
    /// </summary>
    RoutePath Route { get; }

    /// <summary>
    /// Gets the HTTP request handler associated with the match.
    /// This handler processes the request and allows for continuation
    /// of the request pipeline or handling exceptions.
    /// </summary>
    HttpHandler Handler { get; }
}