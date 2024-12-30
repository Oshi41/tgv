using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using akasha.extensions;

namespace akasha.imp;

public class StreamReadArgs
{
    /// <summary>
    /// current array data
    /// </summary>
    private byte[] _data;

    /// <summary>
    /// curent read position start
    /// </summary>
    private int _cursor = 0;

    /// <summary>
    /// curent read position index
    /// </summary>
    private int _cursorCount = 0;

    /// <summary>
    /// last pending data byte (0 -> _pending)
    /// </summary>
    private int _pending = 0;

    /// <summary>
    /// total buff meaningful data (0 -> _count)
    /// </summary>
    private int _count = 0;

    private int _nextUnread = 0;

    public StreamReadArgs(int size)
    {
        _data = new byte[size];
    }

    #region Public

    /// <summary>
    /// Reads current chunk data
    /// </summary>
    /// <param name="span"></param>
    public void GetChunk(out Span<byte> span)
    {
        span = CheckRange(_cursor, _cursorCount)
            ? _data.AsSpan(_cursor, _cursorCount)
            : Span<byte>.Empty;
    }

    /// <summary>
    /// Can stream read data
    /// </summary>
    public bool CanRead { get; private set; } = true;

    /// <summary>
    /// Force stream to stop reading from socket.
    /// Flusing all the pending data as the last call
    /// </summary>
    public (byte[] buffer, int offset, int count) StopAndFlush()
    {
        CanRead = false;

        // set current chunk as read
        MoveToNextChunk();

        var offset = _cursor;
        var count = _count - _cursor;

        if (!CheckRange(offset, count)) return ([], 0, 0);

        var line = _data.AsSpan(offset, count).ToUtf8String();
        
        // read to end
        _cursor = _count;
        _nextUnread = _count;
        return (_data, offset, count);

    }

    #endregion

    #region Internal

    /// <summary>
    /// Read steram data and set inner state.
    /// </summary>
    /// <param name="stream">Data stream</param>
    internal async Task<int> ReadStream(Stream stream)
    {
        var read = await stream.ReadAsync(_data, _pending, _data.Length - _pending);
        _cursorCount = _count = read + _pending;
        _nextUnread = _cursor = _pending = 0;
        return read;
    }

    /// <summary>
    /// Can we process next chunk inside received stream data?
    /// </summary>
    /// <returns></returns>
    internal bool CanReadNextChunk() => CanRead
                                        && CheckRange(_cursor, _cursorCount);

    /// <summary>
    /// Returns span with all yet unprocessed stream data
    /// </summary>
    /// <param name="span">span</param>
    internal void GetUnreadData(out Span<byte> span)
    {
        span = CheckRange(_cursor, _count - _cursor)
            ? _data.AsSpan(_cursor, _count - _cursor)
            : Span<byte>.Empty;
    }

    /// <summary>
    /// Set current chunk count. Also is needed to set next unread byte index <br/>
    /// Easy using with <see cref="GetUnreadData"/> (see example below)
    /// Affects <see cref="GetChunk"/>
    /// </summary>
    /// <example>
    ///  In case of new line splitter you can use
    ///<code>
    /// args.GetUnreadData(out var span);
    /// var index = span.IndexOf("\r\n"u8);
    /// if (index >= 0)
    /// {
    ///     // set chunk size here - first occurance of newline
    ///     // and next unread index will be 2 bytes away from chunk end (newline skipping)
    ///     args.SetChunkCount(index, 2);
    /// //
    ///     // some user code here
    ///     UserProcessing(args);
    /// //
    ///     // move to next meaningful unprocessed data
    ///     args.MoveNext(); 
    /// }
    /// </code>
    /// </example>
    /// <param name="count"></param>
    internal void SetChunkCount(int count, int nextStartAdjustment = 0)
    {
        _cursorCount = count;
        _nextUnread = _cursor + _cursorCount + nextStartAdjustment;
    }

    /// <summary>
    /// Moving cursor to first unread position. <br/>
    /// Class contains own cursor state and move it after chunk was read. 
    /// </summary>
    internal void MoveToNextChunk()
    {
        _cursor = _nextUnread;
        _cursorCount = _count - _cursor;
        _nextUnread = _count;
    }

    internal bool SaveUnread()
    {
        var span = CheckRange(_cursor, _count - _cursor)
            ? _data.AsSpan(_cursor, _count - _cursor)
            : Span<byte>.Empty;
        if (span.IsEmpty)
        {
            _nextUnread = _cursorCount = _cursor = -1;
            return false;
        }

        _nextUnread = _cursor = 0;
        _cursorCount = span.Length;

        // resizing buffer if 60% of storage is taking 
        if (span.Length > _data.Length * 0.6)
        {
            Array.Resize(ref _data, _data.Length * 2);
        }

        // copy to the array start
        span.CopyTo(_data);

        _pending = span.Length;
        return true;
    }

    private bool CheckRange(int start, int count)
    {
        return start >= 0
               && start < _data.Length
               && start < _count
               && start + count <= _count
               && start + count <= _data.Length;
    }

    #endregion
}