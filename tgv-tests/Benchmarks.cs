using Flurl.Http;
using Microsoft.AspNetCore.Builder;
using tgv_core.imp;
using tgv_server;
using tgv;

namespace tgv_tests;

[TestFixture, TestFixtureSource(typeof(Benchmarks), nameof(Cases))]
public class Benchmarks
{
    public static string[] Cases() => ["asp", "tgv"];

    private static readonly string[] Users = Enumerable.Range(0, 20).Reverse().Select(x => "Id_" + x).ToArray();
    private readonly Uri _url;


    public Benchmarks(string srv)
    {
        if (srv == "asp")
        {
            _url = new("http://localhost:7000/");

            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            app.MapGet("/users", () => Users.OrderBy(x => x));
            app.Urls.Add(_url.ToString());
            app.StartAsync().Wait();
            return;
        }

        if (srv == "tgv")
        {
            _url = new("http://localhost:7001/");

            var app = new App(x => new Server(new Settings(), x, new Logger()));
            app.Get("/users", (ctx, next, _) => ctx.Json(Users.OrderBy(x => x)));
            app.Start(_url.Port).Wait();
        }
    }

    [TestCase(10)]
    public async Task GetUsers(int count)
    {
        long items = 0;

        await Task.WhenAll(Enumerable.Range(0, count).Select(async _ =>
        {
            var ids = await (_url + "users").GetJsonAsync<string[]>();
            Interlocked.Add(ref items, ids.Length);
        }));

        Assert.That(items, Is.EqualTo(count * 20));
    }
}