using System.Collections.Specialized;
using System.Net;

namespace tgv.extensions;

public static class CookieCollectionExtensions
{
    public static void Parse(this CookieCollection cookies, string header)
    {
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

                if (!c.Expired)
                    cookies.Add(c);
            }
        }
    }

    public static void WriteHeaders(this CookieCollection cookies, NameValueCollection headers)
    {
        if (cookies.Count < 1) return;
        
        headers["Set-Cookie"] = string.Join(", ", cookies.OfType<Cookie>().Select(x => x.ToString()));
    }
}