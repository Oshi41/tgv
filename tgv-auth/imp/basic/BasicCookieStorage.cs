using System.Diagnostics.Metrics;
using System.Net;
using tgv_auth.api.storage;
using tgv_core.api;
using tgv_core.extensions;

namespace tgv_auth.imp.basic;

public class BasicCookieStorage : ICookieStorage<BasicSession>
{
    private readonly string _cookieName;

    public BasicCookieStorage(string cookieName)
    {
        _cookieName = cookieName;
    }

    public BasicSession? GetUserSession(Context ctx)
    {
        var cookie = ctx.Cookies[_cookieName];
        if (cookie == null) return null;
        
        if (!cookie.IsValid())
        {
            Statics.Metrics.CreateCounter<int>("basic_cookie_storage", description: "Basic cookie expired or invalid")
                .Add(1, ctx.ToTagsFull().With("session_cookie", cookie));
            return null;
        }

        var when = cookie.Expires;
        var start = cookie.TimeStamp;
        var name = cookie.Value;
        return new BasicSession(start, when, name);
    }

    public Cookie CreateCookie(Context ctx, BasicSession userSession)
    {
        return new Cookie(_cookieName, userSession.Name)
        {
            Expires = userSession.Expired,
        };
    }
}