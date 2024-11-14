using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Flurl.Http;
using Moq;
using tgv;
using tgv.core;

namespace tgv_tests;

public static class TestUtils
{
    public static IContext Create(string path, string method = "GET")
    {
        var ctx = new Mock<IContext>();
        var q = new NameValueCollection();
        if (!path.StartsWith("/"))
            path = "/" + path;
        var uri = new Uri($"http://localhost{path}");
        var history = new Stack<IMatch>();
        if (uri.Query.Length > 1)
        {
            foreach (var arr in uri.Query[1..].Split('&', StringSplitOptions.RemoveEmptyEntries)
                         .Select(x => x.Split('=')))
            {
                q[arr[0]] = arr[1];
            }
        }

        ctx.Setup(x => x.Url).Returns(uri);
        ctx.Setup(x => x.Query).Returns(q);
        ctx.Setup(x => x.HttpMethod).Returns(method);
        ctx.Object.Stage = HandleStages.Handle;
        return ctx.Object;
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
}