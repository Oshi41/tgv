using System;
using System.Collections.Generic;
using JWT.Builder;
using tgv_auth.api;

namespace tgv_auth.imp.bearer;

public class BearerCredentials: ICredentials
{
    private readonly JwtHeader _header;
    public IDictionary<string, object> Claims { get; }

    public AuthSchemes Scheme => AuthSchemes.Bearer;

    public BearerCredentials(JwtHeader header, IDictionary<string, object> claims)
    {
        _header = header;
        Claims = claims;
        
        if (!claims.TryGetValue("iss", out var iss))
            throw new Exception($"Every JWT token must contain a 'iss' claim.");
    }

    protected bool Equals(BearerCredentials other) => Claims.Equals(other.Claims);

    public bool Equals(ICredentials other) => Equals((object)other);
    
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((BearerCredentials)obj);
    }

    public override int GetHashCode()
    {
        return Claims.Count;
    }
}