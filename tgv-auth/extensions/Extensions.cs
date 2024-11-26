using System;
using System.Collections.Generic;
using tgv_auth.api;

namespace tgv_auth.extensions;

public static class Extension
{
    public static string ToHeader(this AuthSchemes scheme) => scheme switch
    {
        AuthSchemes.Basic => "Basic",
        AuthSchemes.Bearer => "Bearer",
        AuthSchemes.Digest => "Digest",
        AuthSchemes.Hoba => "HOBA",
        AuthSchemes.Mutal => "Mutal",
        AuthSchemes.Negotiate or AuthSchemes.Ntlm => "Negotiate",
        AuthSchemes.Vapid => "vapid",
        AuthSchemes.Scram => "SCRAM-SHA-256",
        AuthSchemes.Aws => "AWS4-HMAC-SHA256",
        
        _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null)
    };

    public static T Parse<T>(this IDictionary<string, object> claims, string name, Func<object, T> convert)
    {
        if (!claims.TryGetValue(name, out var value))
            throw new KeyNotFoundException(name);

        if (value is not T result)
        {
            result = convert(value);
        }

        claims.Remove(name);
        return result;
    }
}