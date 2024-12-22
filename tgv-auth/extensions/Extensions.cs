using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using tgv_auth.api;
using tgv_auth.api.storage;
using tgv_core.api;
using tgv_core.imp;
using tgv;

namespace tgv_auth.extensions;

public static class Extension
{
    public static string ToHeader(this AuthSchemes scheme) => scheme switch
    {
        AuthSchemes.Basic => "Basic",
        AuthSchemes.Bearer => "Bearer",
        AuthSchemes.Digest => "Digest",
        AuthSchemes.Hoba => "HOBA",
        AuthSchemes.Mutal => "Mutal",
        AuthSchemes.Negotiate or AuthSchemes.Ntlm => "Negotiate",
        AuthSchemes.Vapid => "vapid",
        AuthSchemes.Scram => "SCRAM-SHA-256",
        AuthSchemes.Aws => "AWS4-HMAC-SHA256",

        _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null)
    };

    public static T Parse<T>(this IDictionary<string, object> claims, string name, Func<object, T> convert)
    {
        if (!claims.TryGetValue(name, out var value))
            throw new KeyNotFoundException(name);

        if (value is not T result)
        {
            result = convert(value);
        }

        claims.Remove(name);
        return result;
    }

    public static void UseAuth<TCreds, TSession>(this IRouter app,
        ICredentialProvider<TCreds> credentialProvider,
        ISessionStorage<TCreds, TSession> sessionStorage,
        ICookieStorage<TSession>? cookieStorage = null,
        string path = "*")
        where TSession : IUserSession
        where TCreds : ICredentials
    {
        var mw = new AuthMiddleware<TCreds, TSession>(credentialProvider, sessionStorage, cookieStorage);
        app.Use(path, mw);
    }

    /// <summary>
    /// Returns current request's auth state
    /// </summary>
    /// <param name="ctx">HTTP context</param>
    /// <typeparam name="T">Auth type provided</typeparam>
    public static async Task<T?> Auth<T>(this Context ctx)
    {
        var task = ctx.Visited.ToList()
            .OfType<MiddlewareExtension>()
            .Select(x => x.Factory)
            .OfType<IExtensionProvider<T>>()
            .FirstOrDefault()
            ?.GetOrCreate(ctx);

        if (task is null) return default;
        
        return await task;
    } 
}