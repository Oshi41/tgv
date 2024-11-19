using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace tgv_session;

public interface IStore
{
    /// <summary>
    /// Retrieves session
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Session or null if no session founded</returns>
    Task<SessionContext?> FindAsync(Guid id);

    /// <summary>
    /// Find all opened sessions
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<SessionContext>> FindAllAsync();
    
    /// <summary>
    /// Removes session from store
    /// </summary>
    /// <param name="id">Session ID</param>
    Task RemoveAsync(Guid id);
    
    /// <summary>
    /// Stores current session
    /// </summary>
    /// <param name="context">Session context with ID provided</param>
    Task PutAsync(SessionContext context);
}