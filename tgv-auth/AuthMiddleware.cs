using System;
using System.Threading.Tasks;
using tgv_auth.api;
using tgv_auth.api.storage;
using tgv_core.api;

namespace tgv_auth;

/// <summary>
/// Key contains session or credentials for authentification
/// </summary>
public class AuthKey : IEquatable<AuthKey>
{
    public AuthKey(IUserSession? session, ICredentials? credentials)
    {
        Session = session;
        Credentials = credentials;
    }

    public IUserSession? Session { get;  }
    public ICredentials? Credentials { get;  }

    public bool Equals(AuthKey? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(Session, other.Session) && Equals(Credentials, other.Credentials);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((AuthKey)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Session != null ? Session.GetHashCode() : 0) * 397) ^ (Credentials != null ? Credentials.GetHashCode() : 0);
        }
    }
}

public class AuthMiddleware<TCreds, TSession> : ExtensionFactory<TSession, AuthKey>
    where TSession : IUserSession
    where TCreds : ICredentials
{
    private readonly ICredentialProvider<TCreds> _credentialProvider;
    private readonly ISessionStorage<TCreds, TSession> _sessionStorage;
    private readonly ICookieStorage<TSession>? _cookieStorage;

    public AuthMiddleware(ICredentialProvider<TCreds> credentialProvider,
        ISessionStorage<TCreds, TSession> sessionStorage,
        ICookieStorage<TSession>? cookieStorage = null)
    {
        _credentialProvider = credentialProvider;
        _sessionStorage = sessionStorage;
        _cookieStorage = cookieStorage;
    }

    protected override AuthKey? GetKey(Context context)
    {
        if (_cookieStorage?.GetUserSession(context) is { } session)
            return new AuthKey(session, null);
        
        if (_credentialProvider.GetCredentials(context) is { } credentials)
            return new AuthKey(null, credentials);

        return null;
    }

    protected override AuthKey GetKey(TSession context)
    {
        return new AuthKey(context, null);
    }

    protected override async Task<TSession?> GetOrCreateInternal(Context context, AuthKey key)
    {
        if (key.Session is TSession session)
        {
            context.Logger.Debug("Requesting session {session} status", session);
            var status = await _sessionStorage.GetStatus(session);

            // all works
            if (status == SessionStatus.Active)
            {
                context.Logger.Debug("Session {session} is active", session);
                Put(session, context);
                return session;
            }
            
            // delete expired session
            RemoveKey(key);

            // re-auth
            if (status == SessionStatus.Expired)
            {
                context.Logger.Debug("Session {session} is expired, refreshing...", session);
                var res = await _sessionStorage.Refresh(session);
                if (res != null)
                {
                    Put(session, context);
                    context.Logger.Debug("Session {session} refreshed", session);
                    return res;
                }
                
                context.Logger.Debug("Session {session} was not refreshed", session);
            }

            if (status == SessionStatus.NotFound)
            {
                context.Logger.Debug("Session {session} not found", session);
            }
        }

        if (key.Credentials is TCreds credentials)
        {
            context.Logger.Debug("Auth with credentials");
            // remove obsolete credentials key
            RemoveKey(key);

            var sessionNew = await _sessionStorage.Login(credentials);
            if (sessionNew?.IsValid() == true)
            {
                context.Logger.Debug("Session {sessionNew} logged in", sessionNew);
                Put(sessionNew, context);
                return sessionNew;
            }

            context.Logger.Debug("Session {sessionNew} failed to log in", sessionNew);
        }

        return null;
    }

    private void Put(TSession session, Context? http = null)
    {
        var key = new AuthKey(session, null);
        RemoveKey(key);
        _ = Add(key, session, CreateCachePolicy(session), http);
    }

    protected override TSession? OnResolved(AuthKey key, Context http, TSession? context)
    {
        if (context != null && _cookieStorage != null) 
            http.Cookies.Add(_cookieStorage.CreateCookie(http, context));
        
        if (context != null) 
            http.Logger.WithProperty("_20auth", $"{_credentialProvider.Scheme},{context}");

        return context;
    }

    protected override CachePolicy<TSession>? CreateCachePolicy(Context context, TSession payload) 
        => CreateCachePolicy(payload);

    private CachePolicy<TSession> CreateCachePolicy(IUserSession session) => new(session.Expired);
}