using System.Collections.Specialized;
using Moq;
using sharp_express.core;

namespace Tests;

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
        ctx.Setup(x => x.CurrentPath).Returns(history);
        ctx.SetupProperty(x => x.Parameters);
        ctx.SetupProperty(x => x.Stage);
        ctx.Object.Stage = HandleStages.Handle;
        return ctx.Object;
    }

    public static bool IsSuccess(int code)
    {
        return code - code % 100 == 200;
    }
}