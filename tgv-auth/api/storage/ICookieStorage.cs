using System.Net;
using tgv_core.api;

namespace tgv_auth.api.storage;

public interface ICookieStorage<T>
    where T : IUserSession
{
    /// <summary>
    /// Gets current session from current HTTP request
    /// </summary>
    /// <param name="ctx">HTTP request context</param>
    T? GetUserSession(Context ctx);

    /// <summary>
    /// Creates cookie from current user session
    /// </summary>
    /// <param name="ctx">HTTP request context</param>
    /// <param name="userSession">User session</param>
    Cookie CreateCookie(Context ctx, T userSession);
}