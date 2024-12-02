using System.Collections.Specialized;

namespace tgv_server_kestrel;

public static class Extensions
{
    public static NameValueCollection Convert(this IHeaderDictionary headers)
    {
        var collection = new NameValueCollection();
        foreach (var header in headers)
            collection.Add(header.Key, header.Value);
        return collection;
    }

    public static NameValueCollection Convert(this IQueryCollection query)
    {
        var collection = new NameValueCollection();
        foreach (var queryKey in query)
            collection.Add(queryKey.Key, queryKey.Value);
        return collection;
    }
}