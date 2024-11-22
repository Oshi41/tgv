using System.Security.Claims;
using System.Threading.Tasks;
using tgv_common.api;

namespace tgv_auth;

/// <summary>
/// Auth strategy interface
/// </summary>
public interface IAuthStrategy
{
    /// <summary>
    /// Authentication scheme, using in header
    /// </summary>
    string Scheme { get; }

    /// <summary>
    /// Obtaining identity from credentials and do auth if needed
    /// </summary>
    /// <param name="header">Authentication or Proxy-Authentication header value</param>
    /// <param name="doAuth">Should perform authentification?</param>
    /// <returns>Identity or null if no user found <p/>
    /// </returns>
    Task<ClaimsIdentity?> GetIdentity(string header, bool doAuth);
    
    /// <summary>
    /// If auth request fails, server must provide info about supported auth challanges. <p/>
    /// Value from here will be sent via WWW-Authenticate header 
    /// </summary>
    /// <param name="ctx">Context request</param>
    /// <returns>string header content</returns>
    string Challenge(Context ctx);

    /// <summary>
    /// Converts identity to header
    /// </summary>
    /// <param name="identity">Logged in identity</param>
    /// <returns>header content</returns>
    string ToHeader(ClaimsIdentity identity);
}