using System;
using System.Collections.Specialized;

namespace tgv_core.extensions;

public static class UrlExtensions
{
    public static NameValueCollection Query(this Uri uri)
    {
        return System.Web.HttpUtility.ParseQueryString(uri.Query);
    }
}