using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace tgv_auth;

/// <summary>
/// Use rentry record
/// </summary>
/// <param name="Name">Unique user name</param>
/// <param name="Expire">When it will expire</param>
/// <param name="Role">User role</param>
/// <param name="Claims">User claims</param>
public record UserEntry(string Name, TimeSpan Expire, string Role, Dictionary<string, object> Claims);

/// <summary>
/// Adapter for requesting user entry
/// </summary>
/// <typeparam name="T">Credentials type</typeparam>
public interface IStore<in T>
{
    /// <summary>
    /// Finds user from provided credentials. <p/>
    /// Returns provided claims or null if user was not found
    /// </summary>
    /// <param name="credentials"></param>
    /// <returns>
    /// Null if user was not found <p/>
    /// Provided user claims
    /// </returns>
    Task<UserEntry?> FindAsync(T credentials);
}