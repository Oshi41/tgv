using System;

namespace akasha.api;

[Flags]
public enum SessionStatus
{
    /// <summary>
    /// socket was disconnected
    /// </summary>
    Disonnected = 1 << 0,
    
    /// <summary>
    /// Just connected
    /// </summary>
    Connected = 1 << 1,
    
    /// <summary>
    /// Receiving packets from client
    /// </summary>
    Receiving = 1 << 2,
    
    /// <summary>
    /// Sending packets to client
    /// </summary>
    Sending = 1 << 3,
    
    /// <summary>
    /// Disconnecting now
    /// </summary>
    Disonnecting = 1 << 4,
    
    /// <summary>
    /// Session error
    /// </summary>
    Error = 1 << 5,
    
    /// <summary>
    /// Request was already sent
    /// </summary>
    Sent = 1 << 6,
}