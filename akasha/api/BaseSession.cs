using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using akasha.extensions;
using akasha.parser;

namespace akasha.api;

public abstract class BaseSession
{
    private SocketAsyncEventArgs? _receiveArgs;
    private CancellationTokenSource? _receiveTokenSource;
    private SocketAsyncEventArgs? _sendArgs;
    private CancellationTokenSource? _sendTokenSource;
    
    /// <param name="id">Uniq session ID</param>
    /// <param name="socket">Session socket</param>
    /// <param name="token">Cancellation token for receive from client</param>
    protected BaseSession(Guid id, Socket socket)
    {
        Id = id;
        Socket = socket;
        SendArgs = new();
        ReceiveArgs = new();
        ReceiveTokenSource = new();
        SendTokenSource = new();

        Socket.SetupSocket();

        if (Socket.Connected)
            Status = SessionStatus.Connected;
        
        // running receive loop
        _ = Task.Run(ReceiveAsync, ReceiveTokenSource.Token);
    }

    #region Properties

    /// <summary>
    /// Session ID
    /// </summary>
    public Guid Id { get; }

    public SessionStatus Status { get; protected set; } = SessionStatus.Disonnected;

    #endregion

    #region Receive

    #region Properties

    /// <summary>
    /// Receiving from client
    /// </summary>
    protected SocketAsyncEventArgs? ReceiveArgs
    {
        get => _receiveArgs;
        set
        {
            if (_receiveArgs == value) return;

            var old = Interlocked.Exchange(ref _receiveArgs, value);
            if (old != null)
            {
                if (old.UserToken is IDisposable d) d.Dispose();
                old.Completed -= ProcessRequest;
                old.Dispose();
            }

            if (_receiveArgs != null)
            {
                _receiveArgs.Completed += ProcessRequest;
                _receiveArgs.UserToken = new BufferStream();
            }
        }
    }

    /// <summary>
    /// Cancellation token for receiving from client
    /// </summary>
    public CancellationTokenSource? ReceiveTokenSource
    {
        get => _receiveTokenSource;
        set
        {
            if (_receiveTokenSource == value) return;

            var old = Interlocked.Exchange(ref _receiveTokenSource, value);

            old?.Cancel();
            old?.Dispose();
        }
    }

    #endregion

    /// <summary>
    /// Receiving data from client
    /// </summary>
    protected virtual void ReceiveAsync()
    {
        // invalid session status
        if (Status != SessionStatus.Connected) return;

        // starting receiving
        Status |= SessionStatus.Receiving;

        ReceiveArgs ??= new();
        ReceiveTokenSource ??= new();

        // local copy
        var token = ReceiveTokenSource.Token;
        // starting parsing
        _ = Task.Run(ParseSocketRequestAsync, token);

        // stop receiving on token.Cancel() 
        token.Register(() =>
        {
            if (!Status.HasFlag(SessionStatus.Receiving)) return;

            Status &= ~SessionStatus.Receiving;
            ReceiveTokenSource = null;
        });

        while (Status.HasFlag(SessionStatus.Receiving))
        {
            token.ThrowIfCancellationRequested();

            if (!Socket.ReceiveAsync(ReceiveArgs))
            {
                ProcessRequest(Socket, ReceiveArgs);
            }
        }
    }

    /// <summary>
    /// Parsing socket message to HTTP request
    /// </summary>
    private async Task ParseSocketRequestAsync()
    {
        if (ReceiveArgs?.UserToken is not Stream stream) return;

        try
        {
            var req = await stream.ParseHttpResponse();
            // receiving stopped
            Status &= ~SessionStatus.Receiving;
            await HandleRequestAsync(req);
        }
        catch (Exception e)
        {
            OnError(e);
        }
    }

    /// <summary>
    /// Should be overriden to handle HTTP requests
    /// </summary>
    /// <param name="request">HTTP request</param>
    protected abstract Task HandleRequestAsync(HttpRequest request);

    #endregion

    #region Send

    #region Properties

