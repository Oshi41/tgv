using System.Net;
using Flurl.Http;
using tgv_auth.extensions;
using tgv_auth.imp.basic;
using tgv_cors;
using tgv_server;
using tgv_session;
using tgv_session.imp;
using tgv;

namespace tgv_tests;

class Storage : TestStorage
{
    public Storage() : base(new BasicCredentials("user", "pass"))
    {
        
    }
    
    public override async Task<BasicSession?> Login(BasicCredentials credentials)
    {
        var session = await base.Login(credentials);
        if (session?.Name == "user")
        {
            session["secret"] = "true";
        }

        return session;
    }
}

public class TgvValidation
{
    private readonly TgvSettings _settings;
    private App _app;
    private IFlurlClient _client;

    public TgvValidation(TgvSettings settings)
    {
        _settings = settings;
    }

    public TgvValidation() : this(new TgvSettings()) {}

    [SetUp]
    public void Setup()
    {
        _app = new App(new TgvServer(_settings));
        
        _app.UseSession(new InMemoryStore(), "_session");
        _app.UseAuth(new BasicCredentialProvider(), 
            new Storage(),
            new BasicCookieStorage("_auth"));
        _app.Use(CorsMiddleware.Cors());

        _app.Get("/sensitive", async (ctx, next, _) =>
        {
            var session = (await ctx.Session())?.Id;
            if (session == null || session == Guid.Empty)
                throw ctx.Throw(HttpStatusCode.Unauthorized);

            var auth = await ctx.Auth<BasicSession>();
            if (auth == null || !auth.IsValid() || auth["secret"] == null)
                throw ctx.Throw(HttpStatusCode.Unauthorized);

            next();
        }, (ctx, _, _) => ctx.Text("Top secret!"));
        
        _app.Start(TestUtils.RandPort()).Wait();

        _client = _app.CreateAgent(Guid.NewGuid().ToString());
    }

    [TearDown]
    public void Teardown()
    {
        _client?.Dispose();
        _app?.Stop();
    }

    [Test(Description = "Should get though validation")]
    public async Task Success()
    {
        for (int i = 0; i < 5; i++)
        {
            var str = await _client.Request("sensitive")
                .WithHeader("Authorization", new BasicCredentials("user", "pass").ToHeader())
                .AllowHttpStatus("2xx")
                .GetStringAsync();
            
            Assert.That(str, Is.EqualTo("Top secret!"));
        }
    }
}