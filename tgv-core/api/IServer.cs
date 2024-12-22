using System;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using NLog;

namespace tgv_core.api;

/// <summary>
/// Delegate representing an asynchronous HTTP request handler that processes requests within a given context.
/// It allows for the continuation of the request processing pipeline and optionally handles exceptions.
/// </summary>
/// <param name="ctx">The HTTP context for the current request.</param>
/// <param name="next">The action to invoke the next handler in the pipeline.</param>
/// <param name="e">Optional exception parameter for handling errors.</param>
public delegate Task HttpHandler(Context ctx, Action next, Exception? e = null);

/// <summary>
/// Delegate for handling server-side processing, focusing on managing requests and optionally dealing with exceptions within a specific context.
/// </summary>
/// <param name="ctx">The context in which the server request is being processed.</param>
/// <param name="e">Optional exception parameter for error handling.</param>
public delegate Task ServerHandler(Context ctx, Exception? e = null);

/// <summary>
/// Abstract class representing a server.
/// </summary>
public abstract class IServer
{
    public ServerHandler Handler { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the server is currently listening for incoming connections.
    /// </summary>
    /// <value>
    /// <c>true</c> if the server is started; otherwise, <c>false</c>.
    /// </value>
    public abstract bool IsListening { get; }

    /// <summary>
    /// Gets a value indicating whether the server is using HTTPS protocol.
    /// </summary>
    /// <value>
    /// <c>true</c> if the server is using HTTPS; otherwise, <c>false</c>.
    /// </value>
    public abstract bool IsHttps { get; }

    /// <summary>
    /// Gets the port number on which the server is currently listening for incoming connections.
    /// </summary>
    /// <value>
    /// The port number if the server is running and the endpoint is available; otherwise, <c>-1</c>.
    /// </value>
    public abstract int Port { get; }

    /// <summary>
    /// Gets the <see cref="Logger"/> instance associated with the server.
    /// </summary>
    /// <value>
    /// The <see cref="Logger"/> instance used to handle logging operations for the server.
    /// </value>
    public Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Starts the server asynchronously on the specified port.
    /// </summary>
    /// <param name="port">The port number on which the server should start listening.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    public abstract Task StartAsync(int port);

    /// <summary>
    /// Stops the server if it is currently listening.
    /// </summary>
    public abstract void Stop();
}