using Flurl.Http;
using HtmlParserDotNet;
using tgv_serve_static;
using tgv_tests.utils;
using tgv;

namespace tgv_tests.serve_static;

[TestFixtureSource(typeof(Servers), nameof(Servers.AllServers))]
public class ServeStaticTests
{
    private readonly App _app;

    public ServeStaticTests(Servers.ServerCreationCase fn)
    {
        _app = new App(fn);
    }

    [SetUp]
    public async Task Setup()
    {
        _app.ServeStatic(new StaticFilesConfig(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "static")))
        {
            FallThrough = true,
        });

        _app.Get("/hello", (context, _, _) =>
        {
            context.Text("world");
            return Task.CompletedTask;
        });

        await _app.Start(TestUtils.RandPort());
    }

    [TearDown]
    public void TearDown()
    {
        _app?.Stop();
    }

    [Test]
    public async Task Works()
    {
        var resp = await _app.CreateAgent(nameof(Works))
            .Request("")
            .AllowHttpStatus("2xx")
            .GetHtmlAsync();

        Assert.That(resp.GetElementsByTagName("title").First().InnerHTML,
            Is.EqualTo("Hello World!"));
    }
}