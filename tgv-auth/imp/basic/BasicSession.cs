using System;
using tgv_auth.api;

namespace tgv_auth.imp.basic;

public class BasicSession : IUserSession
{
    public BasicSession(DateTime expired, DateTime start, string name)
        : base(expired, start)
    {
        Name = name;
    }

    public string Name { get;  }
}