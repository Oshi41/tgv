using System;
using System.Collections;
using System.Collections.Generic;

namespace tgv_auth.api;

public abstract class IUserSession : IEnumerable<KeyValuePair<string, object>>
{
    private IDictionary<string, object> _data;

    protected IUserSession(DateTime expired, DateTime start)
    {
        Expired = expired;
        Start = start;
    }

    /// <summary>
    /// Expired time
    /// </summary>
    public DateTime Expired { get; }

    /// <summary>
    /// User session started date
    /// </summary>
    public DateTime Start { get; }

    /// <summary>
    /// Current session claims
    /// </summary>
    /// <param name="name"></param>
    public object? this[string name]
    {
        get => _data[name];
        set
        {
            if (value == null)
                _data.Remove(name);
            else
                _data[name] = value;
        }
    }

    /// <summary>
    /// Is current session valid
    /// </summary>
    /// <returns></returns>
    public virtual bool IsValid() => Start > DateTime.MinValue
                                     // expired is swithed off
                                     && (Expired == DateTime.MinValue 
                                         // or somewhere in a future
                                        || Expired > DateTime.Now);

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return _data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}