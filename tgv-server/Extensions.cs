using System;
using System.Collections.Specialized;
using System.Net;

namespace tgv_server;

public static class Extensions
{
    public static NameValueCollection Query(this Uri uri)
    {
        var result = new NameValueCollection();
        var str = uri.Query;
        if (!string.IsNullOrEmpty(str))
        {
            foreach (var pair in str.Substring(1).Split('&'))
            {
                var arr = pair.Split('=');
                if (arr.Length == 2 && !string.IsNullOrEmpty(arr[0]))
                {
                    result.Add(arr[0], arr[1]);
                }
            }
        }

        return result;
    }
}