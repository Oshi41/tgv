using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

namespace tgv_session;

public interface IStore : IAsyncEnumerable<SessionContext>
{
    /// <summary>
    /// open new session.
    /// <remarks>Guid may be changed, see <see cref="SessionContext.Id"/> for actual data</remarks>
    /// </summary>
    /// <param name="id">probable GUID</param>
    Task<SessionContext> CreateNew(Guid id);

    /// <summary>
    /// Try to close open session
    /// </summary>
    /// <param name="id">session ID</param>
    Task<bool> TryRemove(Guid id);
    
    Meter Metrics { get; internal set; }

    /// <summary>
    /// Called on session changed
    /// </summary>
    event EventHandler<SessionContext> OnSessionChanged;
    
    /// <summary>
    /// Called on creating new session
    /// </summary>
    event EventHandler<SessionContext> OnNewSession;
    
    /// <summary>
    /// Called after session removal
    /// </summary>
    event EventHandler<Guid> OnRemovedSession;
}