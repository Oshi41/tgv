using System;

namespace tgv_core.api;

public class CachePolicy<T>
{
    /// <summary>
    /// Default cache policy. Do not support <see cref="StoreType.Custom"/>!
    /// </summary>
    /// <param name="type">Store type</param>
    /// <exception cref="ArgumentException"></exception>
    public CachePolicy(StoreType type)
    {
        if (type == StoreType.Custom)
            throw new ArgumentException("Use another contructor instead");
        
        StoreType = type;
    }

    public CachePolicy(Func<Context, T, bool> isAlive)
    {
        IsAlive = isAlive;
        StoreType = StoreType.Custom;
    }

    /// <summary>
    /// Time-expiring policy
    /// </summary>
    /// <param name="when">Expire time</param>
    public CachePolicy(DateTime when)
    {
        StoreType = StoreType.Custom;
        IsAlive = (_, _) => when > DateTime.Now;
    }
    
    public StoreType StoreType { get; }

    public Func<Context, T, bool>? IsAlive { get; set; }
}