using System;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using tgv_auth.api;
using tgv_auth.extensions;
using tgv_core.api;
using tgv_core.extensions;

namespace tgv_auth.imp.basic;

public class BasicCredentialProvider : ICredentialProvider<BasicCredentials>
{
    private readonly Encoding _encoding = Encoding.UTF8;
    public AuthSchemes Scheme => AuthSchemes.Basic;
    
    public BasicCredentials? GetCredentials(Context ctx)
    {
        // First priority - URL
        if (ctx.Url?.UserInfo?.Contains(":") == true)
        {
            var arr = ctx.Url.UserInfo.Split([':']);
            if (arr.Length == 2)
            {
                Metrics.CreateCounter<int>("basic_cookie_provider_url", description: "Basic auth was resolved from URL")
                    .Add(1, ctx.ToTagsFull());
                return new BasicCredentials(arr[0], arr[1]);
            }
        }

        // Second priority - header
        var result = Parse(ctx.ClientHeaders[HttpRequestHeader.Authorization.ToString()]);

        if (result == null) return null;
        
        Metrics.CreateCounter<int>("basic_cookie_provider_cookie", description: "Basic auth was resolved from cookie")
            .Add(1, ctx.ToTagsFull());
        return result;
    }
    
    private BasicCredentials? Parse(string? header)
    {
        if (header?.StartsWith(Scheme.ToHeader()) != true) return null;
        
        var base64 = header.Replace($"{Scheme.ToHeader()} ", "");
        var bytes = Convert.FromBase64String(base64);
        var decoded = _encoding.GetString(bytes);
        var tokens = decoded.Split(':');
        if (tokens.Length == 2)
            return new BasicCredentials(tokens[0], tokens[1]);
        
        return null;
    }

    public string GetChallenge(Context ctx, Exception? ex)
    {
        return $"{Scheme.ToHeader()} charset={_encoding.WebName} {ex?.Message ?? ""}";
    }

    public Meter Metrics { get; set; }
}