using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using akasha.extensions;

namespace akasha.api;

public abstract class BaseServer<T>
    where T : BaseSession
{
    private long _sessionsCount = 0;
    private SocketAsyncEventArgs? _serverSocketArgs;
    private CancellationTokenSource? _loopToken;

    protected BaseServer(EndPoint endpoint)
    {
        Endpoint = endpoint;
    }

    #region Public properties

    /// <summary>
    /// Server ID
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Working endpoint
    /// </summary>
    public EndPoint Endpoint { get; private set; }

    /// <summary>
    /// Current server working status
    /// </summary>
    public SrvStatus Status { get; protected set; } = SrvStatus.Stopped;

    #endregion

    #region Public methods

    /// <summary>
    /// Start the server
    /// </summary>
    /// <returns>'true' if the server was successfully started, 'false' if the server failed to start</returns>
    public virtual bool Start()
    {
        if (!Status.HasFlag(SrvStatus.Stopped))
        {
            Console.WriteLine("Server should be stopped to start him again");
            return false;
        }

        Status = SrvStatus.Starting;

        try
        {
            ServerSocketArgs = new SocketAsyncEventArgs();
            ServerSocket = CreateSocket();

            Console.WriteLine("Server started: {0}", Endpoint);
        }
        catch (Exception e)
        {
            Status &= SrvStatus.Error;
            Console.Error.WriteLine("Error during server starting: {0}", e);
            Stop();
            return false;
        }

        ServerSocket.SetupSocket();

        // Bind the acceptor socket to the endpoint
        ServerSocket.Bind(Endpoint);

        // Refresh the endpoint property based on the actual endpoint created
        Endpoint = ServerSocket.LocalEndPoint;

        // start listening
        ServerSocket.Listen(1024);

        Status = SrvStatus.Accepting;

        LoopToken = new();
        _ = Task.Run(ListenLoop, LoopToken.Token);

        return true;
    }

    /// <summary>
    /// Stop the server
    /// </summary>
    /// <returns>'true' if the server was successfully stopped, 'false' if the server is already stopped</returns>
    public bool Stop()
    {
        if (Status.HasFlag(SrvStatus.Stopping | SrvStatus.Stopped))
        {
            Console.WriteLine("Cannot stop server due to wrong status: {0}", Status);
            return false;
        }

        SetStatusRespectingError(SrvStatus.Stopping);

        try
        {
            LoopToken = null;

            // disconnect from all sessions
            Sessions.Clear((_, session) => session.Disconnect());

            ServerSocket?.Close();
            ServerSocketArgs = null;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("FATAL error during stopping server: {0}", e);
            throw;
        }

        SetStatusRespectingError(SrvStatus.Stopped);
        return true;
    }

    /// <summary>
    /// Restart the server
    /// </summary>
    /// <returns>'true' if the server was successfully restarted, 'false' if the server failed to restart</returns>
    public bool Restart()
    {
        // need to stop server
        if (!Status.HasFlag(SrvStatus.Stopping | SrvStatus.Stopped))
        {
            // trying to stop
            if (!Stop()) return false;
        }

        // than starting
        return Start();
    }

    #endregion

    #region Protected properties and fields

    /// <summary>
    /// Currently opened sessions
    /// </summary>
    protected ConcurrentDictionary<Guid, T> Sessions { get; } = new();

    /// <summary>
    /// Server working socket
    /// </summary>
    protected Socket? ServerSocket { get; set; }

    /// <summary>
    /// Event args for server socket
    /// </summary>
    protected SocketAsyncEventArgs? ServerSocketArgs
    {
        get => _serverSocketArgs;
        set
        {
            if (_serverSocketArgs == value) return;

            var old = Interlocked.Exchange(ref _serverSocketArgs, value);

            if (old != null)
            {
                old.Completed -= ProcessRequest;
                old.Dispose();
            }

            if (_serverSocketArgs != null)
            {
                _serverSocketArgs.Completed += ProcessRequest;
            }
        }
    }

    /// <summary>
    /// Cancellation source for server listener
    /// </summary>
    protected CancellationTokenSource? LoopToken
    {
        get => _loopToken;
        set
        {
            if (_loopToken == value) return;

            var old = Interlocked.Exchange(ref _loopToken, value);
            old?.Cancel();
            old?.Dispose();
        }
    }

    private void ProcessRequest(object sender, SocketAsyncEventArgs e)
    {
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Listening loop.
    /// <example>
    /// <code>
    /// var cts = new CancellationTokenSource();
    /// _loopTask = Task.Run(ListenLoop, cts.Token);
    /// </code>
    /// </example>
    /// </summary>
    protected virtual void ListenLoop()
    {
        if (ServerSocket == null) throw new ArgumentException(nameof(ServerSocket));
        ServerSocketArgs ??= new();
        LoopToken ??= new();

        // creating local token copy
        var token = LoopToken.Token;

        while (Status.HasFlag(SrvStatus.Accepting))
        {
            token.ThrowIfCancellationRequested();

            // Socket must be cleared since the context object is being reused
            ServerSocketArgs.AcceptSocket = null;

            // Async accept a new client connection
            if (!ServerSocket.AcceptAsync(ServerSocketArgs))
            {
                switch (ServerSocketArgs.SocketError)
                {
                    case SocketError.Success:
                        var id = CreateSessionId();
                        var session = NewSession(id, ServerSocketArgs.AcceptSocket!);
                        Sessions[id] = session;
                        session.OnDisconnected += (sender, args) =>
                        {
                            if (sender is T s)
                                Sessions.TryRemove(s.Id, out _);
                        };
                        break;

                    case SocketError.ConnectionAborted:
                    case SocketError.ConnectionRefused:
                    case SocketError.ConnectionReset:
                    case SocketError.OperationAborted:
                    case SocketError.Shutdown:

                        // TODO swallow?..
                        break;

                    default:
                        // TODO handle error?..
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Saving error flag
    /// </summary>
    /// <param name="status">Desired status</param>
    protected virtual void SetStatusRespectingError(SrvStatus status)
    {
        if (Status.HasFlag(SrvStatus.Error))
            status |= SrvStatus.Error;

        Status = status;
    }

    /// <summary>
    /// Uniq id generation
    /// </summary>
    /// <remarks>Must be no collision function!</remarks>
    protected virtual Guid CreateSessionId()
    {
        // increment total sessions count
        var id = Interlocked.Increment(ref _sessionsCount);
        var ticks = DateTime.UtcNow.ToBinary();
        return new Guid(BitConverter.GetBytes(id).Concat(BitConverter.GetBytes(ticks)).ToArray());
    }

    /// <summary>
    /// Creating socket on server starting
    /// </summary>
    /// <returns></returns>
    protected abstract Socket CreateSocket();

    /// <summary>
    /// Create new session with provided ID
    /// </summary>
    /// <param name="id">Uniq Session ID</param>
    /// <param name="socket">Connection socket</param>
    protected abstract T NewSession(Guid id, Socket socket);

    #endregion
}