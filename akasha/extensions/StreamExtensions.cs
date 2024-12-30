using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using akasha.imp;

namespace akasha.extensions;

public static class StreamExtensions
{
    /// <summary>
    /// Read stream and return newline span positions
    /// </summary>
    /// <param name="stream">Data stream</param>
    /// <param name="chunkSize">Initial buffer size</param>
    /// <returns></returns>
    /// <exception cref="FormatException">In case if newline string did not fit fully in buffer</exception>
    public static async IAsyncEnumerable<StreamReadArgs> ByLineAsync(this Stream stream,
        int chunkSize = 4096)
    {
        var args = new StreamReadArgs(chunkSize);
        var isFinished = false;
        var unflushed = false;

        while (args.CanRead && !isFinished)
        {
            isFinished = await args.ReadStream(stream) == 0;
            while (args.CanReadNextChunk())
            {
                args.GetUnreadData(out var data);
                var index = data.IndexOf("\r\n"u8);
                if (index < 0) break;
                
                args.SetChunkCount(index, 2);
                yield return args;
                
                args.MoveToNextChunk();
            }
            
            unflushed = args.SaveUnread();
        }

        if (unflushed)
        {
            yield return args;
        }
    }
}