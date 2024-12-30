using Flurl.Http;
using tgv_session;
using tgv_session.imp;
using tgv_tests.utils;
using tgv;

namespace tgv_tests.session;

[TestFixtureSource(typeof(Servers), nameof(Servers.AllServers))]
public class SessionTests
{
    private readonly App _app;

    public SessionTests(Servers.ServerCreationCase fn)
    {
        _app = new App(fn);
    }

    [SetUp]
    public async Task Setup()
    {
        _app.Get("/hello", (context, _, _) =>
        {
            context.Text("world");
            return Task.CompletedTask;
        });

        _app.UseSession(new InMemoryStore(), "_test");
        await _app.Start(TestUtils.RandPort());
    }

    [TearDown]
    public void Teardown()
    {
        _app?.Stop();
    }

    [Test]
    public async Task Works()
    {
        var resp = await _app.CreateAgent("1").Request("hello").GetStringAsync();
        Assert.That(resp, Is.EqualTo("world"));

        var sessions = await _app.GetStore()!.ToListAsync();
        Assert.That(sessions.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task ReuseSession()
    {
        var client = _app.CreateAgent("1");
        var jar = new CookieJar();

        var resp = await client.Request("hello").WithCookies(jar).GetAsync();
        Assert.That(await resp.GetStringAsync(), Is.EqualTo("world"));
        Assert.That(jar.Count, Is.AtLeast(1));

        var sessions = await _app.GetStore()!.ToListAsync();
        Assert.That(sessions.Count, Is.EqualTo(1));
        var oldId = sessions.First().Id;

        resp = await client.Request("hello").WithCookies(jar).GetAsync();
        Assert.That(await resp.GetStringAsync(), Is.EqualTo("world"));
        
        sessions = await _app.GetStore()!.ToListAsync();
        Assert.That(sessions.Count(), Is.EqualTo(1));
        
        Assert.That(sessions.First().Id, Is.EqualTo(oldId));
    }

    [Test]
    public async Task Reuse2Session()
    {
        var client = _app.CreateAgent("1");
        var jar = new CookieJar();

        var client2 = _app.CreateAgent("2");
        var jar2 = new CookieJar();

        for (int i = 0; i < 10; i++)
        {
            _ = await client.Request("hello").WithCookies(jar).GetAsync();
            Assert.That(jar.Count, Is.AtLeast(1));

            _ = await client2.Request("hello").WithCookies(jar2).GetAsync();
            Assert.That(jar2.Count, Is.AtLeast(1));
        }

        var sessions = await _app.GetStore()!.ToListAsync();
        Assert.That(sessions.Count(), Is.EqualTo(2));
    }
}