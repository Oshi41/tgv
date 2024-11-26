using System;
using System.Collections.Generic;
using System.Net;
using JWT;
using JWT.Algorithms;
using JWT.Exceptions;
using JWT.Serializers;
using tgv_auth.api.storage;
using tgv_core.api;
using tgv_core.extensions;

namespace tgv_auth.imp.bearer;

/// <summary>
/// Default JWT token storage
/// </summary>
public class BearerCookieStorage : ICookieStorage<BearerSession>
{
    private readonly string _cookieName;
    private readonly string? _secret;
    private readonly JwtEncoder _encoder;
    private readonly JwtDecoder _decoder;

    public BearerCookieStorage(IJwtAlgorithm algo, string cookieName, string? secret = null)
    {
        _cookieName = cookieName;
        _secret = secret;

        var jsonSerializer = new JsonNetSerializer();
        var base64Encoder = new JwtBase64UrlEncoder();
        _encoder = new JwtEncoder(algo, jsonSerializer, base64Encoder);
        _decoder = new JwtDecoder(jsonSerializer, new JwtValidator(jsonSerializer, new UtcDateTimeProvider()),
            base64Encoder, algo);
    }

    public BearerSession? GetUserSession(Context ctx)
    {
        var cookie = ctx.Cookies[_cookieName];
        if (!cookie.IsValid()) return null;

        try
        {
            var payload = _decoder.DecodeToObject(cookie.Value, _secret);
            if (payload is null) return null;
            return new BearerSession(payload);
        }
        catch (TokenNotYetValidException)
        {
            ctx.Logger.Debug("Token is not valid yet");
        }
        catch (TokenExpiredException)
        {
            ctx.Logger.Debug("Token has expired");
        }
        catch (SignatureVerificationException)
        {
            ctx.Logger.Debug("Token has invalid signature");
        }
        catch (Exception ex)
        {
            ctx.Logger.Error($"Unknown error: {ex}");
        }
        
        return null;
    }

    public Cookie CreateCookie(Context ctx, BearerSession jwtSession)
    {
        var payload = new Dictionary<string, object>
        {
            { "iss", jwtSession.Id },
            { "ref", jwtSession.Refresh },
            { "exp", jwtSession.Expired },
            { "iat", jwtSession.Start }
        };
        
        foreach (var pair in jwtSession)
        {
            payload.Add(pair.Key, pair.Value);
        }
        
        var jwt = _encoder.Encode(payload, _secret);
        return new Cookie(_cookieName, jwt)
        {
            Secure = true,
            HttpOnly = true,
            Expires = jwtSession.Expired,
        };
    }
    
    private T Parse<T>(IDictionary<string, object> claims, string key, Func<string, T?> parser)
    {
        if (!claims.ContainsKey(key))
        {
            throw new Exception($"Claims doesn't contain key {key}");
        }
        
        var value = claims[key];
        if (value is T t) return t;

        try
        {
            t = parser(value.ToString());
            
            if (t is null)
                throw new NullReferenceException(nameof(t));
        }
        catch (Exception ex)
        {
            throw new Exception($"Invalid claims entry: {key}={value}");
        }
        
        claims.Remove(key);
        return t;
    }
}