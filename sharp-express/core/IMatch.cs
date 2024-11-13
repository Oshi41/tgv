namespace sharp_express.core;

public interface IMatch
{
    /// <summary>
    /// Current entity route
    /// </summary>
    RoutePath Route { get; }
    
    /// <summary>
    /// Handler assigned with this entity
    /// </summary>
    Handle Handler { get; }
}