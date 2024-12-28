using System.Net.Sockets;

namespace akasha.extensions;

public static class SocketExtensions
{
    public static bool IsSuccess(this SocketAsyncEventArgs e) => e.SocketError == SocketError.Success;

    /// <summary>
    /// This kind of errors can be swallowed
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static bool IsConnectionLost(this SocketAsyncEventArgs e) => e.SocketError is SocketError.ConnectionAborted
        or SocketError.ConnectionRefused
        or SocketError.ConnectionReset
        or SocketError.OperationAborted
        or SocketError.Shutdown;

    /// <summary>
    /// This kind of errors should be treat as fatal
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static bool IsError(this SocketAsyncEventArgs e) => !e.IsSuccess() && !e.IsConnectionLost();

    public static void SetupSocket(this Socket socket, bool reuseAddress = true, bool exclusiveAccess = true,
        bool dualMode = true)
    {
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, reuseAddress);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, exclusiveAccess);
        if (socket.AddressFamily == AddressFamily.InterNetworkV6)
            socket.DualMode = dualMode;
    }
}