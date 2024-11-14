using tgv.core;
using tgv.imp;
using Handle = tgv.core.Handle;

namespace tgv;

public class App : IRouter
{
    private readonly RouterConfig? _routerConfig;
    private readonly AppConfig _config;
    private IRouter _root;
    private HttpServer _server;

    public App(AppConfig? config = null, RouterConfig? routerConfig = null)
    {
        _config = config ?? new AppConfig();
        _routerConfig = routerConfig ?? new RouterConfig();
        _root = new Router("*", _routerConfig);
    }

    public void Start(int port = 7000)
    {
        Stop();
        _server = new HttpServer(_root, _config);
        _server.Start(port);
        
        Started?.Invoke(this, _server);
    }

    public bool Stop()
    {
        if (_server?.Stop() == true)
        {
            Closed?.Invoke(this, _server);
            return true;
        }

        return false;
    }

    public event EventHandler<HttpServer> Started; 
    public event EventHandler<HttpServer> Closed; 

    public string? RunningUrl => _server?.Url;

    public RoutePath Route => _root.Route;

    public IRouter Use(params Handle[] handlers)
    {
        _root.Use(handlers);
        return this;
    }

    public IRouter After(params Handle[] handlers)
    {
        _root.After(handlers);
        return this;
    }

    public IRouter Use(string path, params Handle[] handlers)
    {
        _root.Use(path, handlers);
        return this;
    }

    public IRouter After(string path, params Handle[] handlers)
    {
        _root.After(path, handlers);
        return this;
    }

    public IRouter Use(IRouter router)
    {
        _root.Use(router);
        return this;
    }

    public IRouter Get(string path, params Handle[] handlers)
    {
        _root.Get(path, handlers);
        return this;
    }

    public IRouter Post(string path, params Handle[] handlers)
    {
        _root.Post(path, handlers);
        return this;
    }

    public IRouter Delete(string path, params Handle[] handlers)
    {
        _root.Delete(path, handlers);
        return this;
    }

    public IRouter Patch(string path, params Handle[] handlers)
    {
        _root.Patch(path, handlers);
        return this;
    }

    public IRouter Put(string path, params Handle[] handlers)
    {
        _root.Put(path, handlers);
        return this;
    }

    public IRouter Head(string path, params Handle[] handlers)
    {
        _root.Head(path, handlers);
        return this;
    }

    public IRouter Error(string path, params Handle[] handlers)
    {
        _root.Error(path, handlers);
        return this;
    }

    public Handle Handler => _root.Handler;
}