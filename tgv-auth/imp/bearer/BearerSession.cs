using System;
using System.Collections.Generic;
using tgv_auth.api;
using tgv_auth.extensions;

namespace tgv_auth.imp.bearer;

public class BearerSession : IUserSession
{
    public BearerSession(IDictionary<string, object> claims)
        : base(
            claims.Parse("exp", x => DateTime.Parse(x.ToString())),
            claims.Parse("iat", x => DateTime.Parse(x.ToString())))
    {
        Id = claims.Parse("jti", x => Guid.Parse(x.ToString()));
        Owner = claims.Parse("iss", x => x.ToString());
        Refresh = claims.Parse("red", x => Guid.Parse(x.ToString()));
    }

    public Guid Refresh { get;  }

    public string Owner { get; }

    public Guid Id { get; }
    public override string ToString()
    {
        return Id.ToString();
    }
}