    /// <summary>
    /// Sending to client
    /// </summary>
    protected SocketAsyncEventArgs? SendArgs
    {
        get => _sendArgs;
        set
        {
            if (_sendArgs == value) return;

            var old = Interlocked.Exchange(ref _sendArgs, value);
            if (old != null)
            {
                if (old.UserToken is IDisposable d) d.Dispose();
                old.Completed -= ProcessRequest;
                old.Dispose();
            }

            if (_sendArgs != null)
            {
                _sendArgs.Completed += ProcessRequest;
                _sendArgs.UserToken = new BufferStream();
            }
        }
    }

    /// <summary>
    /// Cancellation token for sending to client
    /// </summary>
    public CancellationTokenSource? SendTokenSource
    {
        get => _sendTokenSource;
        set
        {
            if (_sendTokenSource == value) return;

            var old = Interlocked.Exchange(ref _sendTokenSource, value);

            old?.Cancel();
            old?.Dispose();
        }
    }

    #endregion

    /// <summary>
    /// Sending data to client
    /// </summary>
    public virtual async Task SendAsync(HttpResponse response)
    {
        // invalid session status
        if (Status != SessionStatus.Connected || SendArgs == null) return;

        Status |= SessionStatus.Sending;

        // local copy
        var token = SendTokenSource!.Token;
        token.Register(() =>
        {
            if (!Status.HasFlag(SessionStatus.Sending)) return;

            Status &= ~SessionStatus.Sending;
            SendTokenSource = null;
        });

        SendArgs.BufferList = new List<ArraySegment<byte>>();
        var buffer = response.GetHttpWithoutBody();
        SendArgs.BufferList[0] = new ArraySegment<byte>(buffer, 0, buffer.Length);
        
        if (!Socket.SendAsync(SendArgs))
            ProcessRequest(Socket, SendArgs);

        if (response.Body != null)
        {
            Array.Clear(buffer, 0, buffer.Length);
            var read = 0;
            buffer = new byte[2048];

            while ((read = await response.Body.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                SendArgs.BufferList[0] = new ArraySegment<byte>(buffer, 0, read);
                if (!Socket.SendAsync(SendArgs))
                    ProcessRequest(Socket, SendArgs);
            }
        }
        
        Status &= ~SessionStatus.Sending;
        Status |= SessionStatus.Sent;
    }

    #endregion

    /// <summary>
    /// Called after session was diconnected
    /// </summary>
    public event EventHandler OnDisconnected;

    /// <summary>
    /// Disconnect the session
    /// </summary>
    /// <returns>'true' if the section was successfully disconnected, 'false' if the section is already disconnected</returns>
    public virtual bool Disconnect()
    {
        // already started or finished
        if (Status.HasFlag(SessionStatus.Disonnecting | SessionStatus.Disonnected))
        {
            return false;
        }

        SetStatusRespectingError(SessionStatus.Disonnecting);

        SendArgs = ReceiveArgs = null;
        ReceiveTokenSource = null;
        
        try
        {
            Socket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception)
        {
            // swallow
        }
        
        Socket.Close();
        SetStatusRespectingError(SessionStatus.Disonnected);
        
        OnDisconnected?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>
    /// Using socket
    /// </summary>
    protected Socket Socket { get; }

    protected void SetStatusRespectingError(SessionStatus status)
    {
        if (Status.HasFlag(SessionStatus.Error))
            status |= SessionStatus.Error;

        Status = status;
    }

    protected virtual void ProcessRequest(object sender, SocketAsyncEventArgs e)
    {
        if (e.IsSuccess()
            && e.LastOperation == SocketAsyncOperation.Receive
            && e.BytesTransferred > 0
            && e.UserToken is Stream stream)
        {
            // as BufferStream is used, call will block receive thread until first read
            stream.Write(e.Buffer, e.Offset, e.BytesTransferred);
        }

        if (!e.IsSuccess())
        {
            Disconnect();
        }
    }

    protected virtual void OnError(Exception? e = null)
    {
        Status |= SessionStatus.Error;

        try
        {
            Socket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception ex)
        {
            // swallow
        }
    }
}