using tgv_common.imp;

namespace tgv_common.api;

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