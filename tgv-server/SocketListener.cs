using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using tgv_core.imp;

namespace tgv_server;

public class SocketListener(IPEndPoint address, Settings settings, PipeScheduler scheduler) : IDisposable
{
    private readonly SocketAsyncEventArgs _args = new();

    private Socket? _socket;
    private Task? _listenTask;

    public void Run(Func<Pipe, Task> handler, CancellationToken token)
    {
        Stop();
        token.Register(Stop);
        
        _socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) SetHandleInformation(_socket.Handle, 1, 0);
        if (Equals(address.Address, IPAddress.IPv6Any)) _socket.DualMode = true;
        _socket.Bind(address);
        _socket.Listen(512);

        _listenTask = Task.Run(() => RunLoopAsync(handler), token);
    }

    public void Stop()
    {
        _args.Dispose();
        _socket?.Dispose();
        _listenTask?.Dispose();
        _listenTask?.Wait();
    }

    private async Task RunLoopAsync(Func<Pipe, Task> handler)
    {
        while (true)
        {
            var acceptedSocket = await _socket.AcceptAsync();
            acceptedSocket.NoDelay = true;
            _ = Process(acceptedSocket, handler);
        }
    }

    private async Task Process(Socket socket, Func<Pipe, Task> handler)
    {
        var pipe = await Receive(socket);
        await handler(pipe);
    }

    private async Task<Pipe> Receive(Socket socket)
    {
        var pipe = new Pipe(new PipeOptions(
            pool: MemoryPool<byte>.Shared,
            readerScheduler: scheduler,
            writerScheduler: scheduler,
            pauseWriterThreshold: settings.BufferSize,
            resumeWriterThreshold: settings.BufferSize,
            useSynchronizationContext: false,
            minimumSegmentSize: 4096
        ));

        while (true)
        {
            // Wait for data before allocating a buffer.
            WaitForDataAsync(socket);

            var buffer = (ReadOnlyMemory<byte>)pipe.Writer.GetMemory(settings.BufferSize);
            if (!MemoryMarshal.TryGetArray(buffer, out var result))
                throw new InvalidOperationException("Buffer backed by array was expected");

            _args.SetBuffer(result.Array, result.Offset, result.Count);
            socket.ReceiveAsync(_args);
            if (_args.BytesTransferred == 0) break;

            pipe.Writer.Advance(_args.BytesTransferred);
            await pipe.Writer.FlushAsync();
        }

        return pipe;
    }

    private void WaitForDataAsync(Socket socket)
    {
        _args.SetBuffer([], 0, 0);
        socket.ReceiveAsync(_args);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);

    public void Dispose() => Stop();
}