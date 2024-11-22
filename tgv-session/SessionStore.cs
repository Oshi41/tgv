using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using tgv_core.api;
using tgv;

namespace tgv_session;

public class SessionStore
{
    private readonly App _app;
    private readonly SessionConfig _config;
    private readonly Task<IStore> _sessions;
    
    public SessionStore(App app, SessionConfig config)
    {
        _app = app;
        _config = config;
        _sessions = config.CreateCache();
    }
    
    /// <summary>
    /// Creating session context attached to regular HHTP context.
    /// </summary>
    /// <param name="ctx">HTTP context</param>
    /// <param name="shouldOpen">Should create new session if expired / not autificated / wrong data / etc.</param>
    /// <returns></returns>
    public async Task<SessionContext?> GetContext(Context ctx, bool shouldOpen)
    {
        var cookie = ctx.Cookies[_config.Cookie];
        var store = await _sessions;

        if (cookie != null)
        {
            if (!cookie.Expired)
            {
                if (Guid.TryParse(cookie.Value, out var guid))
                {
                    var session = await store.FindAsync(guid);
                    if (session != null)
                    {
                        if (!session.IsExpired)
                        {
                            return session;
                        }

                        await store.RemoveAsync(guid);
                        ctx.Logger.Debug($"Session {guid} was expired");
                    }
                    else
                    {
                        ctx.Logger.Debug($"Session {guid} not found");
                    }
                }
                else
                {
                    ctx.Logger.Debug($"Wrong session cookie format: {cookie.Value}");
                }
            }
            else
            {
                ctx.Logger.Debug($"Session cookie was expired: {cookie.Value}");
            }
        }
        else
        {
            ctx.Logger.Debug($"Session cookie was not found");
        }

        if (!shouldOpen) return null;
        
        ctx.Logger.Debug($"Opening new session: {ctx}");
        var result = new SessionContext(await _config.GenerateId(), DateTime.Now + _config.Expire);
        
        cookie = new Cookie(_config.Cookie, result.Id.ToString())
        {
            Expires = result.Expires,
            HttpOnly = true,
        };
        ctx.Cookies.Add(cookie);
        await store.PutAsync(result);
        ctx.Logger.Debug($"Session {result.Id} created");
        return result;
    }

    /// <summary>
    /// Returns currently opened sessions
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<SessionContext>> GetAllSessions()
    {
        var store = await _sessions;
        return await store.FindAllAsync();
    }

    /// <summary>
    /// Closing current session and clear the cookie
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public async Task<bool> CloseSession(Context ctx)
    {
        var session = await GetContext(ctx, false);
        if (session == null) return false;

        var store = await _sessions;
        await store.RemoveAsync(session.Id);
        var cookie = ctx.Cookies[_config.Cookie];
        if (cookie != null)
        {
            cookie.Expired = true;
            cookie.Value = "";
        }
        return true;
    }
}