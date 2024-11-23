using System.Collections.Specialized;
using Microsoft.AspNetCore.Http;

namespace tgv_kestrel_server.extensions;

public static class Extensions
{
    public static NameValueCollection Convert(this IHeaderDictionary header)
    {
        var result = new NameValueCollection();
        foreach (var pair in header)
            result.Add(pair.Key, pair.Value);
        return result;
    }
    
    public static NameValueCollection Convert(this IQueryCollection query)
    {
        var result = new NameValueCollection();
        foreach (var pair in query)
            result.Add(pair.Key, pair.Value.ToString());
        return result;
    }

    public static Guid ToGuid(long id, DateTime? dateTime = null)
    {
        dateTime ??= DateTime.UtcNow;
        var ticks = dateTime.Value.ToFileTimeUtc();
        var guidData = new byte[16];
        Array.Copy(BitConverter.GetBytes(ticks), guidData, 8);
        Array.Copy(BitConverter.GetBytes(id), 0, guidData, 8, 8);
        return new Guid(guidData);
    }

    public static (DateTime time, long id) FromGuid(this Guid guid)
    {
        var bytes = guid.ToByteArray();
        var ticks = BitConverter.ToInt64(bytes, 0);
        var id = BitConverter.ToInt64(bytes, 8);
        return (DateTime.FromFileTimeUtc(ticks), id);
    }

    public static long NextLong(this Random random, long min = long.MinValue, long max = long.MaxValue)
    {
        var buf = new byte[8];
        random.NextBytes(buf);
         var longRand = BitConverter.ToInt64(buf, 0);
        return (Math.Abs(longRand % (max - min)) + min);
    }
}