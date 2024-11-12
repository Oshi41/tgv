namespace sharp_express.core;

public interface IRouter
{
    Router Use(params Handle[] handlers);
    Router Use(string path, params Handle[] handlers);
    Router Get(string path, params Handle[] handlers);
    Router Post(string path, params Handle[] handlers);
    Router Delete(string path, params Handle[] handlers);
    Router Patch(string path, params Handle[] handlers);
    Router Put(string path, params Handle[] handlers);
    Router Head(string path, params Handle[] handlers);
}