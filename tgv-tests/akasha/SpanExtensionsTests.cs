using akasha.extensions;

namespace tgv_tests.akasha;

public class SpanExtensionsTests
{
    void TestSpan(IList<byte> source, Span<byte> span)
    {
        AreEquals(source.Count, span.Length, "source.Length == span.Length");
            
        for (var i = 0; i < source.Count; i++)
        {
            AreEquals(source[i], span[i], "source[i] = span[i]");
            var slice = span.Slice(i, 1);
            AreEquals(source[i], slice[0], "source[i] = slice[0]");

            // surely changing byte
            slice[0] += 1;
            TestContext.Out.WriteLine("span={0}, slice={1}, source={2}", span[i], slice[0], source[i]);
                
            AreEquals(slice[0], span[i], "(updated) source[i] = slice[0]");
            AreEquals(slice[0], (byte)(source[i] + 1), "(updated) source[i] = span[i]");
        }
    }
    
    [TestCase]
    public void List_AsSpan()
    {
        var originalSource = new byte[4096];
        var rand  = new Random();
        rand.NextBytes(originalSource);
        
        var list = new List<byte>(originalSource);
        TestSpan(originalSource, list.AsSpan());
    }
}