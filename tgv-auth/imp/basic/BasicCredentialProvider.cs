using System;
using System.Net;
using System.Text;
using tgv_auth.api;
using tgv_auth.extensions;
using tgv_core.api;

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
            if (arr.Length == 2) return new BasicCredentials(arr[0], arr[1]);
        }

        // Second priority - header
        return Parse(ctx.ClientHeaders[HttpRequestHeader.Authorization.ToString()]);
    }
    
    private BasicCredentials? Parse(string? header)
    {
        if (header?.StartsWith(Scheme.ToHeader()) != true) return null;
        
        var base64 = header.Replace($"{Scheme.ToHeader()} ", "");
        var bytes = Convert.FromBase64String(base64);
        var decoded = _encoding.GetString(bytes);
        var tokens = decoded.Split(':');
        if (tokens.Length == 2) return new BasicCredentials(tokens[0], tokens[1]);
        
        return null;
    }

    public string GetChallenge(Context ctx, Exception? ex)
    {
        return $"{Scheme.ToHeader()} charset={_encoding.WebName} {ex?.Message ?? ""}";
    }
}