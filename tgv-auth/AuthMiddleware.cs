using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using tgv_auth.api;
using tgv_auth.api.storage;
using tgv_core.api;
using ICredentials = tgv_auth.api.ICredentials;

namespace tgv_auth;

public static class AuthMiddleware
{
    private static readonly ConditionalWeakTable<Context, IUserSession> _sessions = new();

    public static HttpHandler UseAuth<TCreds, TSession>(ICredentialProvider<TCreds> credentialProvider,
        ISessionStorage<TCreds, TSession> sessionStorage,
        ICookieStorage<TSession>? cookieStorage = null)
        where TCreds : ICredentials
        where TSession : IUserSession
    {
        async Task<bool> TryAuth(Context ctx, TSession? session)
        {
            if (session == null) return false;

            switch (await sessionStorage.GetStatus(session))
            {
                // user was not found
                case SessionStatus.NotFound:
                    ctx.Logger.Debug($"No user session found for {session}");
                    return false;

                // user session expired, need to refresh
                case SessionStatus.Expired:
                    var other = await sessionStorage.Refresh(session);
                    if (other == null || !other.IsValid())
                    {
                        ctx.Logger.Debug($"Refresh failed for {session}");
                        return false;
                    }

                    ctx.Logger.Debug($"Session was refreshed {session} => {other}");
                    session = other;
                    break;

                // session is active
                case SessionStatus.Active:
                    ctx.Logger.Debug($"Session confirmed as active: {session}");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // store session in local storage / cookies
            StoreSession(ctx, session);
            return true;
        }

        void StoreSession(Context ctx, TSession session)
        {
            _sessions.Add(ctx, session);

            var cookie = cookieStorage?.CreateCookie(ctx, session);
            if (cookie != null)
                ctx.Cookies.Add(cookie);
        }

        return async (ctx, next, _) =>
        {
            // first priority - local cache
            if (await TryAuth(ctx, ctx.GetAuthSession<TSession>()))
            {
                next();
                return;
            }

            // second priority - cookies
            if (await TryAuth(ctx, cookieStorage?.GetUserSession(ctx)))
            {
                next();
                return;
            }

            Exception e = null;

            try
            {
                // obtaining credentials
                var creds = credentialProvider.GetCredentials(ctx);
                if (creds != null)
                {
                    var session = await sessionStorage.Login(creds);
                    if (session != null && session.IsValid())
                    {
                        ctx.Logger.Debug($"Login successful for {session}");
                        StoreSession(ctx, session);
                        next();
                        return;
                    }
                }
                else
                {
                    ctx.Logger.Debug($"Auth credential not found");
                }
            }
            catch (Exception ex)
            {
                e = ex;
            }

            ctx.ResponseHeaders["WWW-Authenticate"] = credentialProvider.GetChallenge(ctx, e);
        };
    }

    public static T? GetAuthSession<T>(this Context ctx)
        where T : IUserSession
    {
        return _sessions.TryGetValue(ctx, out var userSession) && userSession is T authSession
            ? authSession
            : null;
    }
}