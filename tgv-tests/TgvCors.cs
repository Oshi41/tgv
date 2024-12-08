using System.Net;
using System.Text.RegularExpressions;
using Flurl.Http;
using tgv_core.api;
using tgv_core.imp;
using tgv_cors;
using tgv;

namespace tgv_tests;

[TestFixtureSource(typeof(Servers), nameof(Servers.AllServers))]
public class TgvCors
{
    private readonly App _app;

    public TgvCors(Servers.ServerCreationCase fn)
    {
        _app = new App(fn);

        HttpHandler baseHandler = async (ctx, next, exception) =>
        {
            if (exception != null)
            {
                // default error handler
                next();
                return;
            }

            var failRaw = ctx.Parameters["fail"];
            var fail = bool.Parse(failRaw);
            
            var resp = fail
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.OK;

            await ctx.SendCode(resp);
        };

        _app.Get("/get/:fail", baseHandler);
        _app.Post("/post/:fail", baseHandler);
        _app.Put("/put/:fail", baseHandler);
        _app.Patch("/patch/:fail", baseHandler);
        _app.Delete("/delete/:fail", baseHandler);
        _app.Head("/head/:fail", baseHandler);
        _app.Trace("/trace/:fail", baseHandler);
        _app.Options("/options/:fail", baseHandler);
        
        _app.Get("/with_default_cors/:fail", CorsMiddleware.Cors(), baseHandler);
    }

    [SetUp]
    public async Task Setup()
    {
        await _app.Start(TestUtils.RandPort());
    }
    
    [TearDown]
    public void Teardown()
    {
        _app.Stop();
    }

    [TestCase(HttpStatusCode.NoContent)]
    [TestCase(HttpStatusCode.IMUsed)]
    public async Task Preflight(HttpStatusCode statusCode)
    {
        var settings = new CorsSettings(
            ["https://example.com"],
            code: statusCode
            );

        // default settings
        _app.Use(CorsMiddleware.Cors(settings));
        var client = _app.CreateAgent(nameof(Preflight) + statusCode);

        foreach (var path in ((Router)_app._root)._routes.Select(route => route.Route.Segments
                     .FirstOrDefault(x => x is { IsPattern: false, IsWildcard: false })
                     ?.Regex))
        {
            var resp = await client.Request(path)
                .WithHeader("Origin", "https://example.com")
                .OptionsAsync();
            
            Assert.That(resp.StatusCode, Is.EqualTo((int)settings.Code));
            Assert.That(resp.Headers.GetAll("Access-Control-Allow-Origin").First(),
                Is.EqualTo("https://example.com"));
        }
    }

    [Test]
    public async Task CheckHeadersDuringRequest()
    {
        var settings = new CorsSettings(["https://example.com"]);

        // default settings
        _app.Use(CorsMiddleware.Cors(settings));

        var client = _app.CreateAgent(nameof(CheckHeadersDuringRequest));
        
        foreach (var route in ((Router)_app._root)._routes.Select(x => x.Route))
        {
            var path = route.Segments
                .FirstOrDefault(x => x is { IsPattern: false, IsWildcard: false })
                ?.Regex;
            
            foreach (var fail in new[] {true, false})
            {
                var errorMsgHeader = $"Error during {route.Method} {path}?fail={fail}: ";
                
                var req = client.Request($"{path}?fail={fail}")
                    .WithHeader("Origin", "https://example.com")
                    .AllowAnyHttpStatus();
                
                IFlurlResponse? resp = await (route.Method.Method switch
                {
                    "GET" => req.GetAsync(),
                    "POST" => req.PostAsync(),
                    "PUT" => req.PutAsync(),
                    "PATCH" => req.PatchAsync(),
                    "DELETE" => req.DeleteAsync(),
                    "HEAD" => req.HeadAsync(),
                    _ => Task.FromResult<IFlurlResponse>(null)
                });

                if (resp == null)
                {
                    Console.Error.WriteLine($"{errorMsgHeader} not supported");
                    continue;
                }

                var expected = fail
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.OK;
                
                Assert.That(resp.StatusCode, Is.EqualTo((int) expected), 
                    $"{errorMsgHeader} code expected: {expected} but got {resp.StatusCode}");
                
                Assert.That(resp.Headers.GetAll("Access-Control-Allow-Origin").First(),
                    Is.EqualTo("https://example.com"),
                    $"{errorMsgHeader} header expected: \"https://example.com\"" +
                    $" but got {string.Join(", ", resp.Headers.GetAll("Access-Control-Allow-Origin"))}");
            }

        }
    }

