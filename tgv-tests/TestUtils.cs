using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Flurl.Http;
using HtmlParserDotNet;
using tgv_core.api;
using tgv_core.imp;
using tgv_server;
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

    public override Task SendRaw(byte[]? bytes, HttpStatusCode code, string? contentType)
    {
        return AfterSending();
    }

    public override Task SendFile(Stream stream, string? filename)
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
    private static readonly object _locker = new();

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
        return 1000 + (int)(DateTime.Now.ToFileTimeUtc() % 2900) + new Random().Next(0, 100);
    }

    public static IFlurlClient CreateAgent(this App app, string prefix, [CallerMemberName] string method = "",
        [CallerFilePath] string file = "")
    {
        lock (_locker)
        {
            return FlurlHttp.Clients.GetOrAdd($"{file}_{method}_{prefix}", app.RunningUrl);
        }
    }

    public static void SetupFlurlClient(TgvSettings settings)
    {
        lock (_locker)
        {
            FlurlHttp.Clients.WithDefaults(builder =>
            {
                builder.ConfigureInnerHandler(clientHandler =>
                {
                    clientHandler.ClientCertificates.Add(MakeDebugCert(settings.Certificate));
                    clientHandler.ServerCertificateCustomValidationCallback =
                        new Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>(
                            (_, _, _, _) => true);
                });
            });
        }
    }

    public static async Task<HtmlElement> GetHtmlAsync(this IFlurlRequest request)
    {
        var txt = await request.GetStringAsync();
        var html = HtmlParser.LoadFromHtmlString(txt);
        return html.DocumentElement;
    }

    public static X509Certificate2 MakeDebugCert(X509Certificate2? root = null)
    {
        var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pfx");
        Process.GetCurrentProcess().Exited += (sender, args) =>
        {
            if (File.Exists(file))
                File.Delete(file);
        };

        var ecdsa = ECDsa.Create(); // generate asymmetric key pair
        var req = new CertificateRequest("cn=foobar", ecdsa, HashAlgorithmName.SHA256);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 1, true));
        var generated = root != null
            ? req.Create(root, DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1), Guid.NewGuid().ToByteArray())
            : req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

        var data = generated.Export(X509ContentType.Pkcs12);
        File.WriteAllBytes(file, data);
        return X509CertificateLoader.LoadPkcs12FromFile(file, null, X509KeyStorageFlags.PersistKeySet);
    }

    public static IRouter CreateSimpleCRUD(List<User> users)
    {
        var router = new Router("/users");
        var redirected = new Router("/redirected");
        var details = new Router("/:user/details");

        router.Use(redirected);
        router.Use(details);


        router.Get("", (context, _, _) => context.Json(users.Select(x => x.Id).OrderBy(x => x)));
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


        redirected.Get("", (context, _, _) => context.Redirect("/users"));
        redirected.Get("/:user", (context, _, _) => context.Redirect($"/users/{context.Parameters["user"]}"));
        redirected.Put("", (context, _, _) => context.Redirect("/users"));
        redirected.Delete("/:user", (context, _, _) => context.Redirect($"/users/{context.Parameters["user"]}"));


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