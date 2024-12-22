using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using tgv_core.api;

namespace tgv_auth.api.storage;

/// <summary>
/// Generic session store
/// </summary>
/// <typeparam name="TCreds">Any possible credentials</typeparam>
/// <typeparam name="TSession"></typeparam>
public interface ISessionStorage<in TCreds, TSession>
    where TCreds : ICredentials
    where TSession : IUserSession
{
    /// <summary>
    /// Refreshing previous user session
    /// </summary>
    /// <param name="prev">Prev session stored on client side</param>
    /// <returns>Refreshed session or null if not possible</returns>
    /// <exception cref="Exception">Any exception with message provided to client</exception>
    Task<TSession?> Refresh(TSession prev);
    
    /// <summary>
    /// Create new session from credentials.
    /// Should expire all other sessions.
    /// </summary>
    /// <param name="credentials"></param>
    /// <exception cref="Exception">Any exception with message provided to client</exception>
    Task<TSession?> Login(TCreds credentials);
    
    /// <summary>
    /// Logout current user and close all the sessions
    /// </summary>
    /// <param name="session">Current session</param>
    /// <exception cref="Exception">Any exception with message provided to client</exception>
    Task Logout(TSession session);
    
    /// <summary>
    /// Checks if current session is active
    /// </summary>
    /// <param name="session">User session</param>
    /// <exception cref="Exception">Any exception with message provided to client</exception>
    Task<SessionStatus> GetStatus(TSession session);
    
    /// <summary>
    /// Retrieves all sessions
    /// </summary>
    Task<List<TSession>> GetSessions();
}