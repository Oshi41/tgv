using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Flurl.Http;
using tgv_core.imp;
using tgv_server;
using tgv;

namespace tgv_tests;

[Ignore("WIP")]
public class HttpsTests
{
    private readonly List<User> _users = new();
    private App _app;
    private X509Certificate2 _cert;

    [SetUp]
    public void Setup()
    {
        var rand = new Random();

        for (int i = 0; i < 20; i++)
        {
            _users.Add(new User
            {
                Id = $"User_{i}",
                Details = new Details
                {
                    Surname = $"Surname_{i}",
                    Name = $"Name_{i}",
                    Age = rand.Next(0, 100),
                }
            });
        }

        _cert = TestUtils.MakeDebugCert();

        var cfg = new TgvSettings
        {
            Certificate = _cert,
            CertificateValidation = (_, _, _, _) => true,
            AddServerHeader = true
        };

        FlurlHttp.Clients.WithDefaults(builder =>
        {
            builder.ConfigureInnerHandler(clientHandler =>
            {
                clientHandler.ClientCertificates.Add(_cert);
                clientHandler.ServerCertificateCustomValidationCallback =
                    new Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>(
                        (_, _, _, _) => true);
            });
        });

        _app = new App(h => new TgvServer(cfg, h, new Logger()));
        _app.Use(TestUtils.CreateSimpleCRUD(_users));

        _app.Start(TestUtils.RandPort()).Wait();
    }

    [TearDown]
    public void TearDown()
    {
        _app?.Stop();
        _users.Clear();
        _cert.Dispose();
    }

    [Test]
    public async Task GetUsers()
    {
        string[] ids;
        var client = _app.CreateAgent("");

        var expected = _users.Select(x => x.Id).OrderBy(x => x).ToList();
        ids = await client.Request("users").GetJsonAsync<string[]>();
        Assert.That(ids, Is.EqualTo(expected));

        ids = await client.Request("users/redirected").GetJsonAsync<string[]>();
        Assert.That(ids, Is.EqualTo(expected));
    }
}