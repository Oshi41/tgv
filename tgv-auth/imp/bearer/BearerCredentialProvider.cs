using System;
using System.Collections.Generic;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using JWT.Serializers;
using tgv_auth.api;
using tgv_auth.extensions;
using tgv_core.api;

namespace tgv_auth.imp.bearer;

public class BearerCredentialProvider: ICredentialProvider<BearerCredentials>
{
    private readonly string? _secret;
    private readonly JwtDecoder _decoder;

    public BearerCredentialProvider(IJwtAlgorithm algo, string? secret = null)
    {
        _secret = secret;
        var jsonSerializer = new JsonNetSerializer();
        _decoder = new JwtDecoder(jsonSerializer, new JwtValidator(jsonSerializer, new UtcDateTimeProvider()),
            new JwtBase64UrlEncoder(), algo);
    }
    
    public AuthSchemes Scheme => AuthSchemes.Bearer;
    public BearerCredentials? GetCredentials(Context ctx)
    {
        var auth = ctx.ClientHeaders["Authorization"];
        if (auth?.StartsWith(Scheme.ToString()) != true) return null;
        
        var token = auth.Replace($"{Scheme.ToHeader()} ", "");
        
        try
        {
            var jwtHeader = _decoder.DecodeHeader<JwtHeader>(token);
            var payload = _decoder.DecodeToObject(token, _secret);
            return new BearerCredentials(jwtHeader, payload ?? new Dictionary<string, object>());
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

    public string GetChallenge(Context ctx, Exception? ex)
    {
        return $"{Scheme.ToHeader()} {ex?.Message ?? ""}";
    }
}