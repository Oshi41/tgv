# Auth Middleware

The Auth Middleware component provides a flexible
authentication layer for your web applications.
It supports multiple authentication schemes,
such as Bearer and Basic, and integrates seamlessly
with your project to handle user sessions and cookies.

## Features

- **Supports Multiple Auth Schemes**: Easily extendable to handle different types of authentication, including Bearer and Basic.
- **Credential Providers**: Integrate with custom or existing credential providers.
- **Cookie Management**: Securely create and parse cookies to maintain user sessions.

## Getting Started

### Configuration

1. **Initialize the Middleware**: Add the middleware to your HTTP request processing pipeline.

2. **Configure Credential Providers**: Register your credential providers (e.g., BearerCredentialProvider, BasicCredentialProvider).

3. **Setup Session Storage**: Implement the `ISessionStorage<,>` interface to handle session operations such as login and refresh.

### Basic Usage

First of all, you need to implement your custom credential storage.
Below is the example of implementation of SQL-based Basic session storage

```csharp
public class SqlCessionStorage : ISessionStorage<BasicCredentials, BasicSession>
{
    private readonly SqlSession _session;
    
    public async Task<BasicSession?> Login(BasicCredentials credentials)
    {
        var records = await _session.Select("name FROM USERS WHERE name=$1 AND password=$2",
            credentials.Username, credentials.Password);
        
        if (!records.Any()) throw new Exception("Wrong name/password");
        
        var now = DateTime.Now;
        var expired = now + TimeSpan.FromHours(1);
        await _session.Insert("sessions (name, start, expired) VALUES ($1, $2, $3)",
            records[0], now, expired));
        
        return new BasicSession(
            now + TimeSpan.FromHours(1),
            now,
            records[0]
        );
    }
    
    public async Task<SessionStatus> GetStatus(BasicSession session)
    {
        var records = await _session.Select("* FROM sessions WHERE name=$1",
            session.Username));
        
        if (records.Count() != 1) return SessionStatus.NotFound;
        
        return records[0].expired < DateTime.Now
            ? SessionStatus.Expired
            : SessionStatus.Active;
    }
    
    public async Task Logout(BasicSession session)
    {
        await _session.Delete("sessions WHERE name=$1 AND start=$2",
            session.Username, session.Start);
    }
}
```

```csharp

SqlSession session;
...
var provider = new BasicCredentialProvider();
var storage = new SqlCessionStorage(session);
var cookieStorage = new BasicCookieStorage("cookie_name");

var middleware = AuthMiddleware.UseAuth(provider, storage, session);
app.Use(middleware);
```

### Extending Functionality

To support additional authentication schemes:
- Implement `ICredentialProvider<T>` for new credential types.
- Implement `ISessionStorage<TCreds, TSession>` for managing sessions.
- Implement `ICookieStorage<T>` if cookies are required.

For more information, see tgv-tests