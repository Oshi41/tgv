using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using Flurl.Http;
using tgv_auth.api;
using tgv_auth.api.storage;
using tgv_auth.extensions;
using tgv_auth.imp.basic;
using tgv_core.imp;
using tgv;

namespace tgv_tests;

class TestStorage : ISessionStorage<BasicCredentials, BasicSession>
{
    private readonly BasicCredentials[] _users;
    private readonly Dictionary<string, BasicSession> _active = new();

    public TestStorage(params BasicCredentials[] users)
    {
        _users = users;
    }

    public async Task<BasicSession?> Refresh(BasicSession prev)
    {
        if (!_active.TryGetValue(prev.Name, out var original))
        {
            throw new KeyNotFoundException();
        }

        if (prev.Name != original.Name) throw new Exception("Name mismatch");

        await Logout(prev);

        var session = new BasicSession(DateTime.Now, DateTime.Now.Add(TimeSpan.FromHours(1)), prev.Name);
        _active[original.Name] = session;
        return session;
    }

    public async Task<BasicSession?> Login(BasicCredentials credentials)
    {
        if (!_users.Contains(credentials)) return null;

        await Task.WhenAll(_active.Values
            .Where(x => x.Name == credentials.Username)
            .Select(Logout)
            .ToArray());

        var session = new BasicSession(DateTime.Now.AddHours(1), DateTime.Now, credentials.Username);
        _active.Add(session.Name, session);
        return session;
    }

    public Task Logout(BasicSession session)
    {
        _active.Remove(session.Name);
        return Task.CompletedTask;
    }

    public async Task<SessionStatus> GetStatus(BasicSession session)
    {
        if (!_active.TryGetValue(session.Name, out var status)) return SessionStatus.NotFound;

        if (!session.IsValid()) return SessionStatus.Expired;
        return SessionStatus.Active;
    }

    public Task<List<BasicSession>> GetSessions()
    {
        return Task.FromResult(_active.Values.ToList());
    }
}

[TestFixtureSource(typeof(Servers), nameof(Servers.AllServers))]
public class TgvAuth
{
    private readonly App _app;

    public TgvAuth(Servers.ServerCreationCase fn)
    {
        _app = new App(fn);
        _app.Get("/everyone", (ctx, next, exception) => ctx.Text("Hello!"));

        var router = new Router("/authorized");
        router.UseAuth(
            new BasicCredentialProvider(),
            new TestStorage(
                new("user", "pass"),
                new("admin", "pass")
            ),
            new BasicCookieStorage("_test_cookie")
            );
        _app.Use(router);

        router.Get("/any", async (ctx, next, _) =>
            {
                if (await ctx.Auth<BasicSession>() is { } session && session.IsValid())
                {
                    next();
                    return;
                }

                await ctx.Send(HttpStatusCode.Unauthorized);
            },
            (ctx, next, exception) => ctx.Text("Hello!"));

        router.Get("/admin", async (ctx, next, _) =>
            {
                if (await ctx.Auth<BasicSession>() is { } session && session.IsValid() && session.Name == "admin")
                {
                    next();
                    return;
                }

                await ctx.Send(HttpStatusCode.Unauthorized);
            },
            (ctx, next, exception) => ctx.Text("Hello!"));
    }

    private string ToHeader(BasicCredentials credentials)
    {
        var text = $"{credentials.Username}:{credentials.Password}";
        var bytes = Encoding.UTF8.GetBytes(text);
        var base64 = Convert.ToBase64String(bytes);
        return $"Basic {base64}";
    }

    [SetUp]
    public async Task Startup()
    {
        await _app.Start(TestUtils.RandPort());
    }

    [TearDown]
    public void Teardown()
    {
        _app.Stop();
    }

    [Test]
    public async Task Works()
    {
        var client = _app.CreateAgent(nameof(Works));

        var cases = new Dictionary<BasicCredentials, Dictionary<string, string>>
        {
            {
                new BasicCredentials("user", "pass"), new Dictionary<string, string>
                {
                    { "everyone", "2xx" },
                    { "authorized/any", "2xx" },
                    { "authorized/admin", "4xx" },
                }
            },

            {
                new BasicCredentials("admin", "pass"), new Dictionary<string, string>
                {
                    { "everyone", "2xx" },
                    { "authorized/any", "2xx" },
                    { "authorized/admin", "2xx" },
                }
            },
            
            {
                new BasicCredentials("unknown", "unknown"), new Dictionary<string, string>
                {
                    { "everyone", "2xx" },
                    { "authorized/any", "4xx" },
                    { "authorized/admin", "4xx" },
                }
            },

            {
                new BasicCredentials("user", "unknown"), new Dictionary<string, string>
                {
                    { "everyone", "2xx" },
                    { "authorized/any", "4xx" },
                    { "authorized/admin", "4xx" },
                }
            },

            {
                new BasicCredentials("admin", "unknown"), new Dictionary<string, string>
                {
                    { "everyone", "2xx" },
                    { "authorized/any", "4xx" },
                    { "authorized/admin", "4xx" },
                }
            },
            
        };

        foreach (var (credentials, requests) in cases)
        {
            foreach (var (endpoint, httpMask) in requests)
            {
                var resp = await client.Request(endpoint)
                    .WithHeader("Authorization", ToHeader(credentials))
                    .AllowHttpStatus(httpMask)
                    .GetAsync();

                if (httpMask.StartsWith("2"))
                {
                    Assert.That(await resp.GetStringAsync(), Is.EqualTo("Hello!"),
                        $"[{credentials.Username}:{credentials.Password}][{endpoint}] Expected 'Hello!' but got '{await resp.GetStringAsync()}'");
                }
            }
        }
    }
}