using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace tgv_session.imp;

public class InMemoryStore : IStore
{
    private readonly SessionConfig _config;
    private readonly ObjectCache _cache;
    
    public InMemoryStore(SessionConfig config)
    {
        _config = config;
        _cache = new MemoryCache("Session");
    }

    public Task<SessionContext?> FindAsync(Guid id)
    {
        return Task.FromResult((SessionContext?)_cache.Get(id.ToString()));
    }

    public Task<IEnumerable<SessionContext>> FindAllAsync()
    {
        var snapshot = _cache.Select(x => x.Value)
            .OfType<SessionContext>()
            .ToList();
        return Task.FromResult(snapshot.AsEnumerable());
    }

    public Task RemoveAsync(Guid id)
    {
        _cache.Remove(id.ToString());
        return Task.CompletedTask;
    }

    public Task PutAsync(SessionContext context)
    {
        _cache.Add(new CacheItem(context.Id.ToString(), context), new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.Now + _config.Expire,
        });

        return Task.CompletedTask;
    }
}