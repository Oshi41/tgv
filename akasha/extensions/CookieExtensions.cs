using System;
using System.Net;
using System.Text;

namespace akasha.extensions;

public static class CookieExtensions
{
    public static string ToHttpHeader(this Cookie cookie)
    {
        var sb = new StringBuilder();
        sb.Append($"{cookie.Name}={cookie.Value}");
        
        if (cookie.HttpOnly) sb.Append("; HttpOnly");
        if (cookie.Secure) sb.Append("; Secure"); 
        if (cookie.Expires > DateTime.Now) sb.Append($"; Expires={cookie.Expires:R}"); 
        if (!string.IsNullOrEmpty(cookie.Path)) sb.Append($"; Path={cookie.Path}");
        if (!string.IsNullOrEmpty(cookie.Domain)) sb.Append($"; Path={cookie.Domain}");
        
        return sb.ToString();
    }
}