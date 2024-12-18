using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace tgv_session.imp;

public class InMemoryStore : IStore
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
        OnNewSession?.Invoke(this, session);
        return Task.FromResult(session);
    }

    public async Task<bool> TryRemove(Guid id)
    {
        var result = _cache.Remove(id.ToString()) != null;
        if (result) OnRemovedSession?.Invoke(this, id);
        return result;
    }

    public event EventHandler<SessionContext>? OnSessionChanged;
    public event EventHandler<SessionContext>? OnNewSession;
    public event EventHandler<Guid>? OnRemovedSession;
}