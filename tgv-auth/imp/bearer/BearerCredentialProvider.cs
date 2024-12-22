using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Net;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using JWT.Serializers;
using tgv_auth.api;
using tgv_auth.extensions;
using tgv_core.api;
using tgv_core.extensions;

namespace tgv_auth.imp.bearer;

public class BearerCredentialProvider: ICredentialProvider<BearerCredentials>
{
    private readonly string _algo;
    private readonly string? _secret;
    private readonly JwtDecoder _decoder;

    public BearerCredentialProvider(IJwtAlgorithm algo, string? secret = null)
    {
        _algo = algo.Name;
        _secret = secret;
        var jsonSerializer = new JsonNetSerializer();
        _decoder = new JwtDecoder(jsonSerializer, new JwtValidator(jsonSerializer, new UtcDateTimeProvider()),
            new JwtBase64UrlEncoder(), algo);
    }
    
    public AuthSchemes Scheme => AuthSchemes.Bearer;
    public BearerCredentials? GetCredentials(Context ctx)
    {
        var auth = ctx.ClientHeaders[HttpRequestHeader.Authorization.ToString()];
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
            
            Metrics.CreateCounter<int>("bearer_credential_token_not_yet_valid", description: "Bearer Token is not valid yet")
                .Add(1, ctx.ToTagsFull().With("algo", _algo));
        }
        catch (TokenExpiredException)
        {
            ctx.Logger.Debug("Token has expired");
            Metrics.CreateCounter<int>("bearer_credential_token_expired", description: "Bearer Token is expired")
                .Add(1, ctx.ToTagsFull().With("algo", _algo));
        }
        catch (SignatureVerificationException)
        {
            ctx.Logger.Debug("Token has invalid signature");
            Metrics.CreateCounter<int>("bearer_credential_token_not_yet_valid", description: "Bearer Token has invalid signtaure")
                .Add(1, ctx.ToTagsFull().With("algo", _algo));
        }
        catch (Exception ex)
        {
            ctx.Logger.Error("Unknown error: {ex}", ex);
            Metrics.CreateCounter<int>("bearer_credential_token_unknown_error", description: "Bearer Token unknonwn error parse")
                .Add(1, ctx.ToTagsFull(ex).With("algo", _algo));
        }

        return null;
    }

    public string GetChallenge(Context ctx, Exception? ex)
    {
        return $"{Scheme.ToHeader()} {ex?.Message ?? ""}";
    }

    public Meter Metrics { get; set; }
}