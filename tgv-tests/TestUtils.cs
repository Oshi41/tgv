using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Flurl.Http;
using HtmlParserDotNet;
using tgv;
using tgv.core;
using tgv.imp;
using Timestamps;
using WatsonWebserver.Core;
using WatsonWebserver.Lite;
using HttpMethod = System.Net.Http.HttpMethod;

namespace tgv_tests;

public static class TestUtils
{
    public static Context Create(string path, HttpMethod? method = null)
    {
        var now = new Timestamp();
        method ??= HttpMethod.Get;
        
        var context = new HttpContext
        {
            Guid = Guid.NewGuid(),
            Request = new HttpRequest($"127.0.0.1:80", new MemoryStream(),
                $"{method.Method} {path} HTTP/1.1"),
            Timestamp = now,
            Response = new HttpResponse
            {
                StatusCode = 200,
                Timestamp = now,
                Headers = new NameValueCollection(),
                ContentType = "text/plain",
                ResponseSent = false,
                ChunkedTransfer = false,
            },
            RouteType = RouteTypeEnum.Default,
        };
        
        var result = new Context(context, new Logger())
        {
            Stage = method
        };
        return result;
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