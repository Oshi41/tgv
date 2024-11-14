using Flurl.Http;
using sharp_express;
using sharp_express.middleware.session;

namespace Tests;

public class SessionMiddleware
{
    private Express _app;

    [SetUp]
    public void Setup()
    {
        _app = new Express();

        _app.Get("/hello", (context, _, _) =>
        {
            context.Text("world");
            return Task.CompletedTask;
        });

        _app.UseSession(new SessionConfig());
        _app.Start(TestUtils.RandPort());
    }

    [TearDown]
    public void Teardown()
    {
        _app?.Stop();
    }

    [Test]
    public async Task Works()
    {
        var resp = await (_app.RunningUrl + "hello")
            .GetStringAsync();
        Assert.That(resp, Is.EqualTo("world"));

        var sessions = await _app.GetSessionStore()!.GetAllSessions();
        Assert.That(sessions.Count(), Is.EqualTo(1));
    }
    
    [Test]
    public async Task ReuseSession()
    {
        var client = FlurlHttp.Clients.GetOrAdd($"{nameof(ReuseSession)}_1", _app.RunningUrl);
        var jar = new CookieJar();

        var resp = await client.Request("hello").WithCookies(jar).GetAsync();
        Assert.That(await resp.GetStringAsync(), Is.EqualTo("world"));
        Assert.That(jar.Count, Is.AtLeast(1));

        var sessions = await _app.GetSessionStore()!.GetAllSessions();
        Assert.That(sessions.Count, Is.EqualTo(1));
        var oldId = sessions.First().Id;

        resp = await client.Request("hello").WithCookies(jar).GetAsync();
        Assert.That(await resp.GetStringAsync(), Is.EqualTo("world"));
        
        sessions = await _app.GetSessionStore()!.GetAllSessions();
        Assert.That(sessions.Count(), Is.EqualTo(1));
        
        Assert.That(sessions.First().Id, Is.EqualTo(oldId));
    }

    [Test]
    public async Task Reuse2Session()
    {
        var client = FlurlHttp.Clients.GetOrAdd($"{nameof(Reuse2Session)}_1", _app.RunningUrl);
        var jar = new CookieJar();
        
        var client2 = FlurlHttp.Clients.GetOrAdd($"{nameof(Reuse2Session)}_2", _app.RunningUrl);
        var jar2 = new CookieJar();

        for (int i = 0; i < 10; i++)
        {
            _ = await client.Request("hello").WithCookies(jar).GetAsync();
            Assert.That(jar.Count, Is.AtLeast(1));
            
            _ = await client2.Request("hello").WithCookies(jar2).GetAsync();
            Assert.That(jar2.Count, Is.AtLeast(1));
        }
        
        var sessions = await _app.GetSessionStore()!.GetAllSessions();
        Assert.That(sessions.Count(), Is.EqualTo(2));
    }
}