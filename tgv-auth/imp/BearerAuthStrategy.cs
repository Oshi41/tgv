using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using JWT.Serializers;
using tgv_common.api;
using tgv_common.imp;


namespace tgv_auth.imp;

public record JwToken(JwtHeader Header, IDictionary<string, object>? Claims);

public class BearerAuthStrategy : AuthStrategyBase<JwToken>
{
    private readonly JwtEncoder _encoder;
    private readonly JwtDecoder _decoder;
    private readonly string? _secret;
    
    public BearerAuthStrategy(IAsymmetricAlgorithm algo, IStore<JwToken> store, Logger logger,
        string? secret = null)
        : base(store, logger)
    {
        var jsonSerializer = new JsonNetSerializer();
        var base64Encoder = new JwtBase64UrlEncoder();
        _secret = secret;
        
        _encoder = new JwtEncoder(algo, jsonSerializer, base64Encoder);
        _decoder = new JwtDecoder(jsonSerializer, new JwtValidator(jsonSerializer, new UtcDateTimeProvider()),
            base64Encoder, algo);
    }

    protected override JwToken? GetCredentials(string header)
    {
        var token = header.Replace(Scheme + " ", string.Empty);
        try
        {
            var jwtHeader = _decoder.DecodeHeader<JwtHeader>(token);
            var payload = _decoder.DecodeToObject(token, _secret);
            return new JwToken(jwtHeader, payload);

        }
        catch (TokenNotYetValidException)
        {
            _logger.Debug("Token is not valid yet");
        }
        catch (TokenExpiredException)
        {
            _logger.Debug("Token has expired");
        }
        catch (SignatureVerificationException)
        {
            _logger.Debug("Token has invalid signature");
        }
        catch (Exception ex)
        {
            _logger.Error($"Unknown error: {ex}");
        }

        return null;
    }

    protected override string GetUniqueId(JwToken credentials)
    {
        return credentials.Header.KeyId;
    }

    public override string Scheme => "Bearer";
    public override string Challenge(Context ctx)
    {
        return Scheme;
    }

    public override string? ToHeader(ClaimsIdentity identity)
    {
        var payload = identity.Claims.ToDictionary(x => x.Type, y => y.Value);
        var jwt = _encoder.Encode(payload, _secret);
        return $"{Scheme} {jwt}";
    }
}