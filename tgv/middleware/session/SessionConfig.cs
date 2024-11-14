using System.Runtime.Caching;

namespace tgv.middleware.session;

public class SessionConfig
{
    /// <summary>
    /// Must be thread safe
    /// </summary>
    public Func<Task<Guid>> GenerateId { get; set; } = () => Task.FromResult(Guid.NewGuid());
    public string Cookie { get; set; } = "_tgv-session";
    public TimeSpan Expire { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Cache must be thread safe
    /// </summary>
    public Func<Task<ObjectCache>> CreateCache { get; set; } = () => Task.FromResult<ObjectCache>(new MemoryCache("sessions"));
}