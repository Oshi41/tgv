using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using tgv_core.api;
using tgv;

namespace tgv_session;

class ContextExtension : ExtensionFactory<SessionContext>
{
    private readonly IStore _store;
    private readonly string _cookieName;
    private bool _loaded;

    public ContextExtension(IStore store, string cookieName)
    {
        _store = store;
        _cookieName = cookieName;

        _store.OnNewSession += OnAdd;
        _store.OnSessionChanged += OnAdd;
        _store.OnRemovedSession += OnRemove;
        
        _ = Load();
    }

    private void OnRemove(object? sender, Guid id) => RemoveKey(id);

    private async void OnAdd(object? sender, SessionContext ctx)
    {
        try
        {
            RemoveKey(GetKey(ctx));
            await Add(GetKey(ctx), ctx, CreateCachePolicy(ctx));
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error during adding session: {e}");
        }
    }

    private async Task Load()
    {
        _loaded = false;
        Clear();

        try
        {
            await foreach (var ctx in _store)
            {
                OnAdd(null, ctx);
            }
        }
        finally
        {
            _loaded = true;
        }
    }

    protected override IComparable GetKey(Context ctx)
    {
        var cookie = string.IsNullOrEmpty(_cookieName)
            ? null
            : ctx.Cookies[_cookieName];

        if (cookie != null)
        {
            if (!cookie.Expired)
            {
                if (Guid.TryParse(cookie.Value, out var guid))
                {
                    return guid;
                }

                ctx.Logger.Debug($"Wrong session cookie format: {cookie.Value}");
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

        return ctx.TraceId;
    }

    protected override IComparable GetKey(SessionContext context) => context.Id;

    protected override async Task<SessionContext?> GetOrCreateInternal(Context context, IComparable key)
    {
        // not loaded yet
        if (!_loaded) return null;

        context.Logger.Debug($"Opening new session [{key}]: {context}");
        
        var result = await _store.CreateNew(key as Guid? ?? Guid.NewGuid());
        context.Logger.Debug($"Session {GetKey(result)} created");
        return result;
    }

    protected override SessionContext? OnResolved(IComparable key, Context http, SessionContext? context)
    {
        if (!string.IsNullOrEmpty(_cookieName))
        {
            var cookie = http.Cookies[_cookieName];
            var valid = cookie is { Expired: false, Value: not null, HttpOnly: true };
            var shouldBeValid = context != null;
            if (valid != shouldBeValid)
            {
                // should expire cookie
                if (cookie != null && context == null)
                {
                    cookie.Expired = true;
                    cookie.Value = "";
                } 
                // renew cookie (if needed)
                else if (cookie != null && context != null)
                {
                    cookie.Expired = false;
                    cookie.Expires = context.Expires;
                    cookie.HttpOnly = true;
                    cookie.Value = GetKey(context).ToString();
                }
                // add new cookie
                else if (cookie == null && context != null)
                {
                    http.Cookies.Add(new Cookie(_cookieName, GetKey(context).ToString())
                    {
                        Expires = context.Expires,
                        HttpOnly = true,
                    });
                }
            }
        }

        return context;
    }

    // should expire after some time
    protected override CachePolicy<SessionContext>? CreateCachePolicy(Context context, SessionContext payload)
    {
        return CreateCachePolicy(payload);
    }

    private CachePolicy<SessionContext> CreateCachePolicy(SessionContext payload)
    {
        return new CachePolicy<SessionContext>(payload.Expires);
    }
}

public static class SessionMiddleware
{
    private static readonly ConditionalWeakTable<App, ContextExtension> _sessions = new();
    private static readonly ConditionalWeakTable<App, IStore> _stores = new();

    public static void UseSession(this App app, IStore store, string cookieName)
    {
        if (!_sessions.TryGetValue(app, out _))
        {
            var ext = new ContextExtension(store, cookieName);
            app.Use(ext);
            _sessions.Add(app, ext);
            _stores.Add(app, store);
        }
    }

    /// <summary>
    /// Returns session from current context
    /// </summary>
    /// <param name="context">HTTP request</param>
    public static async Task<SessionContext?> Session(this Context context)
    {
        var app = context.GetApp();
        if (app == null) return null;
        
        if (!_sessions.TryGetValue(app, out var session) || session == null) return null;

        return await session.GetOrCreate(context);
    }

    /// <summary>
    /// Returns liked store to application
    /// </summary>
    /// <param name="app">Current Application</param>
    public static IStore? GetStore(this App app) => _stores.TryGetValue(app, out var store) ? store : null;

    /// <summary>
    /// returns all active sessions
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<(Context, SessionContext)> GetAllSessions(this App app)
    {
        if (_sessions.TryGetValue(app, out var extension))
        {
            await foreach (var (context, session) in extension)
            {
                if (session != null) 
                    yield return (context, session);
            }
        }
    }
}