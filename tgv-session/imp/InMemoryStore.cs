using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using tgv_core.api;

namespace tgv_session.imp;

public class InMemoryStore() : IStore
{
    private readonly ObjectCache _cache = new MemoryCache("sessions");
    public async IAsyncEnumerator<SessionContext> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var context in _cache.Select(x => x.Value).OfType<SessionContext>())
        {
            yield return context;
        }
    }

    public Task<SessionContext> CreateNew(Guid id)
    {
        var session = new SessionContext(Guid.NewGuid(), DateTime.Now.AddHours(1));
        _cache.Add(session.Id.ToString(), session, session.Expires);
         Statics.GetMetric().CreateCounter<int>("memory_store_session_created", description: "Session was created")
            .Add(1,
                new KeyValuePair<string, object?>("session_id", session.Id),
                new KeyValuePair<string, object?>("session_expired", session.Expires));
        OnNewSession?.Invoke(this, session);
        return Task.FromResult(session);
    }

    public async Task<bool> TryRemove(Guid id)
    {
        var result = _cache.Remove(id.ToString()) != null;
        if (result)
        {
            OnRemovedSession?.Invoke(this, id);
             Statics.GetMetric().CreateCounter<int>("memory_store_session_removed", description: "Session was removed")
                .Add(1,
                    new KeyValuePair<string, object?>("session_id", id));
        }
        return result;
    }

    public event EventHandler<SessionContext>? OnSessionChanged;
    public event EventHandler<SessionContext>? OnNewSession;
    public event EventHandler<Guid>? OnRemovedSession;
}