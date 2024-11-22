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
}