using System.Diagnostics;
using System.Net;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using sharp_express.core;

namespace sharp_express.middleware.session;

public class SessionStore
{
    private readonly Express _app;
    private readonly SessionConfig _config;
    private readonly ConditionalWeakTable<IContext, SessionContext> _sessionsFields = new();
    private readonly Task<ObjectCache> _sessions;
    
    public SessionStore(Express app, SessionConfig config)
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
    public async Task<SessionContext?> GetContext(IContext ctx, bool shouldOpen)
    {
        if (!_sessionsFields.TryGetValue(ctx, out var result))
        {
            var cookie = ctx.Cookies[_config.Cookie];
            if (cookie is { Expired: false })
            {
                if (Guid.TryParse(cookie.Value, out var guid))
                {
                    if ((await _sessions).Get(guid.ToString()) is SessionContext session)
                    {
                        // reuse same instance
                        result = session;
                        // assotiate with context
                        _sessionsFields.Add(ctx, session);
                    }
                }
                else
                {
                    Debug.WriteLine($"TT[{ctx.TraceId}] Cannot read session id: {cookie.Value}");
                }
            }

            if (cookie?.Expired == true)
            {
                Debug.WriteLine($"TT[{ctx.TraceId}] S[{cookie.Value}] Session expired from cookie");
            }
        }

        // closing session explicitly
        if (result is { IsExpired: true })
        {
            Debug.WriteLine($"TT[{ctx.TraceId}] S[{result.Id}] Session expired from context");
            
            (await _sessions).Remove(result.Id.ToString());
            _sessionsFields.Remove(ctx);
            result = null;
        }

        if (shouldOpen && result == null)
        {
            Debug.WriteLine($"TT[{ctx.TraceId}] Starting session...");
            result = new SessionContext(await _config.GenerateId(), DateTime.Now + _config.Expire);

            var cookie = new Cookie(_config.Cookie, result.Id.ToString())
            {
                Expires = result.Expires
            };
            ctx.Cookies.Add(cookie);

            _sessionsFields.Add(ctx, result);
            (await _sessions).Add(new CacheItem(result.Id.ToString(), result), new CacheItemPolicy
            {
                AbsoluteExpiration = cookie.Expires
            });

            Debug.WriteLine($"TT[{ctx.TraceId}] S[{result.Id}] Session created");
        }

        return result;
    }

    /// <summary>
    /// Returns currently opened sessions
    /// </summary>
    /// <returns></returns>
    public async Task<IList<SessionContext>> GetAllSessions()
    {
        return (await _sessions)
            .Select(x => x.Value)
            .OfType<SessionContext>()
            .ToList();
    }

    /// <summary>
    /// Closing current session and clear the cookie
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public async Task<bool> CloseSession(IContext ctx)
    {
        var session = await GetContext(ctx, false);
        if (session == null) return false;

        _sessionsFields.Remove(ctx);
        (await _sessions).Remove(session.Id.ToString());
        var cookie = ctx.Cookies[_config.Cookie];
        if (cookie != null)
        {
            cookie.Expired = true;
            cookie.Value = "";
        }
        return true;
    }
}