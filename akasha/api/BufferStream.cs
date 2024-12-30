using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using akasha.extensions;

namespace akasha.api;

/// <summary>
/// Blocks write if reached max cache limit
/// Straight forward implementation of <see cref="Pipe"/>
/// </summary>
public class BufferStream : Stream
{
    protected readonly List<byte> _buffer;
    // pause waiting source
    private TaskCompletionSource<bool> _waiter = new();

    public BufferStream(int maxCacheSize = 32768)
    {
        _buffer = new (maxCacheSize);
        _waiter.SetResult(true);
    }
    
    #region Stream imp

    public override void Flush()
    {
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = new[] { count, buffer.Length, _buffer.Count }.Min();
        if (read > 0)
        {
            _buffer.CopyTo(0, buffer, offset, read);
            _buffer.RemoveRange(0, read);

            // can continue if at least half of buffer is free
            if (_buffer.Capacity / 2d > buffer.Length)
                Resume();
        }

        return read;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        WriteAsync(buffer, offset, count, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    // called by socket sending data to server
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await CheckPause();
        cancellationToken.ThrowIfCancellationRequested();

        // space is not enough
        while (_buffer.Capacity < _buffer.Count + count)
        {
            SetPaused();
            await CheckPause();
            cancellationToken.ThrowIfCancellationRequested();
        }

        _buffer.AddRange(buffer.AsSpan(offset, count));
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;

    protected override void Dispose(bool disposing)
    {
        _buffer.Clear();
        Resume();
        base.Dispose(disposing);
    }

    #endregion

    #region Not suported

    public override void SetLength(long value) => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    public override long Length => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    #endregion
    
    #region Protected methods

    protected async Task CheckPause()
    {
        while (IsPaused) await _waiter.Task;
    }

    protected void SetPaused()
    {
        var old = Interlocked.Exchange(ref _waiter, new TaskCompletionSource<bool>());
        old.SetResult(true);
    }

    protected void Resume()
    {
        if (IsPaused)
            _waiter.SetResult(true);
    }

    #endregion
    
    /// <summary>
    /// Is stream pause writes <br/>
    /// Means stream waiting for read calls which will partially or fully release buffer space
    /// </summary>
    public bool IsPaused => !_waiter.Task.IsCompleted;
}