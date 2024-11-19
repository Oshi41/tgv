using System;
using System.Threading.Tasks;
using tgv_session.imp;

namespace tgv_session;

public class SessionConfig
{
    /// <summary>
    /// Session config ctor
    /// </summary>
    /// <param name="createCache">Custom store creation function. <p/>
    /// <see cref="InMemoryStore"/> by default</param>
    /// <param name="generateId">Session ID generating function. <p/>
    /// Must be thread safe</param>
    /// <param name="cookie">Session cookie name <p/>
    /// _tgv_session by default</param>
    /// <param name="expire">Session expire time</param>
    public SessionConfig(Func<Task<IStore>>? createCache = null,
        Func<Task<Guid>>? generateId = null, 
        string? cookie = null,
        TimeSpan? expire = null)
    {
        CreateCache = createCache ?? (() => Task.FromResult<IStore>(new InMemoryStore(this)));
        GenerateId = generateId ?? (() => Task.FromResult<Guid>(Guid.NewGuid()));
        Cookie = cookie ?? "_tgv_session";
        Expire = expire ?? TimeSpan.FromHours(1);
    }
    
    /// <summary>
    /// Must be thread safe
    /// </summary>
    public Func<Task<Guid>> GenerateId { get; }
    public string Cookie { get;  }
    public TimeSpan Expire { get;  }

    /// <summary>
    /// Cache must be thread safe
    /// </summary>
    public Func<Task<IStore>> CreateCache { get; }
}