namespace sharp_express.core;

public interface IRouter : IMatch
{
    /// <summary>
    /// Base route path
    /// </summary>
    RoutePath Route { get; }
    IRouter Use(params Handle[] handlers);
    IRouter After(params Handle[] handlers);
    IRouter Use(string path, params Handle[] handlers);
    IRouter After(string path, params Handle[] handlers);
    IRouter Use(IRouter router);
    IRouter Get(string path, params Handle[] handlers);
    IRouter Post(string path, params Handle[] handlers);
    IRouter Delete(string path, params Handle[] handlers);
    IRouter Patch(string path, params Handle[] handlers);
    IRouter Put(string path, params Handle[] handlers);
    IRouter Head(string path, params Handle[] handlers);
    IRouter Error(string path, params Handle[] handlers);
}