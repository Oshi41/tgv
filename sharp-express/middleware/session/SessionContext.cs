using System.Net;
using sharp_express.core;

namespace sharp_express.middleware.session;

public sealed class SessionContext
{
    public SessionContext(Guid id, DateTime expiresOn)
    {
        Id = id;
        Expires = expiresOn;

        Touch();
    }

    /// <summary>
    /// Uniq ID of session
    /// </summary>
    public Guid Id { get; }
    
    /// <summary>
    /// When session expires
    /// </summary>
    public DateTime Expires { get; }
    
    /// <summary>
    /// Last request time from this session
    /// </summary>
    public DateTime LastTouch { get; private set; }

    /// <summary>
    /// Is current session expired
    /// </summary>
    public bool IsExpired => DateTime.Now > Expires;

    /// <summary>
    /// Recording last session request time
    /// </summary>
    public SessionContext Touch()
    {
        LastTouch = DateTime.Now;
        return this;
    }
    

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;

        if (ReferenceEquals(this, obj)) return true;

        return obj is SessionContext ctx && Equals(Id, ctx.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}