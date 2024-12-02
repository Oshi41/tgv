using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using tgv_core.api;
using tgv_server.api;

namespace tgv_server.imp;

public class SocketListener : IListener<TgvContext>
{
    private readonly IPEndPoint _address;
    private readonly Settings _settings;
    private readonly PipeScheduler _scheduler;
    private readonly IHttpParser<TgvContext> _parser;
    private readonly SocketAsyncEventArgs _args = new();

    private CancellationTokenSource? _cts;
    private Socket? _socket;
    private Task? _loop;

    public SocketListener(IPEndPoint address, Settings settings, PipeScheduler scheduler, IHttpParser<TgvContext> parser)
    {
        _address = address;
        _settings = settings;
        _scheduler = scheduler;
        _parser = parser;
    }

    public bool IsRunning => _loop != null;

    public void Run(ServerHandler handler, CancellationToken token)
    {
        Stop();
        token.Register(Stop);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        
        _socket = new Socket(_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) SetHandleInformation(_socket.Handle, 1, 0);
        if (Equals(_address.Address, IPAddress.IPv6Any)) _socket.DualMode = true;
        _socket.Bind(_address);
        _socket.Listen(512);

        _loop = Task.Run(() => RunLoopAsync(handler), token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _socket?.Close();
        _loop?.Dispose();
        _loop?.Wait();

        _loop = null;
    }
    
    private async Task RunLoopAsync(ServerHandler handler)
    {
        while (true)
        {
            var acceptedSocket = await _socket.AcceptAsync();
            acceptedSocket.NoDelay = true;
            _ = Process(acceptedSocket, handler);
        }
    }

    private async Task Process(Socket socket, ServerHandler handler)
    {
        var pipe = await Receive(socket);
        var ctx = await _parser.Parse(pipe);
        await handler(ctx);
    }
    
    private async Task<Pipe> Receive(Socket socket)
    {
        var pipe = new Pipe(new PipeOptions(
            pool: MemoryPool<byte>.Shared,
            readerScheduler: _scheduler,
            writerScheduler: _scheduler,
            pauseWriterThreshold: _settings.BufferSize,
            resumeWriterThreshold: _settings.BufferSize,
            useSynchronizationContext: false,
            minimumSegmentSize: 4096
        ));

        while (true)
        {
            var buffer = (ReadOnlyMemory<byte>)pipe.Writer.GetMemory(_settings.BufferSize);
            if (!MemoryMarshal.TryGetArray(buffer, out var segment))
                throw new InvalidOperationException("Buffer backed by array was expected");

            _args.SetBuffer(segment.Array, segment.Offset, segment.Count);
            socket.ReceiveAsync(_args);
            if (_args.BytesTransferred == 0) break;

            pipe.Writer.Advance(_args.BytesTransferred);
            await pipe.Writer.FlushAsync();
        }

        return pipe;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);
}