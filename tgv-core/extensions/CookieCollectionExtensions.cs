using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;

namespace tgv_core.extensions;

public static class CookieCollectionExtensions
{
    public static void Parse(this CookieCollection cookies, string header)
    {
        if (string.IsNullOrEmpty(header)) return;
        
        foreach (var cookieRaw in header.Replace("\n", "").Replace("\r", "")
                     .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(x => x.Trim()))
        {
            var c = new Cookie();
            foreach (var part in cookieRaw.Split([';'], StringSplitOptions.RemoveEmptyEntries))
            {
                var arr = part.Split('=').Select(x => x.Trim()).ToArray();
                var attribute = arr[0].ToLowerInvariant();
                var value = arr.ElementAtOrDefault(1) ?? "";

                switch (attribute)
                {
                    case "expires" when DateTime.TryParse(value, out var expires):
                        c.Expires = expires;
                        break;

                    case "max-age" when int.TryParse(value, out var maxAge):
                        c.Expires = DateTime.Now.AddSeconds(maxAge);
                        break;

                    case "domain" when value.Length > 0:
                        value = value.ToLowerInvariant();
                        if (value.StartsWith("."))
                            value = value.Substring(1);
                        c.Domain = value;
                        break;

                    case "path" when value.StartsWith("\\"):
                        c.Path = value;
                        break;

                    case "secure":
                        c.Secure = true;
                        break;

                    case "httponly":
                        c.HttpOnly = true;
                        break;

                    default:
                        c.Name = attribute;
                        c.Value = value;
                        break;
                }

                c.Expired = c.Expires != DateTime.MinValue && c.Expires < DateTime.Now;

                if (c.IsValid())
                    cookies.Add(c);
            }
        }
    }

    public static IEnumerable<Cookie> Diff(this CookieCollection left, CookieCollection right)
    {
        return right.OfType<Cookie>().Except(left.OfType<Cookie>())
            .Union(left.OfType<Cookie>().Except(right.OfType<Cookie>()));
    }

    public static int WriteHeaders(this IEnumerable<Cookie> cookies, NameValueCollection headers)
    {
        var list = cookies.ToList();
        if (!list.Any()) return 0;

        var header = string.Join(", ", list.Select(x => string.Join("; ", x.ToParts())));
        if (!string.IsNullOrEmpty(header))
        {
            headers["Set-Cookie"] = header;
        }

        return list.Count;
    }

    public static IEnumerable<string> ToParts(this Cookie cookie)
    {
        yield return $"{cookie.Name}={cookie.Value}";
        
        if (!string.IsNullOrWhiteSpace(cookie.Path))
            yield return $"{nameof(cookie.Path)}={cookie.Path}";
        
        if (!string.IsNullOrWhiteSpace(cookie.Domain))
            yield return $"{nameof(cookie.Domain)}={cookie.Domain}";
        
        if (cookie.Secure)
            yield return "Secure";
        
        if (cookie.HttpOnly)
            yield return "HttpOnly";
        
        if (cookie.Expires != DateTime.MinValue)
            yield return $"{nameof(cookie.Expires)}={cookie.Expires:R}";
    }

    public static bool IsValid(this Cookie? cookie)
    {
        if (cookie == null) return false;
        if (cookie.Expired || cookie.Discard) return false;
        if (cookie.Expires > DateTime.MinValue && cookie.Expires < DateTime.Now) return false;

        return true;
    }
}