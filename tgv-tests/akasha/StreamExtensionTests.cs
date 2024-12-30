using System.Text;
using akasha.extensions;

namespace tgv_tests.akasha;

public class StreamExtensionTests
{
    [TestCase(10)]
    [TestCase(200)]
    [TestCase(4096)]
    [TestCase(2 << 20)]
    public async Task ByLineAsync_Works(int chunkSize)
    {
        // text size is 851 bytes
        var text = """
                   Lorem ipsum dolor sit amet,
                   consectetur adipiscing elit,
                   sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
                   Ut enim ad minim veniam,


                   quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
                   Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.
                   Excepteur sint occaecat cupidatat non proident,
                   sunt in culpa qui officia deserunt mollit anim id est laborum
                   sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
                   Ut enim ad minim veniam,
                   quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
                   Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.
                   Excepteur sint occaecat cupidatat non proident,
                   sunt in culpa qui officia deserunt mollit anim id est laborum

                   """;

        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes(text));
        stream.Seek(0, SeekOrigin.Begin);

        var lines = text.Split("\r\n");
        var index = 0;
        
        await foreach (var args in stream.ByLineAsync(chunkSize))
        {
            args.GetChunk(out var span);
            var line = span.ToUtf8String();
            AreEquals(lines[index], line);
            index++;
        }
        
        AreEquals(index, lines.Count() - 1);
    }

    [TestCase(10)]
    [TestCase(200)]
    [TestCase(700)]
    public async Task ByLineAsync_StopAndFlush(int size)
    {
        // text size is 851
        var text = """
                   Lorem ipsum dolor sit amet,
                   consectetur adipiscing elit,
                   sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
                   Ut enim ad minim veniam,


                   quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
                   Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.
                   Excepteur sint occaecat cupidatat non proident,
                   sunt in culpa qui officia deserunt mollit anim id est laborum
                   sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
                   Ut enim ad minim veniam,
                   quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
                   Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.
                   Excepteur sint occaecat cupidatat non proident,
                   sunt in culpa qui officia deserunt mollit anim id est laborum

                   """;
        
        using var lStream = new MemoryStream();
        lStream.Write(Encoding.UTF8.GetBytes(text));
        lStream.Seek(0, SeekOrigin.Begin);
        
        using var rStream = new MemoryStream();
        var iterations = 0;

        await foreach (var e in lStream.ByLineAsync(size))
        {
            e.GetChunk(out var span);
            rStream.Write(span);
            rStream.Write("\r\n"u8);
            var (buffer, offset, count) = e.StopAndFlush();
            rStream.Write(buffer, offset, count);
            iterations++;
        }
        
        // called only once
        AreEquals(iterations, 1);

        rStream.Seek(0, SeekOrigin.Begin);
        var receivedTest = await new StreamReader(rStream).ReadToEndAsync();
        Assert.That(receivedTest, Is.Not.Empty);
        AreEquals(true, text.StartsWith(receivedTest));
        AreNotEquals(receivedTest, text);
    }
}