using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using tgv_common.api;

namespace tgv_auth;

public static class AuthMiddleware
{
    private static ConditionalWeakTable<Context, ClaimsIdentity> _identities = new();

    private static IEnumerable<string> GetAuthHeaders(Context ctx, string? cookieName)
    {
        var header = ctx.ClientHeaders["Authorization"];
        if (!string.IsNullOrEmpty(header))
            yield return header;

        if (!string.IsNullOrEmpty(cookieName))
        {
            header = ctx.Cookies[cookieName]?.Value;
            if (!string.IsNullOrEmpty(header) && header != null)
                    yield return header;
        }
    }
    
    public static HttpHandler Auth(string cookieName, params IAuthStrategy[] strategies) => async (context, next, e) =>
    {
        // get identity from context
        var identity = context.GetIdentity();
        if (identity is { IsAuthenticated: true })
        {
            next();
            return;
        }
        
        foreach (var header in GetAuthHeaders(context, cookieName))
        {
            foreach (var strategy in strategies)
            {
                if (header.StartsWith(strategy.Scheme))
                {
                    identity = await strategy.GetIdentity(header, true);
                    if (identity is { IsAuthenticated: true })
                    {
                        _identities.Add(context, identity);
                        
                        // remember auth in cookie
                        if (!string.IsNullOrEmpty(cookieName))
                        {
                            context.Cookies.Add(new Cookie(cookieName, header)
                            {
                                Expires = DateTime.Now.AddHours(1),
                                HttpOnly = true,
                                Secure = true,
                            });
                        }
                        
                        next();
                        return;
                    }
                }
            }
        }

        var challenge = string.Join(", ", strategies.Select(x => x.Challenge(context)));
        context.ResponseHeaders["WWW-Authenticate"] = challenge;

        // clear cookie
        if (!string.IsNullOrEmpty(cookieName))
        {
            context.Cookies.Add(new Cookie(cookieName, "")
            {
                Expired = true,
                Discard = true,
                Expires = DateTime.UtcNow.AddDays(-7)
            });
        }
        
        await context.Send(HttpStatusCode.Unauthorized);
    };

    public static ClaimsIdentity? GetIdentity(this Context ctx)
    {
        if (_identities.TryGetValue(ctx, out var result))
            return result;
        
        return null;
    }
}