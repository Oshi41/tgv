using tgv_core.imp;

namespace tgv_core.api;

public interface IMatch
{
    /// <summary>
    /// Current entity route
    /// </summary>
    RoutePath Route { get; }
    
    /// <summary>
    /// Handler assigned with this entity
    /// </summary>
    HttpHandler Handler { get; }
}