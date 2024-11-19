using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace tgv_auth.imp;

public class ClaimsIdentityImp : ClaimsIdentity
{
    public ClaimsIdentityImp(string scheme, string name, string? role, Dictionary<string, object>? claims)
        : base(scheme, name, role)
    {
        if (claims != null)
        {
            foreach (var pair in claims)
            {
                AddClaim(new Claim(
                    pair.Key,
                    pair.Value.ToString(),
                    pair.Value switch
                    {
                        int _ => ClaimValueTypes.Integer,
                        double _ => ClaimValueTypes.Double,
                        bool _ => ClaimValueTypes.Boolean,
                        DateTime _ => ClaimValueTypes.DateTime,
                        _ => ClaimValueTypes.String,
                    },
                    name,
                    name,
                    this));
            }
        }

        IsDirty = false;
    }

    public bool IsDirty { get; set; }
    
    public override void AddClaim(Claim claim)
    {
        base.AddClaim(claim);
        IsDirty = true;
    }

    public override void AddClaims(IEnumerable<Claim> claims)
    {
        foreach (var claim in claims)
        {
            AddClaim(claim);
        }
    }

    public override void RemoveClaim(Claim claim)
    {
        base.RemoveClaim(claim);
        IsDirty = true;
    }
}