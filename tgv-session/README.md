# TGV-Session Middleware

**tgv-session** is a middleware component of the TGV ecosystem,
providing a flexible and powerful session management system for C#.
Designed for simplicity and customizability, it handles user
sessions seamlessly and enables advanced control over session
lifecycle and user claims.

Available on [NuGet](https://www.nuget.org/packages/tgv-session/).

_TGV is a fast, simple, and intuitive HTTP server library for C#. Inspired by ExpressJS,
TGV is designed to make building web applications straightforward,
even for developers with minimal experience._

## Features

- **Session Management**  
  Automatically creates and manages sessions for every user.

- **Customizable**
    - Define your own GUID generation function.
    - Implement custom session storage using the `IStore` interface.
    - Control session lifecycle, including expiration and manual management.

- **User Claims**  
  Manage claims for precise access control and integrate seamlessly with other TGV middleware.

## Usage

```
using tgv_session;

var app = new App();
app.UseSession(new SessionConfig(
  CreateStore,
  GenerateId,
  "_app_cookie",
  TimeSpan.FromMinutes(60)
));

async Task<Guid> GenerateId()
{
    var id = await _dbProvider.GetNewId();
    return id;
}

async Task<IStore> CreateStore()
{
  var dbProvider = new SqlProvider();
  await dbProvider.Init();
  return new SqlStore(dbProvider);
}

class SqlStore : IStore
{
  ...
  
  public Task<SessionContext?> FindAsync(Guid id)
  {
      return _dbProvider.GetSession(id);
  }
  
  public async Task<SessionContext?> FindAsync(Guid id)
  {
      return Convert(await _dbProvider.GetSession(id));
  }
  
  public async Task<SessionContext[]> FindAllAsync()
  {
      return (await _dbProvider.GetAllSession()).Select(Convert).ToArray();
  }
  
  public async Task RemoveAsync(Guid id)
  {
    await _dbProvider.RemoveIfExists(id);
  }
  
  public async Task PutAsync(SessionContext context)
  {
      await _dbProvider.Insert(context);
  }
}
```


