using System.Net;
using System.Runtime.CompilerServices;
using Flurl.Http;
using HtmlParserDotNet;
using tgv_core.api;
using tgv_core.imp;
using tgv;
using HttpMethod = System.Net.Http.HttpMethod;

namespace tgv_tests;

class TestContext : Context
{
    private bool _wasSent = false;

    public TestContext(HttpMethod method, Uri url)
        : base(method, Guid.NewGuid(), url, new Logger(), new(), new(), new())
    {
        Stage = method;
    }

    public override bool WasSent => _wasSent;

    public override Task<string> Body()
    {
        return Task.FromResult(string.Empty);
    }

    public override Task Redirect(string path, HttpStatusCode code = HttpStatusCode.Moved)
    {
        return Task.CompletedTask;
    }

    protected override Task SendRaw(byte[]? bytes, HttpStatusCode code, string? contentType)
    {
        return AfterSending();
    }

    protected override Task SendRaw(Stream stream, HttpStatusCode code, string contentType)
    {
        return AfterSending();
    }
}

public static class TestUtils
{
    public static Context Create(string path, HttpMethod? method = null)
    {
        return new TestContext(method ?? HttpMethod.Get, new Uri("http://localhost:80" + path));
    }

    public static bool IsSuccess(int code)
    {
        return code - code % 100 == 200;
    }

    public static int RandPort()
    {
        return 5000 + new Random().Next(500) + (int)(DateTime.Now.ToFileTimeUtc()) % 200;
    }

    public static IFlurlClient CreateAgent(this App app, string prefix, [CallerMemberName] string method = "")
    {
        return FlurlHttp.Clients.GetOrAdd($"{method}_{prefix}", app.RunningUrl);
    }

    public static async Task<HtmlElement> GetHtmlAsync(this IFlurlRequest request)
    {
        var txt = await request.GetStringAsync();
        var html = HtmlParser.LoadFromHtmlString(txt);
        return html.DocumentElement;
    }
}