    [Test]
    public async Task ContinuePreflight()
    {
        var settings = new CorsSettings(
            ["https://example.com"],
            continuePreflight:true
            );
        
        _app.Use(CorsMiddleware.Cors(settings));
        _app.Options("/continue_preflight", async (ctx, next, _) =>
        {
            await ctx.SendCode(HttpStatusCode.Forbidden);
        });
        
        var client = _app.CreateAgent(nameof(ContinuePreflight));

        var cases = new Dictionary<string, HttpStatusCode>
        {
            ["continue_preflight"] = HttpStatusCode.Forbidden,
            ["options?fail=true"] = HttpStatusCode.BadRequest,
            ["options?fail=false"] = HttpStatusCode.OK,
        };
        
        foreach (var (path, expected) in cases)
        {
            var resp = await client.Request(path)
                .WithHeader("Origin", "https://example.com")
                .AllowAnyHttpStatus()
                .OptionsAsync();
            Assert.That(resp.StatusCode, Is.EqualTo((int)expected),
                $"OPTIONS /{path} Wrong code {(int)expected} but got {resp.StatusCode}");
        }
    }

    [Test]
    public async Task TestRegex()
    {
        var settings = new CorsSettings(
            [new Regex("^https.+com(:\\d{1,5})?[\\/]?$")]
        );
        _app.Use(CorsMiddleware.Cors(settings));
        var client = _app.CreateAgent(nameof(TestRegex));
        
        var valid = new[] {
            "https://example.com",
            "https://test.com",
            "https://a.com",
            "https://sub.example.com",
            "https://www.test.com",
            "https://api.subdomain.com",
            "https://example.com/",
            "https://sub.test.com/",
            "https://averylongsubdomain.example.com",
            "https://reallyreallylongname.com",
            "https://a.b.c.d.e.f.g.h.i.j.k.l.m.n.o.p.q.r.s.t.com",
            "https://example.com:8080",
            "https://example.com:80",
            "https://sub.test.com:443",
            "https://www.test.com:3000/",
            "https://api.subdomain.com:8080",
            "https://sub.example.com:1234/",
            "https://averylongsubdomain.example.com:65535",
            "https://reallyreallylongname.com:1",
        };
        
        foreach (var site in valid)
        {
            var resp = await client.Request("get")
                .WithHeader("Origin", site)
                .AllowHttpStatus("2xx")
                .OptionsAsync();
            
            Assert.That(resp.Headers.GetAll("Access-Control-Allow-Origin").First(),
                Is.EqualTo(site));
        }

        var invalid = new List<string>
        {
            "https://example.com:123456",
            "https://example.com:",
            "http://example.com:80",
            "ftp://example.com:21",
            "https://example.org:443",
            "https://sub.example.net:8080",
        };
        
        foreach (var site in invalid)
        {
            var resp = await client.Request("get")
                .WithHeader("Origin", site)
                .AllowHttpStatus("2xx")
                .OptionsAsync();
            
            Assert.That(resp.Headers.GetAll("Access-Control-Allow-Origin"), Is.Null.Or.Empty);
        }
    }

    [Test]
    public async Task CheckAllHeaders()
    {
        var settings = new CorsSettings(
            ["*"],
            [HttpMethod.Get, HttpMethod.Head, ],
            false,
            HttpStatusCode.NoContent,
            10,
            true,
            ["Allowed1", "Allowed2"],
            ["Exposed1", "Exposed2"]
        );
        _app.Use(CorsMiddleware.Cors(settings));
        var client = _app.CreateAgent(nameof(TestRegex));
        
        var resp = await client.Request("get")
            .WithHeader("Origin", "https://example.com")
            .AllowHttpStatus(((int)HttpStatusCode.NoContent).ToString())
            .OptionsAsync();

        var headers = new Dictionary<string, string>
        {
            ["Access-Control-Allow-Origin"] = "https://example.com",
            ["Access-Control-Allow-Credentials"] = "true",
            ["Access-Control-Expose-Headers"] = "Exposed1,Exposed2",
            ["Access-Control-Allow-Methods"] = "GET,HEAD",
            ["Access-Control-Max-Age"] = "10",
            ["Access-Control-Allow-Headers"] = "Allowed1,Allowed2",
        };
        
        foreach (var (key, expected) in headers)
        {
            var current = resp.Headers.GetAll(key).FirstOrDefault();
            Assert.That(current, Is.EqualTo(expected),
                $"{key} header expected: {expected} but got {current}");
        }
    }
}