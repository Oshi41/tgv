using System;

namespace akasha.api;

[Flags]
public enum SrvStatus
{
    /// <summary>
    /// Server manually shutdown
    /// </summary>
    Stopped = 1 << 0,
    
    /// <summary>
    /// Server in a process of stopping
    /// </summary>
    Stopping = 1 << 1,
    
    /// <summary>
    /// Server in a process of starting
    /// </summary>
    Starting = 1 << 2,
    
    /// <summary>
    /// Server accepting connections
    /// </summary>
    Accepting = 1 << 3,
    
    /// <summary>
    /// Some error occured
    /// </summary>
    Error = 1 << 4,
}