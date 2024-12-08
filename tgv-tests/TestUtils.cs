using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

public class Details
{
    public int Age { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
}

public class User
{
    public string Id { get; set; }
    public string FullName => $"{Details?.Name} {Details?.Surname}";
    public Details Details { get; set; }
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

    public static IFlurlClient CreateAgent(this App app, string prefix, [CallerMemberName] string method = "", [CallerFilePath] string file = "")
    {
        return FlurlHttp.Clients.GetOrAdd($"{file}_{method}_{prefix}", app.RunningUrl);
    }

    public static async Task<HtmlElement> GetHtmlAsync(this IFlurlRequest request)
    {
        var txt = await request.GetStringAsync();
        var html = HtmlParser.LoadFromHtmlString(txt);
        return html.DocumentElement;
    }
    
    public static X509Certificate2 MakeDebugCert()
    {
        var ecdsa = ECDsa.Create(); // generate asymmetric key pair
        var req = new CertificateRequest("cn=foobar", ecdsa, HashAlgorithmName.SHA256);
        return req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
    }

    public static IRouter CreateSimpleCRUD(List<User> users)
    {
        var router = new Router("/users");
        router.Get("", (context, _, _) => context.Json(users.Select(x => x.Id).OrderBy(x => x)));
        router.Get("/redirected", (context, _, _) => context.Redirect("/users"));
        
        router.Get("/:user", (context, _, _) =>
        {
            var id = context.Parameters["user"];
            var user = users.FirstOrDefault(x => x.Id == id);
            if (user == null)
                return context.Send(HttpStatusCode.NotFound);
            
            return context.Json(new { user.FullName });
        });

        router.Put("", async (context, _, _) =>
        {
            var user = await context.Body<User>();
            if (users.Any(x => x.Id == user.Id))
                throw context.Throw(HttpStatusCode.BadRequest, "User already exists");

            users.Add(user);
            await context.Send(HttpStatusCode.OK);
        });
        
        router.Delete("/:user", (context, _, _) =>
        {
            var id = context.Parameters["user"];
            var user = users.FirstOrDefault(x => x.Id == id);
            if (user == null)
                return context.Send(HttpStatusCode.NotFound);
            users.Remove(user);
            return context.Send(HttpStatusCode.OK);
        });
        
        var details = new Router("/:user/details");
        router.Use(details);
        details.Get("", (context, _, _) =>
        {
            var user = users.FirstOrDefault(x => x.Id == context.Parameters["user"]);
            if (user == null)
                throw context.Throw(HttpStatusCode.NotFound, "User does not exist");

            return context.Json(user.Details);
        });

        return router;
    }
}