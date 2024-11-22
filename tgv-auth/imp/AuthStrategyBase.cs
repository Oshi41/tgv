using System;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Threading.Tasks;
using tgv_core.api;
using tgv_core.imp;

namespace tgv_auth.imp;

public abstract class AuthStrategyBase<T> : IAuthStrategy
{
    protected readonly Logger _logger;
    protected readonly ObjectCache _identities = new MemoryCache("identities");
    protected readonly IStore<T> _store;

    public AuthStrategyBase(IStore<T> store, Logger logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<ClaimsIdentity?> GetIdentity(string header, bool doAuth)
    {
        var creds = GetCredentials(header);
        if (creds == null)
        {
            _logger.Debug($"No credentials founded for {header}");
            return null;
        }

        var id = GetUniqueId(creds);
        var identity = _identities.Get(id) as ClaimsIdentity;
        if (identity is { IsAuthenticated: true })
        {
            return identity;
        }

        _logger.Debug($"Identity is null of unauthorized: {identity?.Name}");
        if (!doAuth) return null;

        var claims = await GetUserAsync(creds);
        if (claims == null)
        {
            _logger.Debug($"No such user: {creds}");
            return null;
        }

        var imp = new ClaimsIdentityImp(Scheme, claims.Name, claims.Role, claims.Claims);
        _identities.Add(id, imp, DateTimeOffset.Now.Add(claims.Expire));
        return identity;
    }

    /// <summary>
    /// Retreives credentials from auth header
    /// </summary>
    /// <param name="header">Authentication header</param>
    /// <returns>Credentials or null of not succeeded</returns>
    protected abstract T? GetCredentials(string header);

    /// <summary>
    /// Uniq ID of credentials.
    /// </summary>
    /// <param name="credentials"></param>
    /// <returns></returns>
    protected abstract string GetUniqueId(T credentials);

    /// <summary>
    /// Searching the user in any adaptor (DB \ in memory)
    /// </summary>
    /// <param name="credentials">User credentials</param>
    /// <returns></returns>
    protected virtual Task<UserEntry?> GetUserAsync(T credentials) => _store.FindAsync(credentials);

    public abstract string Scheme { get; }
    public abstract string Challenge(Context ctx);
    public abstract string? ToHeader(ClaimsIdentity identity);
}