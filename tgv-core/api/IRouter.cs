namespace tgv_core.api;

public interface IRouter : IMatch
{
    IRouter Use(params HttpHandler[] handlers);
    IRouter After(params HttpHandler[] handlers);
    IRouter Use(string path, params HttpHandler[] handlers);
    IRouter After(string path, params HttpHandler[] handlers);
    IRouter Use(IRouter router);
    IRouter Get(string path, params HttpHandler[] handlers);
    IRouter Post(string path, params HttpHandler[] handlers);
    IRouter Delete(string path, params HttpHandler[] handlers);
    IRouter Patch(string path, params HttpHandler[] handlers);
    IRouter Put(string path, params HttpHandler[] handlers);
    IRouter Head(string path, params HttpHandler[] handlers);
    IRouter Error(string path, params HttpHandler[] handlers);
    IRouter Options(string path, params HttpHandler[] handlers);
    IRouter Connect(string path, params HttpHandler[] handlers);
    IRouter Trace(string path, params HttpHandler[] handlers);
}