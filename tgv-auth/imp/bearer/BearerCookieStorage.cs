using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
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
    private readonly string _algo;
    private readonly string _cookieName;
    private readonly string? _secret;
    private readonly JwtEncoder _encoder;
    private readonly JwtDecoder _decoder;

    public BearerCookieStorage(IJwtAlgorithm algo, string cookieName, string? secret = null)
    {
        _algo = algo.Name;
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
        if (cookie == null) return null;
        
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
            Statics.Metrics.CreateCounter<int>("bearer_cookie_token_not_yet_valid", description: "Bearer Token is not valid yet")
                .Add(1, ctx.ToTagsFull().With("algo", _algo));
        }
        catch (TokenExpiredException)
        {
            ctx.Logger.Debug("Token has expired");
            Statics.Metrics.CreateCounter<int>("bearer_cookie_token_expired", description: "Bearer Token is expired")
                .Add(1, ctx.ToTagsFull().With("algo", _algo));
        }
        catch (SignatureVerificationException)
        {
            ctx.Logger.Debug("Token has invalid signature");
            Statics.Metrics.CreateCounter<int>("bearer_cookie_token_invalid_signature", description: "Bearer Token has invalid signature")
                .Add(1, ctx.ToTagsFull().With("algo", _algo));
        }
        catch (Exception ex)
        {
            ctx.Logger.Error("Unknown error: {ex}", ex);
            Statics.Metrics.CreateCounter<int>("bearer_cookie_token_unknown_error", description: "Bearer Token unknown error during validation")
                .Add(1, ctx.ToTagsFull().With("algo", _algo));
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
}