using Flurl.Http;
using HtmlParserDotNet;
using tgv_common.api;
using tgv_serve_static;
using tgv_watson_server;
using tgv;

namespace tgv_tests;

[TestFixtureSource(typeof(Servers), nameof(Servers.AllServers))]
public class TgvServeStatic
{
    private readonly App _app;

    public TgvServeStatic(Func<ServerHandler, IServer> fn)
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