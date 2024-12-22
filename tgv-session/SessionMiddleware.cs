using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NLog;
using tgv_core.api;
using tgv_core.extensions;
using tgv_core.imp;
using tgv;

namespace tgv_session;

class ContextExtension : ExtensionFactory<SessionContext, Guid>
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    public IStore Store { get; }
    private readonly string _cookieName;
    private bool _loaded;

    public ContextExtension(IStore store, string cookieName)
    {
        Store = store;
        _cookieName = cookieName;

        Store.OnNewSession += OnAdd;
        Store.OnSessionChanged += OnAdd;
        Store.OnRemovedSession += OnRemove;

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
            _logger.Error("Error during adding session: {e}", e);
        }
    }

    private async Task Load()
    {
        _loaded = false;
        Clear();

        try
        {
            await foreach (var ctx in Store)
            {
                OnAdd(null, ctx);
            }
        }
        finally
        {
            _loaded = true;
        }
    }

    protected override Guid GetKey(Context ctx)
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

                ctx.Logger.Debug("Wrong session cookie format: {cookie}", cookie.Value);
            }
            else
            {
                ctx.Logger.Debug("Session cookie was expired: {cookie}", cookie.Value);
            }
        }
        else
        {
            ctx.Logger.Debug("Session cookie was not found");
        }

        return ctx.TraceId;
    }

    protected override Guid GetKey(SessionContext context) => context.Id;

    protected override async Task<SessionContext?> GetOrCreateInternal(Context context, Guid key)
    {
        // not loaded yet
        if (!_loaded) return null;

        context.Logger.Debug("Opening new session [{key}]: {context}", key, context);

        var result = await Store.CreateNew(key as Guid? ?? Guid.NewGuid());
        context.Logger.Debug("Session {result} created", GetKey(result));
        return result;
    }

    protected override SessionContext? OnResolved(Guid key, Context http, SessionContext? context)
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
        
        http.Logger.Debug("Session={key}", key);

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
    public static void UseSession(this IRouter app, IStore store, string cookieName)
    {
        app.Use(new ContextExtension(store, cookieName));
    }

    /// <summary>
    /// Returns session from current context
    /// </summary>
    /// <param name="context">HTTP request</param>
    public static async Task<SessionContext?> Session(this Context context)
    {
        var task = context.Visited.OfType<MiddlewareExtension>()
            .Select(x => x.Factory)
            .OfType<IExtensionProvider<SessionContext>>()
            .FirstOrDefault()
            ?.GetOrCreate(context);

        if (task != null) return await task;

        return null;
    }

    /// <summary>
    /// Returns liked store to application
    /// </summary>
    /// <param name="app">Current Application</param>
    public static IStore? GetStore(this IRouter app) => app.AllRoutes()
        .OfType<MiddlewareExtension>()
        .Select(x => x.Factory)
        .OfType<ContextExtension>()
        .FirstOrDefault()
        ?.Store;

    /// <summary>
    /// returns all active sessions
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<(Context, SessionContext)> GetAllSessions(this App app)
    {
        var mw = app.AllRoutes()
            .OfType<MiddlewareExtension>()
            .Select(x => x.Factory)
            .OfType<IExtensionProvider<SessionContext>>()
            .FirstOrDefault();

        if (mw != null)
        {
            await foreach (var (context, session) in mw)
            {
                yield return (context, session);
            }
        }
    }
}